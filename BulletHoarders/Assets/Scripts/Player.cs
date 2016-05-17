using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    public int id;

    public string name;

    private GameObject cam;

    private float prot = 0;

    public Vector3 target = new Vector3();

    private float maxSpeed = 10.0f;

    public GameObject label;

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update ()
    {
        Vector3 dir = (target - transform.position).normalized;

        if (Vector3.Distance(transform.position, target) < maxSpeed * Time.deltaTime)
        {
            transform.position = target;
        }
        else
        {
            transform.position += dir * maxSpeed * Time.deltaTime;
        }

        label.transform.position = this.transform.position - new Vector3(0, -0.35f, 1);

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
            if (Mathf.Abs(rot - prot) > 5)
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

    void OnDestroy()
    {
        Destroy(label);
    }

    private float AngleBetweenTwoPoints(Vector3 a, Vector3 b)
    {
        return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
    }
}
