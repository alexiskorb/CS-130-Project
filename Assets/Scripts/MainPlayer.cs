using UnityEngine;

// @class MainPlayer
// @desc Extends NetworkedPlayer so that the main player is moved based on player input
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
        // Move the main player based on player input
		Move(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"), Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
	}
}
