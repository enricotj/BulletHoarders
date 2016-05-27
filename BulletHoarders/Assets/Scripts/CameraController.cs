using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

    private string pname = "";

	// Use this for initialization
	void Start () {
        pname = Game.Instance.playerName;
	}

    void OnGUI()
    {
        if (!Game.Instance.connected)
        {
            GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperCenter;

            if (!Game.Instance.ready)
            {
                bool connect = false;
                if (Event.current.Equals(Event.KeyboardEvent("return")))
                {
                    connect = true;
                }

                pname = GUI.TextField(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 70, 200, 32), pname, 16);
                if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 - 16, 100, 32), "CONNECT") || connect)
                {
                    Game.Instance.Connect(pname);
                }
            }
            
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 50, 400, 50), Game.Instance.status, centeredStyle);
        }
    }
}
