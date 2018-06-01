using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// @class CreateMatchUI
// @desc Controls the UI for the Create Match screen
public class CreateMatchUI : MonoBehaviour {

    // UI elements
    public GameObject playerNameText;
    public GameObject matchNameInput;
    public GameObject createMatchError;

    void Start ()
    {
        createMatchError.SetActive(false);
        playerNameText.GetComponent<Text>().text = FpsClient.GameClient.Instance.MainPlayerName;
    }

    public void SubmitMatchName()
    {
        string name = matchNameInput.GetComponent<InputField>().text;
        FpsClient.GameClient.Instance.CurrentLobby = name;
    }

    public void ShowCreateMatchError()
    {
        createMatchError.SetActive(true);
    }

    public void ResetMenu()
    {
        matchNameInput.GetComponent<InputField>().text = "";
        createMatchError.SetActive(false);
    }
}
