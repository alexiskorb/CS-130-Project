using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    // public variables
    public float moveSpeed = 1;

    // private variables
    private Rigidbody2D m_rigidbody;

    private void Awake(){
        m_rigidbody = GetComponent<Rigidbody2D>();
    }

    // Use this for initialization
    void Start () {
       
	}
	
	// Update is called once per frame
	void Update () {

        // Move player
        float horizontalMove = Input.GetAxis("Horizontal");
        float verticalMove = Input.GetAxis("Vertical");
        m_rigidbody.velocity = new Vector2(horizontalMove * moveSpeed, verticalMove * moveSpeed);

	}
}
