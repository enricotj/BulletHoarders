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
        private const int NAME_LENGTH = 16;

        public string name;

        private IPEndPoint endPoint;

        public float x;
        public float y;

        public float vx;
        public float vy;

        public float r;

        public int bullets = 5;

        public int id;

        public bool init;

        public float invTime = 2.5f;

        public int col;
        public int row;

        public bool shoot = false;

        public bool left, right, up, down;

        public Player(string name, IPEndPoint endPoint, float x, float y, int id)
        {
            this.name = name;
            this.endPoint = endPoint;
            this.x = x;
            this.y = y;
            this.id = id;
        }

        public Player(string name, IPEndPoint endPoint, int id)
        {
            this.name = name;
            this.endPoint = endPoint;
            this.id = id;
        }

        public IPEndPoint EndPoint
        {
            get
            {
                return endPoint;
            }
        }

        public void CalculateVelocity()
        {
            if (left && up)
            {
                vx = -Game.ANGLE;
                vy = Game.ANGLE;
            }
            else if (right && up)
            {
                vx = Game.ANGLE;
                vy = Game.ANGLE;
            }
            else if (right && down)
            {
                vx = Game.ANGLE;
                vy = -Game.ANGLE;
            }
            else if (left && down)
            {
                vx = -Game.ANGLE;
                vy = -Game.ANGLE;
            }
            else if (left)
            {
                vx = -1;
                vy = 0;
            }
            else if (right)
            {
                vx = 1;
                vy = 0;
            }
            else if (up)
            {
                vx = 0;
                vy = 1;
            }
            else if (down)
            {
                vx = 0;
                vy = -1;
            }
            else
            {
                vx = 0;
                vy = 0;
            }
        }

        public byte[] GetBytes()
        {
            // id, x, y, vx, vy, r
            byte[] data = { };
            byte[] temp = BitConverter.GetBytes(id);
            data = data.Concat(temp).ToArray();

            temp = BitConverter.GetBytes(x);
            data = data.Concat(temp).ToArray();

            temp = BitConverter.GetBytes(y);
            data = data.Concat(temp).ToArray();
            
            temp = BitConverter.GetBytes(vx);
            data = data.Concat(temp).ToArray();
            
            temp = BitConverter.GetBytes(vy);
            data = data.Concat(temp).ToArray();

            temp = BitConverter.GetBytes(r);
            data = data.Concat(temp).ToArray();

            return data;
        }

        public bool CollidesWith(Bullet bullet)
        {
            double distance = Math.Sqrt(Math.Pow(x - bullet.x, 2) + Math.Pow(y - bullet.y, 2));
            if (distance < 0.5 + Game.BULLET_SIZE)
            {
                return true;
            }
            return false;
        }
    }
}
