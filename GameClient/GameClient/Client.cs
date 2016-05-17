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
        public static int port;

        static void Main(string[] args)
        {
            string name;

            do
            {
                Console.Write("Enter a Name (16 char limit): ");
                name = Console.ReadLine();
            } while (name.Length > 16);

            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            Console.WriteLine((int)nameBytes[3]);

            Console.Write("Enter Port: ");
            port = Convert.ToInt32(Console.ReadLine());

            Game game = new Game(name);
            Listener listener = new Listener(game);

            Thread gameThread = new Thread(new ThreadStart(game.Run));
            gameThread.Start();
            while (!gameThread.IsAlive) ;

            Thread listenThread = new Thread(new ThreadStart(listener.Listen));
            listenThread.Start();
            while (!listenThread.IsAlive) ;

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
