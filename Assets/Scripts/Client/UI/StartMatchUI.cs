using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartMatchUI : MonoBehaviour {

    public GameObject matchName;
    public GameObject playerLobbyName;
    public GameObject playerLobby;
    public GameObject inviteSteamFriendsButton;
    public GameObject steamFriendsLobbyElement;
    public GameObject steamFriendsLobby;
    public GameObject steamFriendButtonPrefab;

    private Text m_playerLobbyText;

    private bool steamFriendsListActive = false;
    private Dictionary<string, GameObject> steamFriendsObjects;

    // Use this for initialization
    void Start () {
        m_playerLobbyText = playerLobbyName.GetComponent<Text>();
        steamFriendsObjects = new Dictionary<string, GameObject>();
        HideSteamFriendsList();
    }

    void Update ()
    {
        // Update match name
        matchName.GetComponent<Text>().text = "Match: " + FpsClient.GameClient.Instance.CurrentLobby;

        // Update player lobby text
        m_playerLobbyText.text = "";
        foreach (string playerID in FpsClient.GameClient.Instance.LobbyPlayers)
        {
            m_playerLobbyText.text += playerID + "\n";
        }

        // Update steam friends list
        if (steamFriendsListActive)
        {
            // MELODIE: TOOD
            // Get list of steam friends from the client
            List<string> steamFriends = new List<string> { "friend1", "friend2", "friend3", "friend4" };  // TEMP CODE

            // Add in new friends
            foreach (string friend in steamFriends)
            {
                if (!steamFriendsObjects.ContainsKey(friend))
                {
                    GameObject steamFriendButton = (GameObject)Instantiate(steamFriendButtonPrefab);
                    steamFriendButton.transform.SetParent(steamFriendsLobby.transform);
                    Button inviteButton = steamFriendButton.GetComponentInChildren<Button>();
                    inviteButton.onClick.AddListener(() => { InviteSteamFriend(friend); });
                    Text matchText = steamFriendButton.GetComponentInChildren<Text>();
                    matchText.text = friend;
                    steamFriendsObjects.Add(friend, steamFriendButton);
                }
            }

            // Delete old friends
            List<string> steamFriendsObjectsKeys = new List<string>(steamFriendsObjects.Keys);
            foreach (string friend in steamFriendsObjectsKeys)
            {
                if (!steamFriends.Contains(friend) && steamFriendsObjects.ContainsKey(friend))
                {
                    Destroy(steamFriendsObjects[friend]);
                    steamFriendsObjects.Remove(friend);
                }
            }
        }
    }

    public void ShowSteamFriendsList()
    {
        playerLobby.transform.Translate(new Vector3(-100, 0, 0));
        inviteSteamFriendsButton.SetActive(false);
        steamFriendsLobbyElement.SetActive(true);
        steamFriendsListActive = true;

    }

    public void HideSteamFriendsList()
    {
        playerLobby.transform.Translate(new Vector3(100, 0, 0));
        inviteSteamFriendsButton.SetActive(true);
        steamFriendsLobbyElement.SetActive(false);
        steamFriendsListActive = false;
    }

    // MELODIE: TODO
    public void RefreshSteamFriends()
    {
        Debug.Log("Refresh friends");
    }

    // MELODIE: TODO
    public void InviteSteamFriend(string friend)
    {
        Debug.Log("Invite " + friend);
    }
}
