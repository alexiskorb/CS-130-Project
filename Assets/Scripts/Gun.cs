using UnityEngine;

// @class Gun
// @desc Code attached to each gun 
public class Gun : MonoBehaviour
{
    public float fireRate = 0.2f;
    // Bullets to fire
    public GameObject bulletPrefab;
    private float last = 0.0f;
    // Speed to fire the bullet
    public float bulletSpeed_ = 10;

    void Update()
    {
        last += Time.deltaTime;
    }
    // Fires a bullet from the front of the gun
    public void Fire()
    {
        if (last > fireRate)
        {
            last = 0.0f;
            var bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
            bullet.GetComponent<Rigidbody>().velocity = transform.forward * bulletSpeed_;
        }
    }
}