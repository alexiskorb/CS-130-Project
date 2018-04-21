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
        ShowMainMenu();
    }

    public void StartMatch()
    {
        GameManager.Instance.StartMatch("MainScene");
    }

    public void ShowMainMenu()
    {
        mainMenu.enabled = true;
        createMatchMenu.enabled = false;
        joinMatchMenu.enabled = false;
    }

    public void ShowCreateMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = true;
        joinMatchMenu.enabled = false;
    }

    public void ShowJoinMatchMenu()
    {
        mainMenu.enabled = false;
        createMatchMenu.enabled = false;
        joinMatchMenu.enabled = true;
    }

}
