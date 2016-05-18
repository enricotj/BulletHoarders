using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Cell
    {
        private List<Player> players;
        private List<Bullet> bullets;

        public Cell()
        {
            players = new List<Player>();
            bullets = new List<Bullet>();
        }

        public void RemovePlayer(int id)
        {
            int r = 0;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].id == id)
                {
                    r = i;
                    break;
                }
            }
            if (players.Count > r)
            {
                players.RemoveAt(r);
            }
        }

        public void AddPlayer(Player p)
        {
            players.Add(p);
        }

        public void RemoveBullet(Bullet b)
        {
            bullets.Remove(b);
        }

        public void AddBullet(Bullet b)
        {
            bullets.Add(b);
        }

        public List<Player> GetPlayers()
        {
            return players;
        }

        public List<Bullet> GetBullets()
        {
            return bullets;
        }
    }
}
