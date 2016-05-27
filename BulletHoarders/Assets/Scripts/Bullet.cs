using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {

    private const float maxSpeed = 20;
    public Vector3 velocity = new Vector3(0, 0, 0);

	// Use this for initialization
	void Start () {
	    
	}
	
	// Update is called once per frame
	void Update () {
        transform.position += velocity * maxSpeed * Time.deltaTime;
	}
}
