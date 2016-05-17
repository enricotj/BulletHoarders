using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    public int id;

    private GameObject cam;

    private float prot = 0;

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (id == Game.Instance.playerId)
        {
            cam = GameObject.Find("Main Camera");
            cam.transform.position = new Vector3(transform.position.x, transform.position.y, cam.transform.position.z);

            // mouse aim
            Vector2 positionOnScreen = cam.GetComponent<Camera>().WorldToViewportPoint(transform.position);
            Vector2 mouseOnScreen = (Vector2)cam.GetComponent<Camera>().ScreenToViewportPoint(Input.mousePosition);
            float angle = AngleBetweenTwoPoints(positionOnScreen, mouseOnScreen) + 180;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            bool sentRot = false;
            float rot = transform.rotation.eulerAngles.z;
            if (Mathf.Abs(rot - prot) > Mathf.PI / 8)
            {
                Game.Instance.Send(rot);
                prot = rot;
                sentRot = true;
            }

            // movement
            if (Input.GetKeyDown(KeyCode.A))
            {
                Game.Instance.Send(Command.LeftPress);
            }
            if (Input.GetKeyUp(KeyCode.A))
            {
                Game.Instance.Send(Command.LeftRelease);
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                Game.Instance.Send(Command.RightPress);
            }
            if (Input.GetKeyUp(KeyCode.D))
            {
                Game.Instance.Send(Command.RightRelease);
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                Game.Instance.Send(Command.UpPress);
            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                Game.Instance.Send(Command.UpRelease);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Game.Instance.Send(Command.DownPress);
            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                Game.Instance.Send(Command.DownRelease);
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (!sentRot)
                {
                    Game.Instance.Send(rot);
                }
                Game.Instance.Send(Command.Shoot);
            }
        }
	}

    private float AngleBetweenTwoPoints(Vector3 a, Vector3 b)
    {
        return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
    }
}
