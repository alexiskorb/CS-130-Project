using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour {

    public float lifetime = 3.0f;
	
    // Destroy bullet after a given lifetime
    void Awake()
    {
        Destroy(this.gameObject, lifetime);
    }

}
