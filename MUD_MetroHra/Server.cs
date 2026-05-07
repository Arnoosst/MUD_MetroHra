using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServer2
{
    public class Server
    {
        private TcpListener myServer;
        private bool isRunning;

        public Server(int port)
        {
            myServer = new TcpListener(System.Net.IPAddress.Any, port);
            myServer.Start();
            isRunning = true;
            ServerLoop();
        }

        private void ServerLoop()
        {
            Console.WriteLine("Server byl spusten");
            while (isRunning)
            {
                TcpClient client = myServer.AcceptTcpClient();
                Thread thread = new Thread(new ParameterizedThreadStart(ClientLoop));
                thread.Start(client);
            }

        }

        private void ClientLoop(object obj)
        {
            TcpClient client = (TcpClient)obj;
            StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.UTF8);

            writer.WriteLine("Byl jsi pripojen");
            writer.Flush();
            bool clientConnect = true;
            string data = null;
            string dataRecive = null;
            while (clientConnect)
            {
                data = reader.ReadLine();
                data = data.ToLower();

                dataRecive = data;
                writer.WriteLine(dataRecive);
                writer.Flush();
            }

        }
    }
}
