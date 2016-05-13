using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.Net;
using System.Net.Sockets;

using GameServer;

namespace GameServer
{
    public class Game : Receiver
    {
        private int updateWidth = 12;
        private int updateHeight = 7;

        private Dictionary<string, Player> clients;
        private List<Bullet> bullets;

        private Socket sock;

        private const int MAX_PLAYERS = 50;
        private int nextPlayerId = 0;

        private Cave level;
        private Cell[,] cells;

        public static float ANGLE = (float)Math.Sqrt(2)/2;

        private const float PLAYER_SPEED = 5.0f;
        private const float BULLET_SPEED = 10.0f;
        public static float BULLET_SIZE = 0.125f;

        public Game()
        {
            clients = new Dictionary<string, Player>();
            bullets = new List<Bullet>();

            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // Generate Level
            level = new Cave();
            level.GetBytes();

            cells = new Cell[Cave.caveWidth, Cave.caveHeight];
            for (int c = 0; c < Cave.caveWidth; c++)
            {
                for (int r = 0; r < Cave.caveHeight; r++)
                {
                    cells[c, r] = new Cell();
                }
            }
        }

        public void Receive(string source, byte[] data)
        {
            Console.WriteLine("DATA RECEIVED");

            if (!clients.ContainsKey(source))
            {
                // add client to list
                byte[] portData = data.SubArray(0, sizeof(int));
                int clientPort = BitConverter.ToInt32(data, 0);
                IPEndPoint cep = new IPEndPoint(IPAddress.Parse(source), clientPort);
                Console.WriteLine(clientPort);

                // get player name
                byte[] nameData = data.SubArray(sizeof(int));
                string name = Encoding.ASCII.GetString(nameData);
                Console.WriteLine(name);

                clients.Add(source, new Player(name, cep, nextPlayerId));
                nextPlayerId++;

                // send initial level data
                byte[] initData = level.GetBytes();
                
                // TODO: send playerNameMap
                //initData = initData.Concat(playerMap).ToArray();
                
                // TODO: send player id

                // SEND
                sock.SendTo(initData, cep);
            }
            else
            {
                Player player = clients[source];
                if (!player.init && data[0] == 255)
                {
                    player.init = true;
                    Console.WriteLine("PLAYER INITIALIZED");

                    GridPoint gp = level.GetSpawnPoint();
                    player.x = gp.columm + 0.5f;
                    player.y = gp.row + 0.5f;
                    player.col = (int)player.x;
                    player.row = (int)player.y;

                    cells[gp.columm, gp.row].AddPlayer(player);

                    // send scene packet to player
                    byte[] scene = this.GetScene(gp.columm, gp.row);
                    sock.SendTo(scene, player.EndPoint);
                }
                else
                {
                    int code = data[0];
                    switch (code)
                    {
                        // key presses for left, right, up, down
                        case 0:
                            player.left = true;
                            player.right = false;
                            break;
                        case 10:
                            player.right = true;
                            player.left = false;
                            break;
                        case 20:
                            player.up = true;
                            player.down = false;
                            break;
                        case 30:
                            player.down = true;
                            player.down = false;
                            break;

                        // key releases for left, right, up, down
                        case 100:
                            player.left = false;
                            break;
                        case 110:
                            player.right = false;
                            break;
                        case 120:
                            player.up = false;
                            break;
                        case 130:
                            player.down = false;
                            break;

                        // bullet was shot
                        case 40:
                            player.shoot = true;
                            break;

                        // player disconnected
                        case 200:
                            // TODO: add player to list to be safely disconnected later
                            break;
                    }
                }
            }

        }

        private void UpdatePlayers(float dt)
        {
            foreach (Player player in clients.Values)
            {
                if (!player.init)
                {
                    continue;
                }

                player.CalculateVelocity();

                // wall collision logic
                PlayerCollide(player);

                player.x += player.vx * PLAYER_SPEED * dt;
                player.y += player.vy * PLAYER_SPEED * dt;

                int col = player.col;
                int row = player.row;

                int ncol = (int)player.x;
                int nrow = (int)player.y;

                if (col != ncol || row != nrow)
                {
                    cells[col, row].RemovePlayer(player.id);
                    cells[ncol, nrow].AddPlayer(player);
                }

                player.col = ncol;
                player.row = nrow;

                if (player.shoot)
                {
                    Bullet bullet = new Bullet(player.x, player.y, player.id);
                    bullet.vx = (float)Math.Cos(player.r);
                    bullet.vy = (float)Math.Sin(player.r);
                    bullets.Add(bullet);
                    player.shoot = false;
                }
            }
        }

