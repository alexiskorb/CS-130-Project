using UnityEngine;

public class MainPlayer : NetworkedPlayer {

	new void Start()
	{
		base.Start();
		Transform cameraTransform = transform.Find("Main Camera");
		cameraTransform.eulerAngles = transform.eulerAngles;
	}

	void Update()
	{
		Move(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"), Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
	}
}
