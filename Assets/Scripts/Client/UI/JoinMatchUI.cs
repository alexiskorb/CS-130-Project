using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JoinMatchUI : MonoBehaviour {

    public GameObject playerNameInput;
    public GameObject joinMatchError;
    public GameObject matchLobby;
    public GameObject matchButtonPrefab;
    public GameObject matchTextPrefab;

    private Dictionary<string, GameObject> openMatchTexts;

    // Use this for initialization
    void Start ()
    {
        joinMatchError.SetActive(false);
        openMatchTexts = new Dictionary<string, GameObject>();
	}

    // Update is called once per frame
    void Update ()
    {
        // Get list of open matches from the client
        List<string> openMatches = ClientManager.Instance.GetOpenMatches();

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

        // Delete old matches
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
        ClientManager.Instance.RequestOpenMatchList();
    }

    public void SubmitMainPlayerName()
    {
        string name = playerNameInput.GetComponent<InputField>().text;
        GameManager.Instance.MainPlayerName = name;
    }

    public void SubmitMatchName(string name)
    {
        GameManager.Instance.MatchName = name;
    }

    public void ShowJoinMatchError()
    {
        joinMatchError.SetActive(true);
    }

    public void ResetMenu()
    {
        playerNameInput.GetComponent<InputField>().text = "";
        joinMatchError.SetActive(false);
    }
}