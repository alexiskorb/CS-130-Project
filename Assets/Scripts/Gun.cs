using UnityEngine;

public class Gun : MonoBehaviour {
	public GameObject bulletPrefab;
	public float bulletSpeed_ = 10;

	public void Fire()
	{
		var bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
		bullet.GetComponent<Rigidbody>().velocity = transform.forward * bulletSpeed_;
	}
}
