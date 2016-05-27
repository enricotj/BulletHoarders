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

    public string playerName = "";

    private Queue<byte[]> messages = new Queue<byte[]>();
    private object _queueLock = new object();

    private Sprite[] tileSprites;

    private GameObject tilePrefab;
    private GameObject playerPrefab;
    private GameObject bulletPrefab;

    private GameObject labelPrefab;

    public int playerId;

    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> bullets = new Dictionary<int, GameObject>();
    private Dictionary<int, Color> colors = new Dictionary<int, Color>();

    private const string HOST_A = "137.112.155.138";
    private const string HOST_B = "23.96.182.222";
    private const string LOCAL = "127.0.0.1";

    private UdpClient listener;

    public bool ready = false;
    public bool connected = false;
    private int levelProgress = -1;
    private int levelPackets = 0;
    private int levelWidth = 0;
    private int levelHeight = 0;

    public string status = "Type in a username and connect!";

	// Use this for initialization
	public void Connect (string playerName) {
        levelWidth = 0;
        levelHeight = 0;
        levelProgress = -1;
        levelPackets = 0;

        ready = false;
        connected = false;
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

        if (sock == null)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Debug.Log("SOCKET INITIALIZED");
        }

        IPAddress ip = IPAddress.Parse(HOST_A);
        serverEndPoint = new IPEndPoint(ip, 11200);
        byte[] handshake = new byte[sizeof(int) + playerName.Length * 2 + 1];

        byte[] portBytes = BitConverter.GetBytes(22000);
        byte[] nameBytes = Encoding.ASCII.GetBytes(playerName);

        handshake[0] = 254;

        int i = 1;
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

        if (listener == null)
        {
            listener = new UdpClient(22000, AddressFamily.InterNetwork);
            listener.BeginReceive(new AsyncCallback(Receive), listener);
            Debug.Log("LISTENING");
        }

        ready = true;
        status = "Loading...";
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
        if (ready)
        {
            lock (_queueLock)
            {
                while (messages.Count > 0)
                {
                    byte[] data = messages.Dequeue();
                    if (levelProgress < levelPackets)
                    {
                        #region Receive Level Data
                        if (levelProgress < 0)
                        {
                            playerId = data[0];
                            levelWidth = (int)data[1];
                            levelHeight = (int)data[2];
                            Debug.Log("ID: " + playerId);
                            Debug.Log("Level Width: " + levelWidth);
                            Debug.Log("Level Height: " + levelHeight);
                            level = new CellType[levelWidth, levelHeight];
                            if (playerId == 0 && levelWidth == 0 && levelHeight == 0)
                            {
                                // put on waitlist
                                status = "SERVER IS FULL: PLEASE TRY AGAIN LATER";
                            }
                            else
                            {
                                levelPackets = levelHeight;
                                levelProgress++;
                                byte[] ack = { (byte)levelProgress };
                                sock.SendTo(ack, serverEndPoint);
                            }
                        }
                        else if (data.Length == levelWidth)
                        {
                            for (int i = 0; i < levelWidth; i++)
                            {
                                level[i, levelProgress] = (CellType)data[i];
                            }
                            levelProgress++;

                            Debug.Log("LEVEL DATA RECEIVED: " + levelProgress + "/" + levelPackets);

                            if (levelProgress >= levelPackets)
                            {
                                for (int r = 0; r < levelHeight; r++)
                                {
                                    for (int c = 0; c < levelWidth; c++)
                                    {
                                        if (level[c, r] != CellType.Empty)
                                        {
                                            GameObject go = (GameObject)Instantiate(tilePrefab, new Vector3(c, r, 10), Quaternion.identity);
                                            go.GetComponent<SpriteRenderer>().sprite = tileSprites[((int)level[c, r]) - 1];
                                        }
                                    }
                                }

                                byte[] ack = { 255 };
                                sock.SendTo(ack, serverEndPoint);

                                connected = true;

                                Debug.Log("LEVEL SUCCESSFULLY LOADED");
                            }
                            else
                            {
                                byte[] ack = { (byte)levelProgress };
                                sock.SendTo(ack, serverEndPoint);
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        int code = data[0];
                        switch (code)
                        {
                            case 111:
                                //Debug.Log("SCENE DATA RECEIVED");
                                int i = 1;
                                List<int> ids = new List<int>();
                                while (true)
                                {
                                    // check for delim
                                    if (BitConverter.ToSingle(data.SubArray(i, 4), 0) == float.MaxValue)
                                    {
                                        i += 4;
                                        break;
                                    }

                                    string name = Encoding.ASCII.GetString(data.SubArray(i, 32));
                                    i += 32;

                                    int id = data[i];
                                    i += 1;

                                    float x = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                    i += 4;
                                    float y = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                    i += 4;

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
                                        colors.Add(id, Color.HSVToRGB(UnityEngine.Random.value, 1, UnityEngine.Random.value * 0.3f + 0.7f));
                                        player.GetComponent<SpriteRenderer>().color = colors[id];
                                        player.GetComponent<Player>().target = player.transform.position;

                                        GameObject label = (GameObject)GameObject.Instantiate(labelPrefab);
                                        player.GetComponent<Player>().label = label;
                                        label.GetComponent<TextMesh>().text = name;
                                        label.GetComponent<TextMesh>().color = colors[id];

                                        Debug.Log("PLAYER NEW " + name);
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
                                    Debug.Log("PLAYER DESTROY " + player.name);
                                }

                                ids.Clear();
                                destroy.Clear();

                                // NEW BULLETS
                                while (true)
                                {
                                    // check for delim
                                    if (i + 4 >= data.Length || BitConverter.ToSingle(data.SubArray(i, 4), 0) == float.MaxValue)
                                    {
                                        i += 4;
                                        break;
                                    }

                                    int id = BitConverter.ToInt32(data.SubArray(i, 4), 0);
                                    i += 4;
                                    int pid = data[i];
                                    i += 1;

                                    float x = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                    i += 4;
                                    float y = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                    i += 4;
                                    float vx = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                    i += 4;
                                    float vy = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                    i += 4;
                                    GameObject bullet = (GameObject)Instantiate(bulletPrefab, new Vector3(x, y, -20), Quaternion.identity);
                                    try
                                    {
                                        bullet.GetComponent<SpriteRenderer>().color = colors[pid];
                                    }
                                    catch (KeyNotFoundException e)
                                    {
                                        bullet.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(UnityEngine.Random.value, 1, UnityEngine.Random.value * 0.3f + 0.7f);
                                    }
                                    bullets.Add(id, bullet);
                                    bullet.GetComponent<Bullet>().velocity = new Vector3(vx, vy, 0);
                                    Debug.Log("BULLET NEW " + id);
                                }

                                // STOP BULLETS
                                while (true)
                                {
                                    // check for delim
                                    if (i + 4 >= data.Length || BitConverter.ToSingle(data.SubArray(i, 4), 0) == float.MaxValue)
                                    {
                                        i += 4;
                                        break;
                                    }

                                    int id = BitConverter.ToInt32(data.SubArray(i, 4), 0);
                                    i += 4;
                                    float x = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                    i += 4;
                                    float y = BitConverter.ToSingle(data.SubArray(i, 4), 0);
                                    i += 4;

                                    GameObject bullet = bullets[id];
                                    bullet.GetComponent<Bullet>().velocity = new Vector3(0, 0, 0);
                                    bullet.transform.position = new Vector3(x, y, bullet.transform.position.z);
                                    Debug.Log("BULLET STOP " + id);
                                }

                                // DESTROY BULLETS
                                while (i < data.Length)
                                {
                                    int id = BitConverter.ToInt32(data.SubArray(i, 4), 0);
                                    i += 4;
                                    Bullet bullet = bullets[id].GetComponent<Bullet>();
                                    GameObject particle = (GameObject)Instantiate(
                                        (GameObject)Resources.Load("Prefabs/Pickup", typeof(GameObject)),
                                        bullet.transform.position, Quaternion.identity);
                                    particle.GetComponent<ParticleSystem>().startColor = bullets[id].GetComponent<SpriteRenderer>().color;
                                    Destroy(bullets[id]);
                                    bullets.Remove(id);
                                    Debug.Log("BULLET DESTROY " + id);
                                }
                                break;
                            case 222:
                                // you have died
                                status = "You exploded into tiny bits.";
                                Restart();
                                break;

                            case 55:
                                // someone has won
                                string winner = Encoding.ASCII.GetString(data.SubArray(1, 32));
                                status = winner + " has won.";
                                Restart();
                                break;

                            case 66:
                                // you have won
                                status = "YOU WON!";
                                Restart();
                                break;
                        }
                    }
                }
            }
        }
	}

    private void Restart()
    {
        connected = false;
        listener.Close();
        sock.Close();
        listener = null;
        sock = null;
        Application.LoadLevel(0);
    }

    private void Receive(IAsyncResult result)
    {
        lock (_queueLock)
        {
            Debug.Log("DATA RECEIVED...");
            UdpClient client = result.AsyncState as UdpClient;
            IPEndPoint source = new IPEndPoint(IPAddress.Any, 22000);
            byte[] data = listener.EndReceive(result, ref source);
            messages.Enqueue(data);
            Debug.Log(source.Address + ":" + source.Port);
            Debug.Log("packet size: " + data.Length);
            foreach (byte b in data)
            {
                Debug.Log(b);
            }

            listener.BeginReceive(new AsyncCallback(Receive), listener);
        }
    }

    public void Send(float rot)
    {
        if (sock != null)
        {
            byte[] data = BitConverter.GetBytes(rot);
            byte[] cmd = { (byte)50 };
            data = cmd.Concat(data).ToArray();
            sock.SendTo(data, serverEndPoint);
        }
    }

    public void Send(Command cmd)
    {
        if (sock != null)
        {
            byte[] data = { (byte)cmd };
            sock.SendTo(data, serverEndPoint);
        }
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
            const float w = 256;
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