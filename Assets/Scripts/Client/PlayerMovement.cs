using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    // public variables
    public bool isMainPlayer = false;
    public float moveSpeed = 5;
    public float horizontalCameraSensitivity = 15f;
    public float verticalCameraSensitivity = 5f;

    // private variables
    private Rigidbody m_rigidbody;
    private Transform m_cameraTransform;
    private float m_yRotation = 0f;
    private float m_xCameraRotation = 15f;

    // Use this for initialization
    void Start ()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        if (isMainPlayer)
        {
            m_cameraTransform = transform.Find("Main Camera");
        }
    }

	// Update is called once per frame
	void Update () {

        // Rotate camera vertically if main player
		if (!GameManager.Instance.MenuOpen && isMainPlayer) {
			m_xCameraRotation -= verticalCameraSensitivity * Input.GetAxis("Mouse Y");
			m_cameraTransform.eulerAngles = new Vector3(m_xCameraRotation, m_yRotation, 0);
		}

        /*
        // Rotate player horizontally
        m_yRotation -= horizontalCameraSensitivity * Input.GetAxis("Mouse X");
        transform.eulerAngles = new Vector3(0, m_yRotation, 0);

        // Move player   
        Vector3 forwardBackDirection = new Vector3(Mathf.Sin(m_yRotation * Mathf.PI / 180), 0, Mathf.Cos(m_yRotation * Mathf.PI / 180));
        Vector3 leftRightDirection = new Vector3(Mathf.Sin((m_yRotation + 90) * Mathf.PI / 180), 0, Mathf.Cos((m_yRotation + 90) * Mathf.PI / 180));
        m_rigidbody.velocity = moveSpeed * (forwardBackDirection*Input.GetAxis("Vertical") + leftRightDirection* Input.GetAxis("Horizontal"));
        */
    }

    public Vector3 CalculateHorizontalRotation(float axis)
    {
        float yRotation = m_yRotation - horizontalCameraSensitivity * axis;
        return new Vector3(0, yRotation, 0);
    }

    public Vector3 CalculateVelocity(float verticalAxis, float horizontalAxis)
    {
        Vector3 forwardBackDirection = new Vector3(Mathf.Sin(m_yRotation * Mathf.PI / 180), 0, Mathf.Cos(m_yRotation * Mathf.PI / 180));
        Vector3 leftRightDirection = new Vector3(Mathf.Sin((m_yRotation + 90) * Mathf.PI / 180), 0, Mathf.Cos((m_yRotation + 90) * Mathf.PI / 180));
        return moveSpeed * (forwardBackDirection * verticalAxis + leftRightDirection * horizontalAxis);
    }

    public void RotatePlayer(Vector3 rotation)
    {
        m_yRotation = rotation[1];
        transform.eulerAngles = rotation;
    }

    public void SetVelocity(Vector3 velocity)
    {
        m_rigidbody.velocity = velocity;
    }
}

