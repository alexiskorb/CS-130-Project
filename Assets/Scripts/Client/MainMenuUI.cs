using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Steamworks;

public class MainMenuUI : MonoBehaviour {

    public string gameScenePath;
    public Canvas mainMenu;
    public Canvas createMatchMenu;
    public Canvas startMatchMenu;
    public Canvas joinMatchMenu;

    public GameObject createMatchError;
    public GameObject playerLobbyName;
    public GameObject matchNameInput;
    public GameObject playerNameInput;

    private Text m_playerLobbyText;

    // Use this for initialization
    void Start()
    {
        m_playerLobbyText = playerLobbyName.GetComponent<Text>();

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
        GameManager.Instance.StartMatch(gameScenePath);
    }

    public void SubmitMainPlayerName()
    {
        string name = playerNameInput.GetComponent<InputField>().text;
        GameManager.Instance.MainPlayerName = name;
    }

    public void SubmitMatchName()
    {
        string name = matchNameInput.GetComponent<InputField>().text;
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
            createMatchError.SetActive(true);
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
        createMatchError.SetActive(false);
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
