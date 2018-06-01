using UnityEngine;

// @class Gun
// @desc Code attached to each gun 
public class Gun : MonoBehaviour {
    
    // Bullets to fire
	public GameObject bulletPrefab;

    // Speed to fire the bullet
	public float bulletSpeed_ = 10;

    // Fires a bullet from the front of the gun
	public void Fire()
	{
		var bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
		bullet.GetComponent<Rigidbody>().velocity = transform.forward * bulletSpeed_;
	}
}
