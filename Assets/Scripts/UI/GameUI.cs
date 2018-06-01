using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// @class GameUI
// @desc Controls the in-game menu screen
public class GameUI : MonoBehaviour {

    public Canvas gameMenu;
    public string endGameScenePath;
    private bool m_enableMenu = false;

    void Start ()
    {
        gameMenu.enabled = m_enableMenu;
	}
	
	void Update () {

        // Show or hide menu on command
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            m_enableMenu = !m_enableMenu;
            gameMenu.enabled = m_enableMenu;
			FpsClient.GameClient.Instance.MenuOpen = m_enableMenu;
        }
       		
	}

    public void DropMatch()
    {
		//send packet to server to drop from other games
		FpsClient.GameClient.Instance.SendDropMatch();

		SceneManager.LoadScene("MainMenu");
    }

    public void ResumeGame()
    {
        m_enableMenu = false;
        gameMenu.enabled = m_enableMenu;
        FpsClient.GameClient.Instance.MenuOpen = m_enableMenu;
    }

}
