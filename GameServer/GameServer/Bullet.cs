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
        public int pid;

        public float x;
        public float y;

        public float vx;
        public float vy;

        public int col;
        public int row;

        public Bullet(float x, float y, int pid)
        {
            this.x = x;
            this.y = y;
            this.pid = pid;
        }

        public byte[] GetBytes()
        {
            int n = 0;
            byte[] data = new byte[sizeof(float) * 4];
            byte[] temp = BitConverter.GetBytes(x);
            temp.CopyTo(data, n);
            n += temp.Length;
            temp = BitConverter.GetBytes(y);
            temp.CopyTo(data, n);
            n += temp.Length;
            temp = BitConverter.GetBytes(vx);
            temp.CopyTo(data, n);
            n += temp.Length;
            temp = BitConverter.GetBytes(vy);
            temp.CopyTo(data, n);
            n += temp.Length;
            return data;
        }
    }
}
