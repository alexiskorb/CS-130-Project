using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ServerNetworkHandler : MonoBehaviour {

	private bool isAtStart = true;

    private static ServerNetworkHandler m_instance = null;

    // Get instance of the ServerNetworkHandler
    public static ServerNetworkHandler Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<ServerNetworkHandler>();
                if (m_instance == null)
                {
                    GameObject nh = new GameObject();
                    nh.name = "ServerNetworkHandler";
                    m_instance = nh.AddComponent<ServerNetworkHandler>();
                    DontDestroyOnLoad(nh);
                }
            }
            return m_instance;
        }
    }

    // Enforce singleton behavior
    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }
    }

	void Update()
	{
		if (isAtStart) {
			NetworkServer.Listen(1001);
			isAtStart = false;
		}

	}

}
