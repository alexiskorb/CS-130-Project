using UnityEngine;

// @class MainPlayer
// @desc Extends NetworkedPlayer so that the main player is moved based on player input.
public class MainPlayer : NetworkedPlayer {

	// @func Start
	// @desc Get a reference to the main camera so that the camera rotates 
	// with the player direction
	new void Start()
	{
		base.Start();
		GameObject camera = GameObject.Find("Main Camera");
		Transform cameraTransform = camera.transform;
		cameraTransform.eulerAngles = transform.eulerAngles;
	}

	// @func Update
	// @desc Move the player using keyboard input. 
	void Update()
	{
        // Move the main player based on player input
		Move(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"), Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
	}
}
