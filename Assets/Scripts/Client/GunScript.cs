using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class GunScript : MonoBehaviour {

    public GameObject bulletPrefab;
    public float bulletSpeed = 10;

    // Use this for initialization
    void Start () {
		if(SteamManager.Initialized) {
			string name = SteamFriends.GetPersonaName();
			Debug.Log(name);
		}
	}
	
	// Update is called once per frame
	void Update () {

        // On left mouse click, shoot bullet
        if (Input.GetMouseButtonDown(0))
        {
            var bullet = (GameObject) Instantiate(bulletPrefab, transform.position, transform.rotation);
            bullet.GetComponent<Rigidbody>().velocity = transform.forward * bulletSpeed;
        }
		
	}
}
