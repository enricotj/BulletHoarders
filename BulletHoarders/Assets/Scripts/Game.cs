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

    public string playerName;

    private Queue<byte[]> messages = new Queue<byte[]>();
    private object _queueLock = new object();

    private object _scorelock = new object();

    private Sprite[] tileSprites;

    private GameObject tilePrefab;
    private GameObject playerPrefab;
    private GameObject bulletPrefab;

    private GameObject labelPrefab;

    public int playerId;

    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> bullets = new Dictionary<int, GameObject>();
    private Dictionary<int, Color> colors = new Dictionary<int, Color>();

    private const string HOST = "137.112.155.138";
    private const string LOCAL = "127.0.0.1";

    private UdpClient listener;

    private bool connected = false;

	// Use this for initialization
	public void Connect (string playerName) {
        level = null;

        messages = new Queue<byte[]>();
        players = new Dictionary<int, GameObject>();
        bullets = new Dictionary<int, GameObject>();
        colors = new Dictionary<int, Color>();

        this.playerName = playerName;

        tilePrefab = (GameObject)Resources.Load("Prefabs/Tile", typeof(GameObject));
        playerPrefab = (GameObject)Resources.Load("Prefabs/Player", typeof(GameObject));
        bulletPrefab = (GameObject)Resources.Load("Prefabs/Bullet", typeof(GameObject));
        labelPrefab = (GameObject)Resources.Load("Prefabs/Label", typeof(GameObject));

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

        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        IPAddress ip = IPAddress.Parse(HOST);
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

        listener = new UdpClient(22000);
        listener.BeginReceive(new AsyncCallback(Receive), listener);

        connected = true;
	}
	
	// Update is called once per frame
	void Update () {
        if (connected)
        {
            lock (_queueLock)
            {
                if (Input.GetKey("escape"))
                {
                    Application.Quit();
                }

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
                            case 111:
                                int i = 1;

                                List<int> ids = new List<int>();
                                while (true)
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

                                    int n = BitConverter.ToInt32(data.SubArray(i, 4), 0);
                                    i += 4;

                                    if (!players.ContainsKey(id))
                                    {
                                        GameObject player = (GameObject)Instantiate(playerPrefab, new Vector3(x, y, -10), Quaternion.identity);
                                        player.GetComponent<Player>().id = id;
                                        player.GetComponent<Player>().username = name;
                                        players.Add(id, player);
                                        colors.Add(id, Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.value * 0.3f + 0.7f, 1));
                                        player.GetComponent<SpriteRenderer>().color = colors[id];
                                        player.GetComponent<Player>().target = player.transform.position;

                                        GameObject label = (GameObject)GameObject.Instantiate(labelPrefab);
                                        player.GetComponent<Player>().label = label;
                                        label.GetComponent<TextMesh>().text = name;
                                    }
                                    else
                                    {
                                        Player player = players[id].GetComponent<Player>();
                                        player.target = new Vector3(x, y, player.transform.position.z);
                                        player.transform.rotation = Quaternion.Euler(0, 0, r);
                                        player.bullets = n;
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
                                    Player player = players[id].GetComponent<Player>();
                                    GameObject particle = (GameObject)Instantiate(
                                        (GameObject)Resources.Load("Prefabs/Explosion", typeof(GameObject)),
                                        player.transform.position, Quaternion.identity);
                                    particle.GetComponent<ParticleSystem>().startColor = players[id].GetComponent<SpriteRenderer>().color;
                                    Destroy(players[id]);
                                    players.Remove(id);
                                }

                                ids.Clear();
                                destroy.Clear();

                                while (i < data.Length)
                                {
                                    int id = BitConverter.ToInt32(data.SubArray(i, 4), 0);
                                    i += 4;

                                    int pid = BitConverter.ToInt32(data.SubArray(i, 4), 0);
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
                                        try
                                        {
                                            bullet.GetComponent<SpriteRenderer>().color = colors[pid];
                                        }
                                        catch (KeyNotFoundException e)
                                        {

                                        }
                                        bullets.Add(id, bullet);
                                        bullet.GetComponent<Bullet>().target = bullet.transform.position;
                                    }
                                    else
                                    {
                                        Bullet bullet = bullets[id].GetComponent<Bullet>();
                                        bullet.target = new Vector3(x, y, bullet.transform.position.z);
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
                                    Bullet bullet = bullets[id].GetComponent<Bullet>();
                                    GameObject particle = (GameObject)Instantiate(
                                        (GameObject)Resources.Load("Prefabs/Pickup", typeof(GameObject)),
                                        bullet.transform.position, Quaternion.identity);
                                    particle.GetComponent<ParticleSystem>().startColor = bullets[id].GetComponent<SpriteRenderer>().color;
                                    Destroy(bullets[id]);
                                    bullets.Remove(id);
                                }
                                break;
                            case 222:
                                connected = false;
                                listener.Close();
                                sock.Close();
                                Application.LoadLevel(0);
                                break;
                        }
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

    void OnApplicationQuit()
    {
        byte[] data = { (byte)Command.Disconnect };
        sock.SendTo(data, serverEndPoint);
        sock.Close();

        listener.Close();
    }

    void OnGUI()
    {
        if (connected)
        {
            Dictionary<int, int> playerScores = new Dictionary<int, int>();
            List<string> usernames = new List<string>();

            foreach (GameObject go in players.Values)
            {
                Player player = go.GetComponent<Player>();

                usernames.Add(player.username);
                playerScores.Add(usernames.Count - 1, player.bullets);
            }

            List<KeyValuePair<int, int>> scoreboard = playerScores.ToList();
            scoreboard.Sort((First, Second) =>
            {
                return Second.Value.CompareTo(First.Value);
            });

            const float x = 10;
            const float w = 128;
            const float h = 20;
            int i = 0;
            foreach (var item in scoreboard)
            {
                int num = i + 1;
                string text = num + ". " + usernames[item.Key] + " : " + item.Value;
                GUI.Label(new Rect(x, (i + 1) * h, w, h), text);
                i++;
            }
        }
    }
}

public class DuplicateKeyComparer<TKey>
                :
             IComparer<TKey> where TKey : IComparable
{
    #region IComparer<TKey> Members

    public int Compare(TKey x, TKey y)
    {
        int result = x.CompareTo(y);

        if (result == 0)
            return 1;   // Handle equality as beeing greater
        else
            return result;
    }

    #endregion
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
    Rotate = 50,
    Disconnect = 200
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