using UnityEngine;

// @class Bullet
// @desc Code attached to each bullet object
public class Bullet : MonoBehaviour {
    
    // Lifetime of the bullet
	public float lifetime_ = 3.0f;

    // Determines whether the bullet is visible and can harm players
    private bool m_active = true;

	void Awake()
	{
        // Destroy the bullet after its lifetime is up
		Destroy(gameObject, lifetime_);
	}

    public bool IsActive
    {
        get
        {
            return m_active;
        }
        set
        {
            // Changes visibility of the bullet
            gameObject.GetComponent<MeshRenderer>().enabled = value;
            m_active = value;
        }
    }

}
