using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// @class MainMenuUI
// @desc Controls all of the menu screens in the MainMenu scene
// and calls matchmaking functions when triggered by UI input.
public class MainMenuUI : MonoBehaviour {

    // Canvases for each menu screen
    public Canvas mainMenu;
    public Canvas createMatchMenu;
    public Canvas startMatchMenu;
    public Canvas joinMatchMenu;
    public Canvas steamJoinMatchCanvas;
    public Canvas steamInitializationErrorCanvas;

    // Singleton instance of the MainMenuUI
	private static MainMenuUI m_instance = null;

    // Menu state variables
    private bool m_isMatchCreator = true;
    private bool m_inMatchLobby = false;

    // Gets singleton instance of the MainMenuUI
	public static MainMenuUI Instance
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = FindObjectOfType<MainMenuUI>();
				if (m_instance == null)
				{
					GameObject gm = new GameObject();
					gm.name = "MainMenuUI";
					m_instance = gm.AddComponent<MainMenuUI>();
					DontDestroyOnLoad(gm);
				}
			}
			return m_instance;
		}
	}

    void Start()
    {
        // Start off in the Main Menu screen
        ShowMainMenu();
        m_inMatchLobby = false;

        // Display error if Steam is initialized incorrectly
        if (SteamManager.Initialized)
        {
            steamInitializationErrorCanvas.enabled = false;
        }
        else
        {
            steamInitializationErrorCanvas.enabled = true;
        }
    }

    public void GoToCreateMatchMenu()
    {
        FpsClient.GameClient.Instance.SendRefreshServerList();
        ShowCreateMatchMenu();
    }

    public void GoToJoinMatchMenu()
    {
        ShowJoinMatchMenu();
    } 
    public void GoToStartMatchMenu()
    {
        ShowStartMatchMenu();
    }

    public void StartMatch()
    {
        FpsClient.GameClient.Instance.SendStartGame();
        startMatchMenu.enabled = false;
    }

    // Create a match lobby if all necesssary input data has been given. Display an error otherwise.
    public void CreateMatch()
    {
        if (FpsClient.GameClient.Instance.NamesSet())
        {
            m_isMatchCreator = true;
            FpsClient.GameClient.Instance.SendCreateLobby();
        }
        else
        {
            createMatchMenu.GetComponent<CreateMatchUI>().ShowCreateMatchError();
        }
        // TODO: Possibly handle match creation error
    }

    // Join a lobby if all necesssary input data has been given. Display an error otherwise.
    public void JoinLobby()
    {
        if (FpsClient.GameClient.Instance.NamesSet())
        {
            m_isMatchCreator = false;
            FpsClient.GameClient.Instance.SendPlayerJoin();
            ShowStartMatchMenu();
        }
        else
        {
            joinMatchMenu.GetComponent<JoinMatchUI>().ShowJoinMatchError();
        }
        // TODO: Possibly handle lobby joining failure
    }

    // Join a lobby from the Steam join match invite
    public void JoinLobbyFromInvite()
    {
		if (FpsClient.GameClient.Instance.NamesSet())
		{
			m_isMatchCreator = false;
			FpsClient.GameClient.Instance.SendPlayerJoinFromInvite();
            CloseSteamJoinMatchPopup();
			//ShowStartMatchMenu();
		}
		else
		{
			joinMatchMenu.GetComponent<JoinMatchUI>().ShowJoinMatchError();
		}
		// TODO: Possibly handle lobby joining failure
    }
   

    // Leave player lobby and return to the previous screen
    public void CancelMatch()
    {
        FpsClient.GameClient.Instance.SendLeaveLobby();
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

    // Return to Main Menu screen and reset menu state
    public void ReturnToMainMenu()
    {
        if(m_inMatchLobby)
        {
            FpsClient.GameClient.Instance.SendLeaveLobby();
        }
        joinMatchMenu.GetComponent<JoinMatchUI>().ResetMenu();
        createMatchMenu.GetComponent<CreateMatchUI>().ResetMenu();
        ShowMainMenu();
    }


    // Called by GameClient when an invite arrives from the server
    public void OpenSteamJoinMatchPopup()
    {
        steamJoinMatchCanvas.enabled = true;
    }

    // Rejects invitation by closing the popup menu and resetting the GameClient's invitedLobby to empty
    public void CloseSteamJoinMatchPopup()
	{
        steamJoinMatchCanvas.enabled = false;
		FpsClient.GameClient.Instance.m_invitedLobby = "";
    }

    // The following functions display the chosen menu screen and hide other menu screens

    private void ShowMainMenu()
    {
        mainMenu.enabled = true;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = false;
        steamJoinMatchCanvas.enabled = false;
        m_inMatchLobby = false;
    }

    private void ShowCreateMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = true;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = false;
        steamJoinMatchCanvas.enabled = false;
        m_inMatchLobby = false;
    }

    private void ShowStartMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = true;
        joinMatchMenu.enabled = false;
        steamJoinMatchCanvas.enabled = false;
        m_inMatchLobby = true;
    }

    private void ShowJoinMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = false;
        startMatchMenu.enabled = false;
        joinMatchMenu.enabled = true;
        steamJoinMatchCanvas.enabled = false;
        //joinMatchMenu.GetComponent<JoinMatchUI>().RefreshOpenMatches();
        m_inMatchLobby = false;
    }
}
