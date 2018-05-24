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
    public Canvas steamJoinMatchCanvas;

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

    public void GoToCreateMatchMenu()
    {
        ShowCreateMatchMenu();
    }

    public void GoToJoinMatchMenu()
    {
        ShowJoinMatchMenu();
    }

    // Start a match
    public void StartMatch()
    {
        FpsClient.GameClient.Instance.SendStartGame();
        startMatchMenu.enabled = false;
    }

    // Create a match
    public void CreateMatch()
    {
        m_isMatchCreator = true;
        FpsClient.GameClient.Instance.CreateLobby();
        ShowStartMatchMenu();
        /* TODO: Possibly handle match creation error
        if ()
        {
            ShowStartMatchMenu();
        }
        else
        {
            createMatchMenu.GetComponent<CreateMatchUI>().ShowCreateMatchError();
        }
        */
    }

    // Join a match
    public void JoinLobby()
    {
        m_isMatchCreator = false;
        FpsClient.GameClient.Instance.SendJoinLobby();
        ShowStartMatchMenu();
        /* TODO: Possibly handle lobby joining failure
        if ()
        {
        }
        else
        {
            joinMatchMenu.GetComponent<JoinMatchUI>().ShowJoinMatchError();
        }
        */
    }

    // MELODIE: TODO
    // Join a lobby from the Steam join match invite
    public void JoinLobbyFromInvite()
    {
        // Called by popup menu join button
    }
   

    // Leave player lobby
    public void CancelMatch()
    {
        /* TODO
        FpsClient.GameClient.Instance.LeaveMatchLobby();
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
        */
    }

    // Return to main menu
    public void ReturnToMainMenu()
    {
        // Need to Implement LeaveMatchLobby
        //FpsClient.GameClient.Instance.LeaveMatchLobby();
        joinMatchMenu.GetComponent<JoinMatchUI>().ResetMenu();
        createMatchMenu.GetComponent<CreateMatchUI>().ResetMenu();
        ShowMainMenu();
    }


    // MELODIE: TODO
    public void OpenSteamJoinMatchPopup()
    {
        steamJoinMatchCanvas.enabled = true;
        // Presumably, you'll change this text based on the user doing the invite
        //steamJoinMatchCanvas.GetComponent<SteamJoinMatchUI>().SetMatchText("User XXXX would like you to to invite you to a match:");
    }

    // MELODIE: TODO
    public void CloseSteamJoinMatchPopup()
    {
        // Called by popup menu decline button
        steamJoinMatchCanvas.enabled = false;
    }


    private void ShowMainMenu()
    {
        mainMenu.enabled = true;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = false;
        steamJoinMatchCanvas.enabled = false;
    }

    private void ShowCreateMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = true;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = false;
        steamJoinMatchCanvas.enabled = false;
    }

    private void ShowStartMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = true;
        joinMatchMenu.enabled = false;
        steamJoinMatchCanvas.enabled = false;
    }

    private void ShowJoinMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = true;
        steamJoinMatchCanvas.enabled = false;
        joinMatchMenu.GetComponent<JoinMatchUI>().RefreshOpenMatches();
    }


}
