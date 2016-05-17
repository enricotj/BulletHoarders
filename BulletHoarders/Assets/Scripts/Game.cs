using UnityEngine;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System;

using Assets.Scripts;

public class Game : Singleton<Game> {

    protected Game() { }

    private CellType[,] level;

    private Socket sock;
    private EndPoint serverEndPoint;

    private string playerName;

    private Queue<byte[]> messages = new Queue<byte[]>();
    private object _queueLock = new object();

    private Sprite[] tileSprites;

    private GameObject tilePrefab;
    private GameObject playerPrefab;
    private GameObject bulletPrefab;

    public int playerId;

    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> bullets = new Dictionary<int, GameObject>();

	// Use this for initialization
	void Awake () {
        tilePrefab = (GameObject)Resources.Load("Prefabs/Tile", typeof(GameObject));
        playerPrefab = (GameObject)Resources.Load("Prefabs/Player", typeof(GameObject));
        bulletPrefab = (GameObject)Resources.Load("Prefabs/Bullet", typeof(GameObject));

        tileSprites = new Sprite[9];
        tileSprites[0] = (Sprite)Resources.Load("Sprites/Filled", typeof(Sprite));
        tileSprites[1] = (Sprite)Resources.Load("Sprites/NorthEast", typeof(Sprite));
        tileSprites[2] = (Sprite)Resources.Load("Sprites/NorthWest", typeof(Sprite));
        tileSprites[3] = (Sprite)Resources.Load("Sprites/SouthWest", typeof(Sprite));
        tileSprites[4] = (Sprite)Resources.Load("Sprites/SouthEast", typeof(Sprite));
        tileSprites[5] = (Sprite)Resources.Load("Sprites/East", typeof(Sprite));
        tileSprites[6] = (Sprite)Resources.Load("Sprites/North", typeof(Sprite));
        tileSprites[7] = (Sprite)Resources.Load("Sprites/West", typeof(Sprite));
        tileSprites[8] = (Sprite)Resources.Load("Sprites/South", typeof(Sprite));

        Debug.Log("GAME AWAKE");

        playerName = "PLAYER";

        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        IPAddress ip = IPAddress.Parse("127.0.0.1");
        serverEndPoint = new IPEndPoint(ip, 11200);
        byte[] handshake = new byte[sizeof(int) + playerName.Length * 2];

        byte[] portBytes = BitConverter.GetBytes(22000);
        byte[] nameBytes = Encoding.ASCII.GetBytes(playerName);

        int i = 0;
        foreach (byte b in portBytes)
        {
            handshake[i] = b;
            i++;
        }
        foreach (byte b in nameBytes)
        {
            handshake[i] = b;
            i++;
        }

        sock.SendTo(handshake, serverEndPoint);

        // begin receiving data from server
        UdpClient listener = new UdpClient(22000);
        listener.BeginReceive(new AsyncCallback(Receive), listener);
	}
	
