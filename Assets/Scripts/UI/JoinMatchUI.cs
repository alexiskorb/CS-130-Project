using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// @class JoinMatchUI
// @desc Controls the UI for the Join Match screen
public class JoinMatchUI : MonoBehaviour {

    // UI elements
    public GameObject playerNameText;
    public GameObject joinMatchError;
    public GameObject matchLobby;
    public GameObject matchButtonPrefab;
    public GameObject inputPlayerNameText;

    // Maps open matches to the game object representing it in the scrollable match list
    private Dictionary<string, GameObject> openMatchTexts;


    void Start ()
    {
        joinMatchError.SetActive(false);
        openMatchTexts = new Dictionary<string, GameObject>();
        playerNameText.GetComponent<Text>().text = FpsClient.GameClient.Instance.MainPlayerName;
    }

    void Update ()
    {
        // Get list of open matches from the client
        List<string> openMatches = FpsClient.GameClient.Instance.ListOfGames;

        // Add in new matches
        foreach (string match in openMatches)
        {
            if (!openMatchTexts.ContainsKey(match))
            {
                GameObject matchButton = (GameObject)Instantiate(matchButtonPrefab);
                matchButton.transform.SetParent(matchLobby.transform);
                matchButton.GetComponent<Button>().onClick.AddListener(() => { SubmitMatchName(match); });
                Text matchText = matchButton.GetComponentInChildren<Text>();
                matchText.text = match;

                openMatchTexts.Add(match, matchButton);
            }
        }

        // Remove old matches
        List<string> openMatchTextKeys = new List<string>(openMatchTexts.Keys);
        foreach (string match in openMatchTextKeys)
        {
            if (!openMatches.Contains(match) && openMatchTexts.ContainsKey(match))
            {
                Destroy(openMatchTexts[match]);
                openMatchTexts.Remove(match);
            }
        }
    }

    public void RefreshOpenMatches()
    {
        FpsClient.GameClient.Instance.SendRefreshLobbyList();
    }

    public void SubmitPlayerName()
    {
        FpsClient.GameClient.Instance.MainPlayerName = inputPlayerNameText.GetComponent<Text>().text;
    }

    public void SubmitMatchName(string name)
    {
        FpsClient.GameClient.Instance.CurrentLobby = name;
    }

    public void ShowJoinMatchError()
    {
        joinMatchError.SetActive(true);
    }

    public void ResetMenu()
    {
        joinMatchError.SetActive(false);
    }

}
