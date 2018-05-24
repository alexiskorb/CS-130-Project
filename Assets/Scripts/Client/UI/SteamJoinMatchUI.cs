using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SteamJoinMatchUI : MonoBehaviour {

    public GameObject joinMatchTextObject;
    private Text m_joinMatchText;

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
