using UnityEngine;
using System.Collections.Generic;

public class InputCallbacks {
	public delegate void InputCallback();
	private Dictionary<Netcode.InputBit, InputCallback> commandCallbacks_ = new Dictionary<Netcode.InputBit, InputCallback>();

	public void Add(Netcode.InputBit cmd, InputCallback commandCallback)
	{
		commandCallbacks_.Add(cmd, commandCallback);
	}

	public void Call(Netcode.InputBit cmd)
	{
		if (commandCallbacks_.ContainsKey(cmd))
			commandCallbacks_[cmd].Invoke();
	}
}

public class NetworkedPlayer : MonoBehaviour {
	public float horizontalCameraSensitivity_ = 15f;
	public float verticalCameraSensitivity_ = 5f;
	public float movementSpeed_ = 5;
	public GameObject primaryWeapon;
	protected Rigidbody rigidbody_;
	private InputCallbacks commandCallbacks_ = new InputCallbacks();

	public void Start()
	{
		rigidbody_ = GetComponent<Rigidbody>();
		commandCallbacks_.Add(Netcode.InputBit.PRIMARY_WEAPON, FireWeapon);
	}

	public void TakeCommands(Netcode.InputBit cmd)
	{
		for (int i = 0; i < (int)Netcode.InputBit.END; i++)
			commandCallbacks_.Call(((Netcode.InputBit)((int)cmd & (1 << i))));
	}

	public void FireWeapon()
	{
		Gun gun = primaryWeapon.GetComponent<Gun>();
		gun.Fire();
	}

	public void Move(float verticalAxis, float horizontalAxis, float mouseX, float mouseY)
	{
		float xRotation = transform.eulerAngles.x;
		float yRotation = transform.eulerAngles.y;
		xRotation -= verticalCameraSensitivity_ * mouseY;
		yRotation -= horizontalCameraSensitivity_ * mouseX;
		transform.eulerAngles = new Vector3(xRotation, yRotation, 0);

		ChangeVelocity(verticalAxis, horizontalAxis);
	}

	public void ChangeVelocity(float verticalAxis, float horizontalAxis)
	{
		float yDirection = transform.eulerAngles.y;
		Vector3 zDirection = new Vector3(Mathf.Sin(yDirection * Mathf.PI / 180), 0, Mathf.Cos(yDirection * Mathf.PI / 180));
		Vector3 xDirection = new Vector3(Mathf.Sin((yDirection + 90) * Mathf.PI / 180), 0, Mathf.Cos((yDirection + 90) * Mathf.PI / 180));
		rigidbody_.velocity = movementSpeed_ * (zDirection * verticalAxis + xDirection * horizontalAxis);
	}
}