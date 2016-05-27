using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    public int id;

    public string username;

    private GameObject cam;

    private float prot = 0;

    public Vector3 target = new Vector3();

    private float maxSpeed = 10.0f;

    public GameObject label;

    private float invTime = 2f;
    

    private bool init = false;

    private Color color, invColor;

    public int bullets = 5;

	// Use this for initialization
	void Start () {
        GameObject particle = (GameObject)Instantiate(
            (GameObject)Resources.Load("Prefabs/Spawn", typeof(GameObject)),
            transform.position, Quaternion.identity);
        particle.GetComponent<ParticleSystem>().startColor = GetComponent<SpriteRenderer>().color;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (!init)
        {
            color = GetComponent<SpriteRenderer>().color;
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            invColor = Color.white;
            init = true;
        }
        
        if (invTime > 0)
        {
            if (((int)invTime * 10) % 2 == 1)
            {
                GetComponent<SpriteRenderer>().color = new Color(invColor.r, invColor.g, invColor.b, 0.75f);
            }
            else
            {
                GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 0.75f);
            }
            invTime -= Time.deltaTime;
        }
        else
        {
            GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, 1.0f);
        }

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * maxSpeed * 1.5f);
        
        label.transform.position = this.transform.position - new Vector3(0, -0.35f, 1);

        if (id == Game.Instance.playerId)
        {
            cam = GameObject.Find("Main Camera");

            // adjust camera offset
            Vector3 mouseInWorld = cam.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            Vector3 cameraOffset = transform.position + (mouseInWorld - transform.position) * 0.3f;
            cameraOffset.z = -500.0f;
            cam.transform.position = cameraOffset;
            //cam.transform.position = new Vector3(transform.position.x, transform.position.y, cam.transform.position.z);

            // mouse aim
            Vector2 positionInWorld = new Vector2(transform.position.x, transform.position.y);
            mouseInWorld = (Vector2)cam.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            float angle = AngleBetweenTwoPoints(positionInWorld, mouseInWorld) + 180;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            float rot = transform.rotation.eulerAngles.z;
            if (rot != prot)
            {
                Game.Instance.Send(rot);
            }
            prot = rot;

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
                //GameObject test = (GameObject) Instantiate((GameObject)Resources.Load("Prefabs/Bullet", typeof(GameObject)), transform.position, Quaternion.identity);
                //test.GetComponent<Bullet>().velocity = new Vector3(1, 0, 0);
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

    void OnGUI()
    {
    }
}
