using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour {

    public Canvas mainMenu;
    public Canvas createMatchMenu;
    public Canvas joinMatchMenu;

    // Use this for initialization
    void Start()
    {
        showMainMenu();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void startMatch()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void showMainMenu()
    {
        mainMenu.enabled = true;
        createMatchMenu.enabled = false;
        joinMatchMenu.enabled = false;
    }

    public void showCreateMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = true;
        joinMatchMenu.enabled = false;
    }

    public void showJoinMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = false;
        joinMatchMenu.enabled = true;
    }

}
