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
    public class Listener
    {
        private Receiver receiver;
        private int port;

        public Listener(Receiver receiver, int port)
        {
            this.receiver = receiver;
            this.port = port;
        }

        public void Listen()
        {
            // buffer for incoming data
            byte[] data;

            // open up a port for listening
            UdpClient listener = new UdpClient(port);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

            while (true)
            {
                // receive and print message
                data = listener.Receive(ref endPoint);
                string msg = Encoding.ASCII.GetString(data, 0, data.Length);
                Console.WriteLine("{0} --> {1}", endPoint.ToString(), msg);

                // handle received data on new thread so that we can immediately go back to listening
                Thread receiveThread = new Thread(() => receiver.Receive(endPoint.Address.ToString(), data));
                receiveThread.Start();
            }

            //listener.Close();
        }
    }
}
