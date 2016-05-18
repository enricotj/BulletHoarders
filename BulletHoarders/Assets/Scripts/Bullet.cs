using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {

    private float maxSpeed = 20.0f;
    public Vector3 target = new Vector3();

	// Use this for initialization
	void Start () {
	    
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 dir = (target - transform.position).normalized;

        if (Vector3.Distance(transform.position, target) < maxSpeed * Time.deltaTime)
        {
            transform.position = target;
        }
        else
        {
            transform.position += dir * maxSpeed * Time.deltaTime;
        }
	}

    void OnDestroy()
    {
    }
}
