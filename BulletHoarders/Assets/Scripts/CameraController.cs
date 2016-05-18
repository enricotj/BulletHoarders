using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

    public bool connected = false;

    private string pname = "PlayerName";

	// Use this for initialization
	void Start () {
        pname = Game.Instance.playerName;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        if (!connected)
        {
            pname = GUI.TextField(new Rect(10, 10, 200, 32), pname, 16);
            if (GUI.Button(new Rect(10, 64, 96, 32), "CONNECT"))
            {
                Game.Instance.Connect(pname);
                connected = true;
            }
        }
    }
}
