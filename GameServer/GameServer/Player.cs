using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Player
    {
        // limit to 16 characters (17 including \0)
        private string name;
        private IPEndPoint endPoint;

        private float x;
        private float y;

        private float vx;
        private float vy;

        private float r;

        private int bullets = 5;

        public Player(string name, IPEndPoint endPoint, float x, float y)
        {
            this.name = name;
            this.endPoint = endPoint;
            this.x = x;
            this.y = y;
        }

        public Player(string name, IPEndPoint endPoint)
        {
            this.name = name;
            this.endPoint = endPoint;
        }

        public IPEndPoint EndPoint
        {
            get
            {
                return endPoint;
            }
        }

        public byte[] GetBytes()
        {
            byte[] data = {255};

            Encoding.ASCII.GetBytes(name);
            BitConverter.GetBytes(x);
            BitConverter.GetBytes(y);
            BitConverter.GetBytes(vx);
            BitConverter.GetBytes(vy);
            BitConverter.GetBytes(r);
            BitConverter.GetBytes(bullets);

            // TODO: Append ALL the things

            return data;
        }

    }
}
