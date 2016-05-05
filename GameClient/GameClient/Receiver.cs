using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient
{
    public interface Receiver
    {
        void Receive(string source, byte[] data);
    }
}