        private void UpdateBullets(float dt)
        {
            List<Bullet> spawnedBullets = new List<Bullet>();
            List<Bullet> pickedBullets = new List<Bullet>();

            foreach (Bullet bullet in bullets)
            {
                // wall collision logic
                BulletCollide(bullet);

                bullet.x += bullet.vx * BULLET_SPEED * dt;
                bullet.y += bullet.vy * BULLET_SPEED * dt;

                int col = bullet.col;
                int row = bullet.row;

                int ncol = (int)bullet.x;
                int nrow = (int)bullet.y;

                if (col != ncol || row != nrow)
                {
                    cells[col, row].RemoveBullet(bullet);
                    cells[ncol, nrow].AddBullet(bullet);
                }

                bullet.col = ncol;
                bullet.row = nrow;

                // COLLISIONS
                float x = bullet.x;
                float y = bullet.y;
                bool moving = !(bullet.vx == 0 && bullet.vy == 0);
                for (int r = nrow - 1; r <= nrow + 1; r++)
                {
                    bool collision = false;
                    for (int c = ncol - 1; c <= ncol + 1; c++)
                    {
                        foreach (Player player in cells[ncol, nrow].GetPlayers())
                        {
                            if (player.invTime <= 0 && moving && bullet.pid != player.id && player.CollidesWith(bullet))
                            {
                                bullet.vx = 0;
                                bullet.vy = 0;

                                for (int i = 0; i < player.bullets; i++)
                                {
                                    spawnedBullets.Add(new Bullet(player.x, player.y, player.id));
                                }

                                // TODO: kill player

                                // TODO: update scoreboard

                                // TODO: add status message to queue "X eliminated Y"

                                collision = true;
                                break;
                            }

                            if (!moving && player.CollidesWith(bullet))
                            {
                                player.bullets++;
                                pickedBullets.Add(bullet);
                            }
                        }
                        if (collision)
                        {
                            break;
                        }
                    }
                    if (collision)
                    {
                        break;
                    }
                }
            }

            // destroy picked bullets
            foreach (Bullet picked in pickedBullets)
            {
                cells[picked.col, picked.row].RemoveBullet(picked);
                bullets.Remove(picked);
            }

            // spawn dropped bullets
            bullets.AddRange(spawnedBullets);
        }

        public void Run()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            while (true)
            {
                //Console.WriteLine("Game is running...");

                // Calculate DeltaTime
                float dt = watch.ElapsedMilliseconds/1000.0f;
                watch.Restart();

                // TODO: remove disconnected players

                // Run game logic
                UpdatePlayers(dt);
                UpdateBullets(dt);

                // Send clients positional data for all game objects
                // ONLY SEND POSITIONAL DATA FOR OBJECTS WITHIN CLIENT'S REGION OF INTEREST
                // Send updates for if player scored a kill
                // Camera-Look will be disabled to allow for this
                // 
                /* Send:
                 *  - Positions of players/bullets in region of interest
                 *  - Scoreboard data
                 *  - State log (player X has killed player Y)
                 */
                foreach (Player p in clients.Values)
                {
                    // send player states
                    
                    // send bullet states

                    // sock.SendTo(data, p.EndPoint);
                }
                
                Thread.Sleep(1000);
            }
        }

        private void PlayerCollide(Player player)
        {
            float x = player.x;
            float y = player.y;
            float vx = player.vx;
            float vy = player.vy;
            float r = (float)Math.Atan2(vy, vx);
            float cx, cy, ccx, ccy;
            cx = (float)Math.Cos(r - Math.PI / 4);
            cy = (float)Math.Sin(r - Math.PI / 4);
            ccx = (float)Math.Cos(r + Math.PI / 4);
            ccy = (float)Math.Sin(r + Math.PI / 4);

            // collision booleans for...
            bool a, b; // ... counter, mid, clockwise (relative to velocity)
            a = level.Collision(x + ccx, y + ccy);
            b = level.Collision(x + cx, y + cy);

            if (a && b)
            {
                player.vx = 0;
                player.vy = 0;
            }
            else if (a)
            {
                player.vx = cx;
                player.vy = cy;
            }
            else if (b)
            {
                player.vx = ccx;
                player.vy = ccy;
            }
        }

        private void BulletCollide(Bullet bullet)
        {
            float x = bullet.x;
            float y = bullet.y;
            float vx = bullet.vx;
            float vy = bullet.vy;
            float r = (float)Math.Atan2(vy, vx);
            float cx, cy, ccx, ccy;
            cx = (float)Math.Cos(r - Math.PI / 4);
            cy = (float)Math.Sin(r - Math.PI / 4);
            ccx = (float)Math.Cos(r + Math.PI / 4);
            ccy = (float)Math.Sin(r + Math.PI / 4);

            // collision booleans for...
            bool b, a, c; // ... counter, mid, clockwise (relative to velocity)
            a = level.Collision(x + vx, y + vy);
            b = level.Collision(x + ccx * BULLET_SIZE, y + ccy * BULLET_SIZE);
            c = level.Collision(x + cx * BULLET_SIZE, y + cy * BULLET_SIZE);

            if (a || b || c)
            {
                bullet.vx = 0;
                bullet.vy = 0;
            }
        }

        private byte[] GetScene(int col, int row)
        {
            byte[] code = {100};
            byte[] delim = BitConverter.GetBytes(float.MaxValue);

            byte[] playerData = {};
            byte[] bulletData = { };

            for (int r = Math.Max(0, row - updateHeight); r < Math.Min(Cave.caveHeight, row + updateHeight); r++)
            {
                for (int c = Math.Max(0, col - updateWidth); c < Math.Min(Cave.caveWidth, col + updateWidth); c++)
                {
                    foreach (Player player in cells[c, r].GetPlayers())
                    {
                        byte[] data = player.GetBytes();
                        playerData = playerData.Concat(data).ToArray();
                    }

                    foreach (Bullet bullet in cells[c, r].GetBullets())
                    {
                        byte[] data = bullet.GetBytes();
                        bulletData = bulletData.Concat(data).ToArray();
                    }
                }
            }

            return code.Concat(playerData).Concat(delim).Concat(bulletData).ToArray();
        }
    }
}
