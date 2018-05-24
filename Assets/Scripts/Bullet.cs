using UnityEngine;

public class Bullet : MonoBehaviour {
	public float lifetime_ = 3.0f;

	void Awake()
	{
		Destroy(gameObject, lifetime_);
	}
}
