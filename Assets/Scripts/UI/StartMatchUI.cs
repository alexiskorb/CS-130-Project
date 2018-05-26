using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;

public class StartMatchUI : MonoBehaviour {

    public GameObject matchName;
    public GameObject playerLobbyName;
    public GameObject playerLobby;
    public GameObject inviteSteamFriendsButton;
    public GameObject steamFriendsLobbyElement;
    public GameObject steamFriendsLobby;
    public GameObject steamFriendButtonPrefab;

    private Text m_playerLobbyText;
	private string m_playerSteamName;

    private bool steamFriendsListActive = false;
	private Dictionary<string, EPersonaState> steamFriendsStates = new Dictionary<string, EPersonaState>();
    private Dictionary<string, GameObject> steamFriendsObjects;

    // Use this for initialization
    void Start () {
        m_playerLobbyText = playerLobbyName.GetComponent<Text>();
        steamFriendsObjects = new Dictionary<string, GameObject>();
        HideSteamFriendsList();

		if (SteamManager.Initialized) {
			m_playerSteamName = SteamFriends.GetPersonaName ();
			Debug.Log (m_playerSteamName);
		} else {
			//TODO: handle error when Steamworks isn't working/Steam Manager didn't get initialized
		}

		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		for (int i = 0; i < friendCount; ++i) {
			CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendName = SteamFriends.GetFriendPersonaName(friendSteamId);
			EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendSteamId);

			//Add friend and their current state to the dictionary
			steamFriendsStates.Add (friendName, friendState);
		}
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
			List<string> steamFriends = new List<string>(steamFriendsStates.Keys);

            // Add in new friends
            foreach (string friend in steamFriends)
            {
				//if the friend is not online or looking to play, don't display them
				if (steamFriendsStates [friend] != EPersonaState.k_EPersonaStateOnline && steamFriendsStates [friend] != EPersonaState.k_EPersonaStateLookingToPlay)
					continue;

				//otherwise, display them in the list of friends to invite
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

			//TODO: for testing, display player's own steam name so they can invite themselves and hopefully receive a popup
			if (!steamFriendsObjects.ContainsKey(m_playerSteamName))
			{
				GameObject steamFriendButton = (GameObject)Instantiate(steamFriendButtonPrefab);
				steamFriendButton.transform.SetParent(steamFriendsLobby.transform);
				Button inviteButton = steamFriendButton.GetComponentInChildren<Button>();
				inviteButton.onClick.AddListener(() => { InviteSteamFriend(m_playerSteamName); });
				Text matchText = steamFriendButton.GetComponentInChildren<Text>();
				matchText.text = m_playerSteamName;
				steamFriendsObjects.Add(m_playerSteamName, steamFriendButton);
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
		
    public void RefreshSteamFriends()
    {
		//get all the friend names and states again
		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		for (int i = 0; i < friendCount; ++i) {
			CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendName = SteamFriends.GetFriendPersonaName(friendSteamId);
			EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendSteamId);

			//if the friend is already stored, update their state
			if (steamFriendsStates.ContainsKey(friendName)){
				steamFriendsStates[friendName] = friendState;
			}
			//if the friend + their state is not already stored, add them to the dictionary
			else {
				steamFriendsStates.Add (friendName, friendState);
			}
		}
    }

    // MELODIE: TODO
    public void InviteSteamFriend(string friend)
    {
		FpsClient.GameClient.Instance.SendInvitePlayer (friend);
		Debug.Log("Invite " + friend);
    }
}
