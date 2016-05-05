using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    public class Game : Receiver
    {
        private Dictionary<string, Player> clients;
        private Socket sock;

        //private List<Player> players;
        //private List<Bullet> bullets;
        //private Level level;

        public Game()
        {
            clients = new Dictionary<string, Player>();
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // Generate Level
        }

        public void Receive(string source, byte[] data)
        {
            Console.WriteLine("DATA RECEIVED");

            if (!clients.ContainsKey(source))
            {
                // add client to list
                int clientPort = BitConverter.ToInt32(data, 0);
                IPEndPoint cep = new IPEndPoint(IPAddress.Parse(source), clientPort);
                clients.Add(source, new Player("" + clientPort, cep));

                // send ACK
                byte[] ack = Encoding.ASCII.GetBytes("ACK");
                sock.SendTo(ack, cep);
            }

            // detect if client is disconnecting

            // if not, update player/bullet velocities for client
        }

        public void Run()
        {
            while (true)
            {
                Console.WriteLine("Game is running...");

                // Calculate DeltaTime

                // Calculate collisions

                // Send clients positional data for all game objects
                foreach (Player p in clients.Values)
                {
                    // send player states
                    
                    // send bullet states

                    // sock.SendTo(data, p.EndPoint);
                }
                
                Thread.Sleep(1000);
            }
        }
    }
}
