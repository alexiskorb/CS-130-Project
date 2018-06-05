using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnTest : MonoBehaviour {

    public GameObject playerPrefab;
	// Use this for initialization
	void Start () {
        Instantiate(playerPrefab);
	}
	
}
