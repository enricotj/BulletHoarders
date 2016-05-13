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
    public class Listener
    {
        private Receiver receiver;

        public Listener(Receiver receiver)
        {
            this.receiver = receiver;
        }

        public void Listen()
        {
            // buffer for incoming data
            byte[] data;

            // open up a port for listening
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Client.port);
            UdpClient listener = new UdpClient(endPoint.Port);

            while (true)
            {
                // receive and print message
                data = listener.Receive(ref endPoint);
                string msg = Encoding.ASCII.GetString(data, 0, data.Length);
                Console.WriteLine(endPoint.Address.ToString());

                // handle received data on new thread so that we can immediately go back to listening
                Thread receiveThread = new Thread(() => receiver.Receive(endPoint.ToString(), data));
                receiveThread.Start();
            }

            //listener.Close();
        }
    }
}
