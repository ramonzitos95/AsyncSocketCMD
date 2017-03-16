using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CmdClient
{
    class Program
    {
        static ManualResetEvent connectDone = new ManualResetEvent(false);
        static ManualResetEvent sendDone = new ManualResetEvent(false);
        static ManualResetEvent receiveDone = new ManualResetEvent(false);

        static int bytesMessage = 0;

        static void Main(string[] args)
        {
            StartClient();
        }

        static void StartClient()
        {
            try
            {
                Console.WriteLine("Async Client Socket");

                Console.Write("IP: ");
                IPAddress ipAddress = IPAddress.Parse(Console.ReadLine());

                IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, 11000);

                Socket client = new Socket(AddressFamily.InterNetwork,
                                           SocketType.Stream,
                                           ProtocolType.Tcp);

                client.BeginConnect(remoteEndPoint, new AsyncCallback(ConnectCallback), client);

                connectDone.WaitOne();

                Console.Write("Digite a mensagem a ser enviada: ");
                string msgSend = Console.ReadLine() + "<EOF>";

                Send(client, msgSend);
                sendDone.WaitOne();

                Receive(client);
                receiveDone.WaitOne();

                client.Shutdown(SocketShutdown.Both);
                client.Close();

                Console.WriteLine("Conexão finalizada...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("\nPressione ENTER para finalizar...");
            Console.Read();
        }

        static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                client.EndConnect(ar);

                Console.WriteLine("Conectado com {0}...", client.RemoteEndPoint);

                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Send(Socket client, String data)
        {
            try
            {
                byte[] byteData = Encoding.UTF8.GetBytes(data);

                client.BeginSend(byteData,
                                 0,
                                 byteData.Length,
                                 0,
                                 new AsyncCallback(SendCallback),
                                 client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                int bytesSent = client.EndSend(ar);

                Console.WriteLine("Foram enviados {0} bytes...", bytesSent);

                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Receive(Socket client)
        {
            try
            {
                StateObject state = new StateObject();
                state.workSocket = client;

                client.BeginReceive(state.buffer,
                                    0,
                                    StateObject.BufferSize,
                                    0,
                                    new AsyncCallback(ReceiveCallback),
                                    state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                int bytesRead = client.EndReceive(ar);

                bytesMessage += bytesRead;

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));

                    client.BeginReceive(state.buffer,
                                        0,
                                        StateObject.BufferSize,
                                        0,
                                        new AsyncCallback(ReceiveCallback),
                                        state);
                }
                else
                {
                    if (state.sb.Length > 1)
                    {
                        Console.WriteLine("Foram recebidos {0} bytes", bytesMessage);
                        Console.WriteLine("Mensagem recebida: {0}", state.sb.ToString());

                        bytesMessage = 0;
                    }

                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    internal class StateObject
    {
        internal Socket workSocket = null;
        internal const int BufferSize = 256;
        internal byte[] buffer = new byte[BufferSize];
        internal StringBuilder sb = new StringBuilder();
    }
}