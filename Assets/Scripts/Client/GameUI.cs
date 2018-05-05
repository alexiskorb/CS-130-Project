using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour {

    public Canvas gameMenu;
    public string endGameScenePath;

    private bool m_enableMenu = false;

    // Use this for initialization
    void Start () {
        gameMenu.enabled = m_enableMenu;
	}
	
	// Update is called once per frame
	void Update () {

        // Show menu on pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            m_enableMenu = !m_enableMenu;
            gameMenu.enabled = m_enableMenu;
			GameManager.Instance.MenuOpen = m_enableMenu;
        }
        		
	}

    public void DropMatch()
    {
        GameManager.Instance.DropMatch(endGameScenePath);
    }

    public void ResumeGame()
    {
        m_enableMenu = false;
        gameMenu.enabled = m_enableMenu;
    }

}
