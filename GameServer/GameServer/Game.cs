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
    public class Game
    {
        private float maxDt = 0;

        private Queue<Message> messages;
        private object _queueLock = new object();

        private int updateWidth = 12;
        private int updateHeight = 7;

        private Dictionary<string, Player> clients;
        private List<Bullet> bullets;

        private Player winner = null;

        private Socket sock;

        private const int MAX_PLAYERS = 50;
        private int nextPlayerId = 0;
        private int nextBulletId = 0;

        private Cave level;
        private Cell[,] cells;

        public static float ANGLE = (float)Math.Sqrt(2)/2;

        private const float PLAYER_SPEED = 10f;
        private const float BULLET_SPEED = 20.0f;
        public static float BULLET_SIZE = 0.125f;

        private List<Bullet> newBullets, stopBullets, destroyBullets;

        private bool[] playerIds = new bool[255];
        UdpClient listener;

        public Game()
        {
            Init();
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // open up a port for listening
            listener = new UdpClient(11200, AddressFamily.InterNetwork);
            listener.BeginReceive(new AsyncCallback(Receive), listener);

            Console.WriteLine("INITIALIZATION COMPLETE");
        }

        private void Receive(IAsyncResult result)
        {
            lock (_queueLock)
            {
                IPEndPoint source = new IPEndPoint(IPAddress.Any, 12000);
                byte[] data = listener.EndReceive(result, ref source);

                Message msg = new Message(source.Address.ToString(), data);
                msg.endPoint = source;

                messages.Enqueue(msg);
                //Console.WriteLine("DATA RECEIVED");

                listener.BeginReceive(new AsyncCallback(Receive), listener);
            }
        }

        private void HandleMessage(Message msg)
        {
            string source = msg.source;
            byte[] data = msg.data;

            if (!clients.ContainsKey(source) && data.Length >= sizeof(int) && data[0] == 254)
            {
                if (clients.Keys.Count >= 40)
                {
                    byte[] portData = data.SubArray(1, sizeof(int));
                    int clientPort = BitConverter.ToInt32(portData, 0);
                    IPEndPoint cep = new IPEndPoint(IPAddress.Parse(source), clientPort);

                    byte[] full = { 0, 0, 0 };

                    sock.SendTo(full, cep);
                }
                else
                {
                    // add client to list
                    byte[] portData = data.SubArray(1, sizeof(int));
                    int clientPort = BitConverter.ToInt32(portData, 0);
                    IPEndPoint cep = new IPEndPoint(IPAddress.Parse(source), clientPort);
                    Console.WriteLine(clientPort);

                    // get player name
                    byte[] nameData = data.SubArray(sizeof(int) + 1);
                    string name = Encoding.ASCII.GetString(nameData);
                    Console.WriteLine(name);

                    Player player = new Player(name, cep, (byte)nextPlayerId);
                    player.source = source;
                    clients.Add(source, player);

                    // send initial level data
                    byte[] initData = { (byte)nextPlayerId };
                    initData = initData.Concat(level.GetBytes()).ToArray();

                    // SEND
                    sock.SendTo(initData.SubArray(0, 3), cep);

                    playerIds[nextPlayerId] = true;
                    for (int i = 0; i < playerIds.Length; i++)
                    {
                        if (!playerIds[i])
                        {
                            nextPlayerId = i;
                        }
                    }
                }
            }
            else if (clients.ContainsKey(source))
            {
                Player player = clients[source];

                if (!player.init && data[0] != 255)
                {
                    player.levelProgress = data[0];
                    Console.WriteLine(player.levelProgress);
                    sock.SendTo(level.GetBytes().SubArray(player.levelProgress * 100 + 2, 100), player.EndPoint);
                }
                else if (!player.init && data[0] == 255)
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
                    byte[] scene = GetInitData();
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
                            break;
                        case 10:
                            player.right = true;
                            break;
                        case 20:
                            player.up = true;
                            break;
                        case 30:
                            player.down = true;
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

                        case 50:
                            // update rotation
                            player.r = BitConverter.ToSingle(data, 1);
                            break;

                        // player disconnected
                        case 200:
                            Player disc = clients[source];
                            playerIds[disc.id] = false;
                            Console.WriteLine("{0}:{1} disconnected", disc.source, disc.name);

                            cells[disc.col, disc.row].RemovePlayer(disc.id);
                            clients.Remove(source);
                            break;
                    }
                }
            }

        }

        private void UpdatePlayers(float dt)
        {
            List<Bullet> spawnedBullets = new List<Bullet>();
            foreach (Player player in clients.Values)
            {
                if (!player.init)
                {
                    continue;
                }

                if (player.invTime > 0)
                {
                    player.invTime -= dt;
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
                    if (player.bullets > 0 && player.invTime <= 0)
                    {
                        Bullet bullet = new Bullet(player.x, player.y, player.id);
                        bullet.vx = (float)Math.Cos(player.r * Math.PI / 180);
                        bullet.vy = (float)Math.Sin(player.r * Math.PI / 180);
                        bullet.id = nextBulletId;
                        nextBulletId++;
                        spawnedBullets.Add(bullet);
                        player.bullets--;
                    }
                    player.shoot = false;
                }
            }

            newBullets.AddRange(spawnedBullets);
            bullets.AddRange(spawnedBullets);
        }

        private void UpdateBullets(float dt)
        {
            List<Bullet> spawnedBullets = new List<Bullet>();
            List<Bullet> pickedBullets = new List<Bullet>();
            List<Player> dead = new List<Player>();

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
                        if (c >= cells.GetLength(0) || r >= cells.GetLength(1) || c < 0 || r < 0)
                        {
                            continue;
                        }
                        foreach (Player player in cells[c, r].GetPlayers())
                        {
                            if (player.invTime <= 0 && moving && bullet.pid != player.id && player.CollidesWith(bullet))
                            {
                                bullet.vx = 0;
                                bullet.vy = 0;

                                if (!stopBullets.Contains(bullet))
                                {
                                    stopBullets.Add(bullet);
                                }

                                for (int i = 0; i < player.bullets; i++)
                                {
                                    Bullet b = new Bullet(player.x, player.y, player.id);
                                    b.id = nextBulletId;
                                    nextBulletId++;
                                    spawnedBullets.Add(b);
                                }

                                // Kill Player
                                dead.Add(player);

                                collision = true;
                                break;
                            }

                            if (!moving && player.CollidesWith(bullet))
                            {
                                player.bullets++;

                                if (player.bullets >= 100)
                                {
                                    winner = player;
                                }

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

            // destroy killed players
            foreach (Player player in dead)
            {
                cells[player.col, player.row].RemovePlayer(player.id);

                byte[] data = { 222 };
                sock.SendTo(data, player.EndPoint);
                clients.Remove(player.source);
            }

            // destroy picked bullets
            foreach (Bullet picked in pickedBullets)
            {
                cells[picked.col, picked.row].RemoveBullet(picked);
                bullets.Remove(picked);
                destroyBullets.Add(picked);
            }

            // spawn dropped bullets
            newBullets.AddRange(spawnedBullets);

            bullets.AddRange(spawnedBullets);
        }

        public void Run()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Stopwatch broadcastTimer = new Stopwatch();
            broadcastTimer.Start();

            while (true)
            {
                // Handle all received messages
                lock (_queueLock)
                {
                    while (messages.Count > 0)
                    {
                        HandleMessage(messages.Dequeue());
                    }
                }
                // Calculate DeltaTime
                float dt = (float) watch.Elapsed.TotalMilliseconds / 1000;
                watch.Restart();

                // Run game logic
                UpdatePlayers(dt);
                UpdateBullets(dt);

                // Send clients positional data for all game objects
                if (winner == null)
                {
                    long bdt = broadcastTimer.ElapsedMilliseconds;
                    if (bdt > 40)
                    {
                        byte[] data = GetAllData();
                        foreach (Player p in clients.Values)
                        {
                            sock.SendTo(data, p.EndPoint);
                        }
                        broadcastTimer.Restart();
                        newBullets.Clear();
                        stopBullets.Clear();
                        destroyBullets.Clear();
                    }
                }
                else
                {
                    byte[] data = {55};
                    data = data.Concat(winner.GetNameBytes()).ToArray();
                    foreach (Player p in clients.Values)
                    {
                        if (p.EndPoint.Equals(winner.EndPoint))
                        {
                            byte[] win = { 66 };
                            win = win.Concat(winner.GetNameBytes()).ToArray();
                            sock.SendTo(win, p.EndPoint);
                        }
                        else
                        {
                            sock.SendTo(data, p.EndPoint);
                        }
                    }
                    Reset();
                }

                Thread.Sleep(1);
            }
        }

        private void PlayerCollide(Player player)
        {
            float x = player.x;
            float y = player.y;
            float vx = player.vx;
            float vy = player.vy;
            if (vx == 0 && vy == 0)
            {
                return;
            }
            float r = (float)Math.Atan2(vy, vx);
            float cx, cy, ccx, ccy;
            cx = (float)Math.Cos(r - Math.PI / 4);
            cy = (float)Math.Sin(r - Math.PI / 4);
            ccx = (float)Math.Cos(r + Math.PI / 4);
            ccy = (float)Math.Sin(r + Math.PI / 4);

            // collision booleans for...
            bool a, b; // ... counter, mid, clockwise (relative to velocity)
            a = level.Collision(x + ccx * 0.5f, y + ccy * 0.5f);
            b = level.Collision(x + cx * 0.5f, y + cy * 0.5f);

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
            if (vx == 0 && vy == 0)
            {
                return;
            }
            float r = (float)Math.Atan2(vy, vx);
            float cx, cy, ccx, ccy;
            cx = (float)Math.Cos(r - Math.PI / 4);
            cy = (float)Math.Sin(r - Math.PI / 4);
            ccx = (float)Math.Cos(r + Math.PI / 4);
            ccy = (float)Math.Sin(r + Math.PI / 4);

            // collision booleans for...
            bool b, a, c; // ... counter, mid, clockwise (relative to velocity)
            a = level.Collision(x + vx * BULLET_SIZE, y + vy * BULLET_SIZE);
            b = level.Collision(x + ccx * BULLET_SIZE, y + ccy * BULLET_SIZE);
            c = level.Collision(x + cx * BULLET_SIZE, y + cy * BULLET_SIZE);

            if (a || b || c)
            {
                bullet.vx = 0;
                bullet.vy = 0;
                stopBullets.Add(bullet);
            }
        }

        private byte[] GetScene(int col, int row)
        {
            byte[] code = {111};
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

        private byte[] GetAllData()
        {
            byte[] code = { 111 };
            byte[] delim = BitConverter.GetBytes(float.MaxValue);

            byte[] playerData = { };
            byte[] newBulletData = { };
            byte[] stopBulletData = { };
            byte[] destroyBulletData = { };

            for (int r = 0; r < Cave.caveHeight; r++)
            {
                for (int c = 0; c < Cave.caveWidth; c++)
                {
                    foreach (Player player in cells[c, r].GetPlayers())
                    {
                        byte[] data = player.GetBytes();
                        playerData = playerData.Concat(data).ToArray();
                    }
                }
            }

            foreach (Bullet b in newBullets)
	        {
                byte[] data = b.GetBytes();
                newBulletData = newBulletData.Concat(data).ToArray();
	        }

            foreach (Bullet b in stopBullets)
            {
                byte[] data = b.GetStopBytes();
                stopBulletData = stopBulletData.Concat(data).ToArray();
            }

            foreach (Bullet b in destroyBullets)
            {
                byte[] data = b.GetDestroyBytes();
                destroyBulletData = destroyBulletData.Concat(data).ToArray();
            }

            return code.Concat(playerData).Concat(delim).Concat(newBulletData).Concat(delim).Concat(stopBulletData).Concat(delim).Concat(destroyBulletData).ToArray();
        }

        private byte[] GetInitData()
        {
            byte[] code = { 111 };
            byte[] delim = BitConverter.GetBytes(float.MaxValue);

            byte[] playerData = { };
            byte[] newBulletData = { };
            byte[] stopBulletData = { };
            byte[] destroyBulletData = { };

            for (int r = 0; r < Cave.caveHeight; r++)
            {
                for (int c = 0; c < Cave.caveWidth; c++)
                {
                    foreach (Player player in cells[c, r].GetPlayers())
                    {
                        byte[] data = player.GetBytes();
                        playerData = playerData.Concat(data).ToArray();
                    }
                }
            }

            foreach (Bullet b in bullets)
            {
                byte[] data = b.GetBytes();
                newBulletData = newBulletData.Concat(data).ToArray();
            }

            return code.Concat(playerData).Concat(delim).Concat(newBulletData).ToArray();
        }

        private void Reset()
        {
            Console.WriteLine("THE SERVER HAS BEEN RESET");
            Init();
        }

        private void Init()
        {
            winner = null;
            playerIds = new bool[255];
            messages = new Queue<Message>();
            clients = new Dictionary<string, Player>();

            bullets = new List<Bullet>();
            newBullets = new List<Bullet>();
            stopBullets = new List<Bullet>();
            destroyBullets = new List<Bullet>();

            // Generate Level
            level = new Cave();
            int dataSize = level.GetBytes().Length;
            Console.WriteLine("Number of Level Bytes: " + dataSize);

            cells = new Cell[Cave.caveWidth, Cave.caveHeight];
            for (int c = 0; c < Cave.caveWidth; c++)
            {
                for (int r = 0; r < Cave.caveHeight; r++)
                {
                    cells[c, r] = new Cell();
                }
            }
        }

    }
}
