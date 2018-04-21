using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour {

    public Canvas gameMenu;

    private bool enableMenu = false;

    // Use this for initialization
    void Start () {
        gameMenu.enabled = enableMenu;
	}
	
	// Update is called once per frame
	void Update () {

        // Show menu on pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            enableMenu = !enableMenu;
            gameMenu.enabled = enableMenu;
        }
        		
	}

    public void dropMatch()
    {
        SceneManager.LoadScene("MainMenu");
    }

}
