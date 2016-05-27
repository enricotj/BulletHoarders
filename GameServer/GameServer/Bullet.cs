using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Bullet
    {
        // keeps track of the player id who shot this bullet
        public byte pid;

        public int id;

        public float x;
        public float y;

        public float vx;
        public float vy;

        public int col;
        public int row;

        public Bullet(float x, float y, byte pid)
        {
            this.x = x;
            this.y = y;
            this.pid = pid;
        }

        public byte[] GetBytes()
        {
            byte[] data = { };
            byte[] temp = BitConverter.GetBytes(id);
            data = data.Concat(temp).ToArray();

            byte[] temp2 = { pid };
            data = data.Concat(temp2).ToArray();

            temp = BitConverter.GetBytes(x);
            data = data.Concat(temp).ToArray();
            temp = BitConverter.GetBytes(y);
            data = data.Concat(temp).ToArray();

            temp = BitConverter.GetBytes(vx);
            data = data.Concat(temp).ToArray();
            temp = BitConverter.GetBytes(vy);
            data = data.Concat(temp).ToArray();
            
            return data;
        }

        public byte[] GetStopBytes()
        {
            byte[] data = { };
            byte[] temp = BitConverter.GetBytes(id);
            data = data.Concat(temp).ToArray();
            
            temp = BitConverter.GetBytes(x);
            data = data.Concat(temp).ToArray();
            temp = BitConverter.GetBytes(y);
            data = data.Concat(temp).ToArray();

            return data;
        }

        public byte[] GetDestroyBytes()
        {
            byte[] data = { };
            byte[] temp = BitConverter.GetBytes(id);
            data = data.Concat(temp).ToArray();
            return data;
        }

    }
}
