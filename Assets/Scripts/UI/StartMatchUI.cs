using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;

/*Steamworks.NET license:

The MIT License (MIT)

Copyright (c) 2013-2018 Riley Labrecque

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,	
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

// @class StartMatchUI
// @desc Controls the UI for the Start Match screen
public class StartMatchUI : MonoBehaviour {

	//Change this to be the app id of your game, matching that in steam_appid
	public static CGameID APP_ID = (CGameID) 480;

    // UI elements
	public GameObject matchName;
    public GameObject playerLobbyName;
    public GameObject playerLobby;
    public GameObject inviteSteamFriendsButton;
    public GameObject steamFriendsLobbyElement;
    public GameObject steamFriendsLobby;
    public GameObject steamFriendButtonPrefab;

    private Text m_playerLobbyText;

    // Steam friends information
	private string m_playerSteamName;
    private bool steamFriendsListActive = false;
	private Dictionary<string, EPersonaState> steamFriendsStates = new Dictionary<string, EPersonaState>();
	private Dictionary<string, CSteamID> steamFriendsIDs = new Dictionary<string, CSteamID> ();

    // Maps available Steam friends to the game object representing it in the scrollable friends list
    private Dictionary<string, GameObject> steamFriendsObjects;


    void Start () {

        m_playerLobbyText = playerLobbyName.GetComponent<Text>();

        // Initialize steam friends information
        steamFriendsObjects = new Dictionary<string, GameObject>();
        HideSteamFriendsList();

        m_playerSteamName = FpsClient.GameClient.Instance.MainPlayerName;

		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		for (int i = 0; i < friendCount; ++i) {
			CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendName = SteamFriends.GetFriendPersonaName(friendSteamId);
			EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendSteamId);

			// Add friend and their current state to the dictionary
			steamFriendsStates.Add (friendName, friendState);
			steamFriendsIDs.Add (friendName, friendSteamId);
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
            // Get list of steam friends from the client
			List<string> steamFriends = new List<string>(steamFriendsStates.Keys);

            // Add in new friends
            foreach (string friend in steamFriends)
            {
				// If the friend is not online or looking to play, don't display them
				if (steamFriendsStates [friend] != EPersonaState.k_EPersonaStateOnline && steamFriendsStates [friend] != EPersonaState.k_EPersonaStateLookingToPlay)
					continue;

				// If the friend is not currently playing this game, don't display them
				FriendGameInfo_t pFriendGameInfo = new FriendGameInfo_t();
				if (SteamFriends.GetFriendGamePlayed(steamFriendsIDs[friend], out pFriendGameInfo))
				{
					Debug.Log ("Game Info: " + pFriendGameInfo.m_gameID);
					//if the friend is playing a game other than this game, don't display them
					if (pFriendGameInfo.m_gameID != APP_ID)
						continue;
				}
				// If the friend is not in game, don't display them
				else 
					continue;

				// Otherwise, display the friend in the list of friends to invite
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

            // Remove display of friends who are no longer available
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
		// Get all the friend names and states again
		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		for (int i = 0; i < friendCount; ++i) {
			CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendName = SteamFriends.GetFriendPersonaName(friendSteamId);
			EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendSteamId);

			// If the friend is already stored, update their state
			if (steamFriendsStates.ContainsKey(friendName)){
				steamFriendsStates[friendName] = friendState;
			}
			// If the friend and their state is not already stored, add them to the dictionary
			else {
				steamFriendsStates.Add (friendName, friendState);
			}

			// If the friend is already stored, update their CSteamID
			if (steamFriendsIDs.ContainsKey(friendName)){
				steamFriendsIDs[friendName] = friendSteamId;
			}
			// If the friend and their CSteamID is not already stored, add them to the dictionary
			else {
				steamFriendsIDs.Add (friendName, friendSteamId);
			}
		}
    }

    // Called when the invite button of a specific friend is pressed
    public void InviteSteamFriend(string friend)
    {
		FpsClient.GameClient.Instance.SendPlayerInvite(friend);
		Debug.Log("Invite " + friend);
    }
    public void RefreshPlayerList()
    {
        Debug.Log("Refreshing list of players in lobby");
        FpsClient.GameClient.Instance.SendRefreshPlayerList();
    }
}
