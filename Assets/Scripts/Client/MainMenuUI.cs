using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Steamworks;

public class MainMenuUI : MonoBehaviour {

    public Canvas mainMenu;
    public Canvas createMatchMenu;
    public Canvas startMatchMenu;
    public Canvas joinMatchMenu;

    private GameObject m_createMatchError;
    private Text m_playerLobbyText;

    // Use this for initialization
    void Start()
    {
        m_createMatchError = GameObject.Find("Create Match Error");
        m_playerLobbyText = GameObject.Find("Player Lobby Text").GetComponent<Text>();

        ShowMainMenu();

		if(SteamManager.Initialized) {
			string name = SteamFriends.GetPersonaName();
			Debug.Log(name);
		}

		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		Debug.Log("[STEAM-FRIENDS] Listing " + friendCount + " Friends.");
		for (int i = 0; i < friendCount; ++i) {
			CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendName = SteamFriends.GetFriendPersonaName(friendSteamId);
			EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendSteamId);

			Debug.Log(friendName + " is " + friendState);
		}

    }

    void Update()
    {
        // Update player lobby text if on startMatchMenu
        if (startMatchMenu.enabled)
        {
            m_playerLobbyText.text = "";

            foreach (string playerIDs in GameManager.Instance.Players)
            {
                m_playerLobbyText.text += playerIDs + "\n";
            }
        }
    }

    public void StartMatch()
    {
        GameManager.Instance.StartMatch("Scenes/Client/MainScene");
    }

    public void SubmitMainPlayerName()
    {
        string name = GameObject.Find("Player Name Input").GetComponent<InputField>().text;
        GameManager.Instance.MainPlayerName = name;
    }

    public void SubmitMatchName()
    {
        string name = GameObject.Find("Match Name Input").GetComponent<InputField>().text;
        GameManager.Instance.MatchName = name;
    }

    public void CreateMatch()
    {
        if (GameManager.Instance.MatchReady)
        {
            ShowStartMatchMenu();
            GameManager.Instance.CreateMatch();
        }
        else
        {
            m_createMatchError.SetActive(true);
        }
    }

    public void ShowMainMenu()
    {
        mainMenu.enabled = true;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = false;
    }

    public void ShowCreateMatchMenu()
    {
        m_createMatchError.SetActive(false);
        mainMenu.enabled = false;
        createMatchMenu.enabled = true;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = false;
    }

    public void ShowStartMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = true;
        joinMatchMenu.enabled = false;
    }

    public void ShowJoinMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = true;
    }

}
