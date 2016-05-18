using UnityEngine;
using System.Collections;

public class Particle : MonoBehaviour {

	// Use this for initialization
	void Start () {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        ps.Play();
        Destroy(this, ps.duration);
	}

}
