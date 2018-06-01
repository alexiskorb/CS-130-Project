using UnityEngine;

public class MainPlayer : NetworkedPlayer {

	new void Start()
	{
		base.Start();
		GameObject camera = GameObject.Find("Main Camera");
		Transform cameraTransform = camera.transform;
		cameraTransform.eulerAngles = transform.eulerAngles;
	}

	void Update()
	{
		Move(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"), Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
	}
}
