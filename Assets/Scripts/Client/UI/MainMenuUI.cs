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

    private bool m_isMatchCreator = true;

    // Use this for initialization
    void Start()
    {
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
    }

    // Start a match
    public void StartMatch()
    {
        GameManager.Instance.StartMatch(gameScenePath);
    }

    // Create a match
    public void CreateMatch()
    {
        m_isMatchCreator = true;
        if (createMatchMenu.GetComponent<CreateMatchUI>().CreateMatch())
        {
            ShowStartMatchMenu();
        }
    }

    // Join a match
    public void JoinMatch()
    {
        m_isMatchCreator = false;
        if (joinMatchMenu.GetComponent<JoinMatchUI>().JoinMatch())
        {
            ShowStartMatchMenu();
        }
    }

    // Leave player lobby
    public void CancelMatch()
    {
        GameManager.Instance.ResetGameManager();
        if (m_isMatchCreator)
        {
            createMatchMenu.GetComponent<CreateMatchUI>().ResetMenu();
            ShowCreateMatchMenu();
        }
        else
        {
            joinMatchMenu.GetComponent<JoinMatchUI>().ResetMenu();
            ShowJoinMatchMenu();
        }
    }

    // Return to main menu
    public void ReturnToMainMenu()
    {
        GameManager.Instance.ResetGameManager();
        joinMatchMenu.GetComponent<JoinMatchUI>().ResetMenu();
        createMatchMenu.GetComponent<CreateMatchUI>().ResetMenu();
        ShowMainMenu();
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
