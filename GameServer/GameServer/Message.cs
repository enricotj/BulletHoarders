using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Message
    {
        public string source;
        public byte[] data;

        public Message(string source, byte[] data)
        {
            this.source = source;
            this.data = data;
        }
    }
}
