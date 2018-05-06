using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartMatchUI : MonoBehaviour {

    public GameObject matchName;
    public GameObject playerLobbyName;
    private Text m_playerLobbyText;

    // Use this for initialization
    void Start () {
        m_playerLobbyText = playerLobbyName.GetComponent<Text>();
    }

    void Update ()
    {
        // Update match name
        matchName.GetComponent<Text>().text = "Match: " + GameManager.Instance.MatchName;

        // Update player lobby text
        m_playerLobbyText.text = "";

        foreach (string playerIDs in GameManager.Instance.PlayerIds)
        {
            m_playerLobbyText.text += playerIDs + "\n";
        }
    }

}
