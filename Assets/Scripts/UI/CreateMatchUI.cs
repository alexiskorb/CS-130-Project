using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateMatchUI : MonoBehaviour {

    public GameObject playerNameInput;
    public GameObject matchNameInput;
    public GameObject createMatchError;



    // Use this for initialization
    void Start ()
    {
        createMatchError.SetActive(false);
    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    public void SubmitMainPlayerName()
    {
        string name = playerNameInput.GetComponent<InputField>().text;
        FpsClient.GameClient.Instance.MainPlayerName = name;
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
        playerNameInput.GetComponent<InputField>().text = "";
        matchNameInput.GetComponent<InputField>().text = "";
        createMatchError.SetActive(false);
    }
}
