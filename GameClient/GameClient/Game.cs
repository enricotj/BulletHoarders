using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace GameClient
{
    public class Game : Receiver
    {
        private Socket sock;
        private EndPoint serverEndPoint;

        private bool hasLevel = false;

        private string name;

        public Game(string name)
        {
            this.name = name;

            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ip = hostEntry.AddressList[0];

            IPAddress ip = IPAddress.Parse("127.0.0.1");
            serverEndPoint = new IPEndPoint(ip, 11200);
            byte[] handshake = new byte[sizeof(int) + name.Length * 2];
            
            byte[] portBytes = BitConverter.GetBytes(Client.port);
            byte[] nameBytes = Encoding.ASCII.GetBytes(this.name);

            int i = 0;
            foreach (byte b in portBytes)
            {
                handshake[i] = b;
                i++;
            }
            foreach (byte b in nameBytes)
            {
                Console.WriteLine(b.ToString());
                handshake[i] = b;
                i++;
            }

            sock.SendTo(handshake, serverEndPoint);
        }

        public void Receive(string source, byte[] data)
        {
            Console.WriteLine("DATA RECEIVED");
            if (!hasLevel)
            {
                int width = (int)data[0];
                for (int r = 0; r < data.Length - 1; r += width)
                {
                    for (int c = 0; c < width; c++)
                    {
                        int cell = data[r + c + 1];
                    }
                }

                hasLevel = true;

                byte[] ready = {255};
                sock.SendTo(ready, serverEndPoint);
            }
        }

        public void Run()
        {
            while (true)
            {
                // Console.WriteLine("Game is running...");

                // Calculate DeltaTime

                // Calculate collisions

                // Send clients positional data for all game objects

                Thread.Sleep(1000);
            }
        }
    }
}
