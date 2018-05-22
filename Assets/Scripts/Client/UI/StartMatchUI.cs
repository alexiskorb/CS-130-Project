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
        matchName.GetComponent<Text>().text = "Match: " + FpsClient.GameClient.Instance.CurrentLobby;

        // Update player lobby text
        m_playerLobbyText.text = "";

        foreach (string playerIDs in FpsClient.GameClient.Instance.LobbyPlayers)
        {
            m_playerLobbyText.text += playerIDs + "\n";
        }
    }

}
