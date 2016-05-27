using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Message
    {
        public string source;
        public IPEndPoint endPoint;
        public byte[] data;

        public Message(string source, byte[] data)
        {
            this.source = source;
            this.data = data;
        }
    }
}
