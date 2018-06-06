using UnityEngine;
using System.Collections.Generic;

// @class InputCallbacks
// @desc Data structure for holding player input callbacks. 
public class InputCallbacks {
	public delegate void InputCallback();
	private Dictionary<Netcode.InputBit, InputCallback> commandCallbacks_ = new Dictionary<Netcode.InputBit, InputCallback>();

	// @func Add
	// @desc Associates a callback with player input.
	public void Add(Netcode.InputBit cmd, InputCallback commandCallback)
	{
		commandCallbacks_.Add(cmd, commandCallback);
	}

	// @func Call
	// @desc Invoke the callback for this input bit.
	public void Call(Netcode.InputBit cmd)
	{
		if (commandCallbacks_.ContainsKey(cmd))
			commandCallbacks_[cmd].Invoke();
	}
}

// @class NetworkedPlayer
// @desc Code for each player character on the server. 
public class NetworkedPlayer : MonoBehaviour {
    // Camera sensitivity.
	public float horizontalCameraSensitivity_ = 15f;
	public float verticalCameraSensitivity_ = 5f;
    // Player move speed.
	public float movementSpeed_ = 5;
    // Time that player is invulnerable to damage after being hit with a bullet.
	public float lostLifeBufferTime = 1f;
    // Game object controlling rotation of player arms and camera.
	public GameObject verticalRotationParent;
	// Main weapon of the player.
	public GameObject primaryWeapon;
    // Overhead UI display.
	public Canvas displayUI;
	// Players can be hit a five times before they die. 
	public int maxLife = 5;
	// Lives remaining.
	private int m_currentLife = 5;
	// Life lost timer, so bullets don't continuously cause damage.
	private float m_timeSinceLifeLost = 0f;
	// Reference to rigid body to change velocity. 
	protected Rigidbody rigidbody_;
	// Command callbacks for this player. 
	private InputCallbacks commandCallbacks_ = new InputCallbacks();

	public void Start()
	{
		rigidbody_ = GetComponent<Rigidbody>();
		commandCallbacks_.Add(Netcode.InputBit.PRIMARY_WEAPON, FireWeapon);
		displayUI.GetComponentInChildren<PlayerDisplay>().SetMaxLife(maxLife);
	}

	void Update()
	{
		m_timeSinceLifeLost += Time.deltaTime;
	}

	// @func TakeCommands
	// @desc The game server calls this to apply input received from the player. 
	public void TakeCommands(Netcode.InputBit cmd)
	{
		for (int i = 0; i < (int)Netcode.InputBit.END; i++)
			commandCallbacks_.Call(((Netcode.InputBit)((int)cmd & (1 << i))));
	}

	public int CurrentLife {
		get { return m_currentLife; }
		set {
			m_currentLife = value;
			displayUI.GetComponentInChildren<PlayerDisplay>().SetCurrentLife(m_currentLife);
		}
	}

	// @func OnTriggerEnter
    // @desc If an active bullet hits the player, make the bullet inactive and remove a life.
    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "bullet" && col.gameObject.GetComponent<Bullet>().IsActive)
        {
            col.gameObject.GetComponent<Bullet>().IsActive = false;
            if (m_timeSinceLifeLost > lostLifeBufferTime)
            {
                CurrentLife = (CurrentLife + 5) % 6;
                m_timeSinceLifeLost = 0f;
            }
        }
    }

	// @func FireWeapon
	// @desc Get the gun component and shoot!
	public void FireWeapon()
	{
		Gun gun = primaryWeapon.GetComponent<Gun>();
		gun.Fire();
	}

	// @func Move
	// @desc Move the player with player input.
	public void Move(float verticalAxis, float horizontalAxis, float mouseX, float mouseY)
	{
		float xRotation = verticalRotationParent.transform.eulerAngles.x;
		float yRotation = transform.eulerAngles.y;
		xRotation -= verticalCameraSensitivity_ * mouseY;
		yRotation -= horizontalCameraSensitivity_ * mouseX;
		transform.eulerAngles = new Vector3(0, yRotation, 0);
		verticalRotationParent.transform.eulerAngles = new Vector3(xRotation, yRotation, 0);
		ChangeVelocity(verticalAxis, horizontalAxis);
	}

	// @func ChangeVelocity
	// @desc Change the velocity of the player. 
	public void ChangeVelocity(float verticalAxis, float horizontalAxis)
	{
		float yDirection = transform.eulerAngles.y;
		Vector3 zDirection = new Vector3(Mathf.Sin(yDirection * Mathf.PI / 180), 0, Mathf.Cos(yDirection * Mathf.PI / 180));
		Vector3 xDirection = new Vector3(Mathf.Sin((yDirection + 90) * Mathf.PI / 180), 0, Mathf.Cos((yDirection + 90) * Mathf.PI / 180));
		rigidbody_.velocity = movementSpeed_ * (zDirection * verticalAxis + xDirection * horizontalAxis);
	}

}
