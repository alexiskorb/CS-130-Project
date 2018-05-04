using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Singleton Game Manager
public class GameManager : MonoBehaviour {

    public string startingScenePath;

    public GameObject mainPlayerPrefab;
    public GameObject playerPrefab;

    private string m_mainPlayerName = "";
    private bool m_mainPlayerNameIsSet = false;
    private string m_matchName = "";
    private bool m_matchNameIsSet = false;
    private bool m_matchReady = false;
    private bool m_matchStarted = false; 
	private bool m_menuOpen = false;

    private Dictionary<string, GameObject> m_players;

    private static GameManager m_instance = null;

    // Get instance of the GameManager
    public static GameManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<GameManager>();
                if (m_instance == null)
                {
                    GameObject gm = new GameObject();
                    gm.name = "GameManager";
                    m_instance = gm.AddComponent<GameManager>();
                    DontDestroyOnLoad(gm);
                }
            }
            return m_instance;
        }
    }

    // Enforce singleton behavior
    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Setup variables
        m_players = new Dictionary<string, GameObject>();

        // Load into main menu at start of game
        SceneManager.LoadScene(startingScenePath);
    }

    void Update()
    {
        // TEST CODE
        if (m_matchStarted)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                AddPlayer("noob");
                SpawnPlayer("noob", new Vector3(10, 1, 10));
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                RemovePlayer("noob");
            }
        }
        // END TEST CODE
    }

	public bool MenuOpen {
		get {
			return m_menuOpen;
		}
		set {
			m_menuOpen = value;
		}
	}

    public string MainPlayerName
    {
        get
        {
            return m_mainPlayerName;
        }
        set
        {
            m_mainPlayerName = value;
            if (value != "")
            {
                m_mainPlayerNameIsSet = true;
            }
            else
            {
                m_mainPlayerNameIsSet = false;
            }
        }
    }

    public string MatchName
    {
        get
        {
            return m_matchName;
        }
        set
        {
            m_matchName = value;
            if (value != "")
            {
                m_matchNameIsSet = true;
            }
            else
            {
                m_matchNameIsSet = false;
            }
        }
    }

    public bool MatchReady
    {
        get
        {
            return m_mainPlayerNameIsSet && m_matchNameIsSet;
        }
    }

    public List<string> Players
    {
        get
        {
            return m_players.Keys.ToList();
        }
    }

    // Creates a new match
    public void CreateMatch()
    {
        m_players.Clear();
        AddPlayer(m_mainPlayerName);
    }

    // Starts a new match in a given scene
    public void StartMatch(string scene)
    {
        m_matchStarted = true;
        SceneManager.LoadScene(scene);
    }

    // Drop out of match and return to a given scene
    public void DropMatch(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    // Adds player to match
    // Returns true if player is a new player.
    // Returns false if player is already in match
    public bool AddPlayer(string playerId)
    {
        if (!m_players.ContainsKey(playerId))
        {
            m_players.Add(playerId, null);
            return true;
        }
        else
        {
            return false;
        }
    }

    // Removes player from match
    // Returns true if the player exists and was destroyed
    // Returns false if the player did not exist
    public bool RemovePlayer(string playerId)
    {
        // If the player exists, destroy them
        if (m_players.ContainsKey(playerId))
        {
            if (m_players[playerId])
            {
                Destroy(m_players[playerId]);
            }
            m_players.Remove(playerId);
            return true;
        }
        return false;
    }

    // Instantiates a player with a given ID at a given spawn point
    public void SpawnPlayer(string playerId, Vector3 spawnPosition)
    {
        // If the player exists, spawn them in
        if (m_players.ContainsKey(playerId))
        {
            // If the player object doesn't exist, create it
            if (!m_players[playerId])
            {
                m_players[playerId] = Instantiate(playerPrefab);
            }
            m_players[playerId].transform.position = spawnPosition;
        }
    }

    // Moves a player with the given ID to the given new position
    public void MovePlayer(string playerId, Vector3 newPosition)
    {
        if (m_players.ContainsKey(playerId))
        {
            m_players[playerId].transform.position = newPosition;
        }
    }


}
