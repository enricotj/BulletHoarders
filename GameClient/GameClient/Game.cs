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
        public Game()
        {
            
        }

        public void Receive(string source, byte[] data)
        {
            Console.WriteLine("DATA RECEIVED");
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
