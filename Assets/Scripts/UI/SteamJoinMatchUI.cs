using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// @class SteamJoinMatchUI
// @desc Controls the UI for the Steam join match popup
public class SteamJoinMatchUI : MonoBehaviour {

    // UI elements
    public GameObject joinMatchTextObject;
    private Text m_joinMatchText;

	// Singleton instance of the SteamJoinMatchUI
	private static SteamJoinMatchUI m_instance = null;

	// Get instance of the SteamJoinMatchUI. 
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

    void Start ()
    {
        m_joinMatchText = joinMatchTextObject.GetComponent<Text>();
    }
	
    // Set display text shown in popup
    public void SetMatchText(string text)
    {
        m_joinMatchText.text = text;
    }


}
