using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    // public variables
    public float moveSpeed = 5;
    public float horizontalCameraSensitivity = 15f;
    public float verticalCameraSensitivity = 5f;

    // private variables
    private Rigidbody m_rigidbody;
    private Transform m_cameraTransform;
    private float m_yRotation = 0f;
    private float m_xCameraRotation = 15f;

    private void Awake(){

    }

    // Use this for initialization
    void Start () {
        m_rigidbody = GetComponent<Rigidbody>();
        m_cameraTransform = transform.Find("Main Camera");
    }
	
	// Update is called once per frame
	void Update () {

		/*if (!GameManager.Instance.MenuOpen) {
			// Rotate camera vertically
			m_xCameraRotation -= verticalCameraSensitivity * Input.GetAxis("Mouse Y");
			m_cameraTransform.eulerAngles = new Vector3(m_xCameraRotation, m_yRotation, 0);
		}*/

       /* // Rotate player horizontally
        m_yRotation -= horizontalCameraSensitivity * Input.GetAxis("Mouse X");
        transform.eulerAngles = new Vector3(0, m_yRotation, 0);

        // Move player   
        Vector3 forwardBackDirection = new Vector3(Mathf.Sin(m_yRotation * Mathf.PI / 180), 0, Mathf.Cos(m_yRotation * Mathf.PI / 180));
		Debug.Log ("ForwardBackDirection: " + forwardBackDirection.ToString ());
        Vector3 leftRightDirection = new Vector3(Mathf.Sin((m_yRotation + 90) * Mathf.PI / 180), 0, Mathf.Cos((m_yRotation + 90) * Mathf.PI / 180));
		Debug.Log ("LeftRightDirection: " + leftRightDirection.ToString ());
        /*m_rigidbody.velocity = moveSpeed * (forwardBackDirection*Input.GetAxis("Vertical") + leftRightDirection* Input.GetAxis("Horizontal"));*/
    }
}

