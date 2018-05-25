using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SteamJoinMatchUI : MonoBehaviour {

    public GameObject joinMatchTextObject;
    private Text m_joinMatchText;

	// Singleton instance of the GameClient.
	private static SteamJoinMatchUI m_instance = null;

	// Get instance of the GameClient. 
	public static SteamJoinMatchUI Instance
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = FindObjectOfType<SteamJoinMatchUI>();
				if (m_instance == null)
				{
					GameObject gm = new GameObject();
					gm.name = "SteamJoinMatchUI";
					m_instance = gm.AddComponent<SteamJoinMatchUI>();
					DontDestroyOnLoad(gm);
				}
			}
			return m_instance;
		}
	}

    // Use this for initialization
    void Start () {
        m_joinMatchText = joinMatchTextObject.GetComponent<Text>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetMatchText(string text)
    {
        m_joinMatchText.text = text;
    }


}
