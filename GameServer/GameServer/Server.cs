using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer
{
    public class Server
    {
        private const int PORT = 11200;

        static void Main(string[] args)
        {
            Game game = new Game();
            Listener listener = new Listener(game, PORT);

            Thread gameThread = new Thread(new ThreadStart(game.Run));
            gameThread.Start();
            while (!gameThread.IsAlive) ;

            Thread listenThread = new Thread(new ThreadStart(listener.Listen));
            listenThread.Start();
            while (!listenThread.IsAlive) ;
        }

    }
}
