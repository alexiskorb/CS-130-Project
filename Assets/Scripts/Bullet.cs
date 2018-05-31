using UnityEngine;

public class Bullet : MonoBehaviour {
	public float lifetime_ = 3.0f;

    private bool m_active = true;

	void Awake()
	{
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
            gameObject.GetComponent<MeshRenderer>().enabled = value;
            m_active = value;
        }
    }

}