	// Update is called once per frame
	void Update () {
        lock (_queueLock)
        {
            while (messages.Count > 0)
            {
                byte[] data = messages.Dequeue();
                if (level == null)
                {
                    playerId = BitConverter.ToInt32(data.SubArray(0, 4), 0);
                    int width = (int)data[4];
                    int height = (int)data[5];
                    Debug.Log("ID: " + playerId);
                    Debug.Log("Level Width: " + width);
                    Debug.Log("Level Height: " + height);
                    level = new CellType[width, height];
                    for (int r = 0; r < height; r++)
                    {
                        for (int c = 0; c < width; c++)
                        {
                            int i = r * width + c + 6;
                            level[c, r] = (CellType)data[i];

                            if (level[c, r] != CellType.Empty)
                            {
                                GameObject go = (GameObject)Instantiate(tilePrefab, new Vector3(c, r, 10), Quaternion.identity);
                                go.GetComponent<SpriteRenderer>().sprite = tileSprites[((int)level[c, r]) - 1];
                            }
                        }
                    }

                    Debug.Log("LEVEL DATA RECEIVED");

                    byte[] ready = { 255 };
                    sock.SendTo(ready, serverEndPoint);
                }
                else
                {
                    int code = data[0];
                    switch (code)
                    {
                        case 100:
                            int i = 1;

                            List<int> ids = new List<int>();
                            while(true)
                            {
                                // check for delim
                                float delimCheck = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                if (delimCheck == float.MaxValue)
                                {
                                    i += 4;
                                    break;
                                }

                                string name = Encoding.ASCII.GetString(data.SubArray(i, 32));
                                i += 32;
                                
                                int id = BitConverter.ToInt32(data.SubArray(i, 4), 0);
                                i += 4;
                                
                                float x = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;
                                float y = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;
                                
                                /*
                                float vx = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;
                                float vy = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;
                                */

                                float r = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;

                                if (!players.ContainsKey(id))
                                {
                                    GameObject player = (GameObject)Instantiate(playerPrefab, new Vector3(x, y, -10), Quaternion.identity);
                                    player.GetComponent<Player>().id = id;
                                    players.Add(id, player);
                                }
                                else
                                {
                                    Player player = players[id].GetComponent<Player>();
                                    player.transform.position = new Vector3(x, y, player.transform.position.z);
                                }

                                ids.Add(id);
                            }

                            // destroy any players that are no longer present
                            List<int> destroy = new List<int>();
                            foreach (int id in players.Keys)
                            {
                                if (!ids.Contains(id))
                                {
                                    destroy.Add(id);
                                }
                            }
                            foreach (int id in destroy)
                            {
                                Destroy(players[id]);
                                players.Remove(id);
                            }

                            ids.Clear();
                            destroy.Clear();
                            
                            while (i < data.Length)
                            {
                                int id = BitConverter.ToInt32(data.SubArray(i, 4), 0);
                                i += 4;

                                float x = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;
                                float y = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;

                                
                                float vx = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;
                                float vy = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                i += 4;
                                
                                if (!bullets.ContainsKey(id))
                                {
                                    GameObject bullet = (GameObject)Instantiate(bulletPrefab, new Vector3(x, y, -20), Quaternion.identity);
                                    bullets.Add(id, bullet);
                                    Debug.Log("BULLET");
                                }
                                else
                                {
                                    Bullet bullet = bullets[id].GetComponent<Bullet>();
                                    bullet.transform.position = new Vector3(x, y, bullet.transform.position.z);
                                }
                                
                                ids.Add(id);
                            }
                            
                            foreach (int id in bullets.Keys)
                            {
                                if (!ids.Contains(id))
                                {
                                    destroy.Add(id);
                                }
                            }
                            foreach (int id in destroy)
                            {
                                Debug.Log("Bullet Destroyed: " + id);
                                Destroy(bullets[id]);
                                bullets.Remove(id);
                            }
                            break;
                    }
                }
            }
        }
	}

    private void Receive(IAsyncResult result)
    {
        lock (_queueLock)
        {
            UdpClient client = result.AsyncState as UdpClient;
            IPEndPoint source = new IPEndPoint(0, 0);
            byte[] data = client.EndReceive(result, ref source);
            messages.Enqueue(data);

            client.BeginReceive(new AsyncCallback(Receive), client);
        }
    }

    public void Send(float rot)
    {
        byte[] data = BitConverter.GetBytes(rot);
        byte[] cmd = { (byte)50 };
        data = cmd.Concat(data).ToArray();
        sock.SendTo(data, serverEndPoint);
    }

    public void Send(Command cmd)
    {
        byte[] data = { (byte)cmd };
        sock.SendTo(data, serverEndPoint);
    }
}

public enum Command
{
    LeftPress = 0,
    LeftRelease = 100,
    RightPress = 10,
    RightRelease = 110,
    UpPress = 20,
    UpRelease = 120,
    DownPress = 30,
    DownRelease = 130,
    Shoot = 40,
    Rotate = 50
}

public enum CellType
{
    Empty = 0,
    Filled = 1,
    SlantNE = 2,
    SlantNW = 3,
    SlantSW = 4,
    SlantSE = 5,
    EdgeE = 6,
    EdgeN = 7,
    EdgeW = 8,
    EdgeS = 9
}