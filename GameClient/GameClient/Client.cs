using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameClient
{
    class Client
    {
        static void Main(string[] args)
        {
            Game game = new Game();
            Listener listener = new Listener(game);

            Thread gameThread = new Thread(new ThreadStart(game.Run));
            gameThread.Start();
            while (!gameThread.IsAlive) ;

            Thread listenThread = new Thread(new ThreadStart(listener.Listen));
            listenThread.Start();
            while (!listenThread.IsAlive) ;

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ip = hostEntry.AddressList[0];


            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint endPoint = new IPEndPoint(ip, 11200);
            Int32 port = 22200;
            byte[] portBytes = BitConverter.GetBytes(port);
            Console.WriteLine(BitConverter.ToInt32(portBytes, 0));
            sock.SendTo(portBytes, endPoint);

            /*while (true)
            {
                Console.WriteLine("Enter text to send: ");
                string msg = Console.ReadLine();
                if (msg.Length == 0)
                {
                    break;
                }
                else
                {
                    byte[] data = Encoding.ASCII.GetBytes(msg);
                    //Console.WriteLine("sending to address: {0} port: {1}", endPoint.Address, endPoint.Port);
                    try
                    {
                        sock.SendTo(data, endPoint);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                
            }
            */
            
        }
    }
}
