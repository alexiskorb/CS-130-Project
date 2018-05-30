using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {

    public string gameScenePath;
    public Canvas mainMenu;
    public Canvas createMatchMenu;
    public Canvas startMatchMenu;
    public Canvas joinMatchMenu;
    public Canvas steamJoinMatchCanvas;
    public Canvas steamInitializationErrorCanvas;

	private static MainMenuUI m_instance = null;
    private bool m_isMatchCreator = true;
    private bool m_inMatchLobby = false;

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

    // Use this for initialization
    void Start()
    {
        ShowMainMenu();
        m_inMatchLobby = false;

        // Check if Steam Initialized
        if (SteamManager.Initialized)
        {
            steamInitializationErrorCanvas.enabled = false;
        }
        else
        {
            steamInitializationErrorCanvas.enabled = true;
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
        if (FpsClient.GameClient.Instance.NamesSet())
        {
            m_isMatchCreator = true;
            FpsClient.GameClient.Instance.CreateLobby();
            ShowStartMatchMenu();
        }
        else
        {
            createMatchMenu.GetComponent<CreateMatchUI>().ShowCreateMatchError();
        }
        /* TODO: Possibly handle match creation error
        if ()
        {
            ShowStartMatchMenu();
        }
        else
        {
        }
        */
    }

    // Join a match
    public void JoinLobby()
    {
        if (FpsClient.GameClient.Instance.NamesSet())
        {
            m_isMatchCreator = false;
            FpsClient.GameClient.Instance.SendJoinLobby();
            ShowStartMatchMenu();
        }
        else
        {
            joinMatchMenu.GetComponent<JoinMatchUI>().ShowJoinMatchError();
        }
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
		if (FpsClient.GameClient.Instance.NamesSet())
		{
			m_isMatchCreator = false;
			FpsClient.GameClient.Instance.SendJoinLobbyFromInvite();
			ShowStartMatchMenu();
		}
		else
		{
			joinMatchMenu.GetComponent<JoinMatchUI>().ShowJoinMatchError();
		}
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
   

    // Leave player lobby
    public void CancelMatch()
    {
        FpsClient.GameClient.Instance.LeaveLobby();
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
        if(m_inMatchLobby)
        {
            FpsClient.GameClient.Instance.LeaveLobby();
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

    // Rejects invitation, just close menu and reset GameClient's invitedLobby to empty
    public void CloseSteamJoinMatchPopup()
	{
        steamJoinMatchCanvas.enabled = false;
		FpsClient.GameClient.Instance.m_invitedLobby = "";
    }


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
        joinMatchMenu.GetComponent<JoinMatchUI>().RefreshOpenMatches();
        m_inMatchLobby = false;
    }


}
