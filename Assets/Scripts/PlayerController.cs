using UnityEngine;

// @Joe I'm using this for testing the server
public class PlayerController : MonoBehaviour {
	public float moveSpeed = 5;
	public float horizontalCameraSensitivity = 15f;
	public float verticalCameraSensitivity = 5f;

	private Rigidbody m_rigidbody;
	private Transform m_cameraTransform;
	private float m_yRotation = 0f;
	private float m_xCameraRotation = 15f;

	void Start()
	{
		m_rigidbody = GetComponent<Rigidbody>();
		m_cameraTransform = transform.Find("Main Camera");
	}

	void Update()
    { 

		// Rotate player horizontally
		m_yRotation -= horizontalCameraSensitivity * Input.GetAxis("Mouse X");
		transform.eulerAngles = new Vector3(m_xCameraRotation, m_yRotation, 0);
		// Move player   
		Vector3 forwardBackDirection = new Vector3(Mathf.Sin(m_yRotation * Mathf.PI / 180), 0, Mathf.Cos(m_yRotation * Mathf.PI / 180));
		Vector3 leftRightDirection = new Vector3(Mathf.Sin((m_yRotation + 90) * Mathf.PI / 180), 0, Mathf.Cos((m_yRotation + 90) * Mathf.PI / 180));
		m_rigidbody.velocity = moveSpeed * (forwardBackDirection * Input.GetAxis("Vertical") + leftRightDirection * Input.GetAxis("Horizontal"));
	}
}
