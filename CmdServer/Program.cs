using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CmdServer
{
    class Program
    {
        static ManualResetEvent allDone = new ManualResetEvent(false);

        static int bytesMessage = 0;

        static void Main(string[] args)
        {
            StartListening();
        }

        static void StartListening()
        {
            try
            {
                Console.WriteLine("<<< Server Async Socket >>>");

                // Recupera o endereço IPv4 em uso
                IPEndPoint server = null;

                var host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        server = new IPEndPoint(ip, 11000);
                    }
                }

                // Cria o socket que aguardará pelas conexões dos clientes
                Socket listener = new Socket(AddressFamily.InterNetwork,
                                             SocketType.Stream,
                                             ProtocolType.Tcp);

                listener.Bind(server);
                listener.Listen(10);

                Console.WriteLine("Servidor {0}...", listener.LocalEndPoint);
                Console.WriteLine("Aguardando conexão...\n");

                // Inicializa a thread que aceitará as conexões
                while (true)
                {
                    allDone.Reset();

                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("\nPressione ENTER para finalizar...");
            Console.Read();
        }

        static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                allDone.Set();

                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                Console.WriteLine("Recebendo dados de {0}...", handler.RemoteEndPoint.ToString());

                StateObject state = new StateObject();
                state.workSocket = handler;

                handler.BeginReceive(state.buffer,
                                     0,
                                     StateObject.BufferSize,
                                     0,
                                     new AsyncCallback(ReadCallback),
                                     state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void ReadCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                int bytesRead = handler.EndReceive(ar);

                bytesMessage += bytesRead;

                if (bytesRead > 0)
                {
                    state.msg.Append(Encoding.UTF8.GetString(state.buffer,
                                                             0,
                                                             bytesRead));

                    if (state.msg.ToString().IndexOf("<EOF>") > -1)
                    {
                        Console.WriteLine("Foram recebidos {0} bytes...", bytesMessage);
                        Console.WriteLine("Mensagem recebida: {0}", state.msg.ToString());

                        bytesMessage = 0;

                        Send(handler, state.msg.ToString());
                    }
                    else
                    {
                        handler.BeginReceive(state.buffer,
                                             0,
                                             StateObject.BufferSize,
                                             0,
                                             new AsyncCallback(ReadCallback),
                                             state);
                    }
                }
            }
            catch (Exception e)
            {                
                Console.WriteLine(e.Message);
            }
        }

        static void Send(Socket handler, string content)
        {
            try
            {
                byte[] byteData = Encoding.UTF8.GetBytes(content);

                handler.BeginSend(byteData,
                                  0,
                                  byteData.Length,
                                  0,
                                  new AsyncCallback(SendCallBack),
                                  handler);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void SendCallBack(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;

                int bytesSent = handler.EndSend(ar);

                Console.WriteLine("Reenviando {0} bytes...", bytesSent);
                Console.WriteLine("Mensagem reenviada...");

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

                Console.WriteLine("Conexão finalizada...\n");
            }
            catch (Exception e )
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    internal class StateObject
    {
        internal Socket workSocket = null;
        internal const int BufferSize = 1024;
        internal byte[] buffer = new byte[BufferSize];
        internal StringBuilder msg = new StringBuilder();
    }
}