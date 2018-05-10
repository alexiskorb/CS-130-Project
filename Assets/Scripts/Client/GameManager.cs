using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Singleton Game Manager
public class GameManager : MonoBehaviour {

    // Events
    public delegate void MatchDelegate(string playerId, string matchId);
    public static event MatchDelegate createMatchEvent;
    public static event MatchDelegate joinMatchEvent;
    public static event MatchDelegate leaveMatchLobbyEvent;
    public static event MatchDelegate startMatchEvent;
    public static event MatchDelegate gameLoadedEvent;
    public static event MatchDelegate dropMatchEvent;

    // Public variables
    public GameObject mainPlayerPrefab;
    public GameObject playerPrefab;
    public bool isServer = false;
	public Vector3 forwardBackMovement = new Vector3();
	public Vector3 leftRightMovement = new Vector3 ();
	public Vector3 rotation = new Vector3();
	public Transform m_cameraTransform;
	public float horizontalCameraSensitivity = 15f;
	public float verticalCameraSensitivity = 5f;
	public float moveSpeed = 5;

    // Private variables
    private string m_mainPlayerName = "";
    private bool m_mainPlayerNameIsSet = false;
    private string m_matchName = "";
    private bool m_matchNameIsSet = false;
    private bool m_inMatch = false; 
	private bool m_menuOpen = false;
    private string m_gameScene = "";
    private string m_endScene = "";
	private float m_yRotation = 0f;


    // Dictionary mapping player names to player objects
    private Dictionary<string, GameObject> m_players;

    // Singleton instance of the GameManager
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
		forwardBackMovement = new Vector3(0,0,0);
		leftRightMovement = new Vector3(0,0,0);
		m_cameraTransform = transform.Find("Main Camera");
    }

    // Subscribe to sceneLoaded event
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Unsubscribe from sceneLoaded event
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Used to call relevant functions after the scene loads since scene loads complete in the frame after they're called
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Call startMatchEvent when game scene is loaded
        /*
        if (scene.name == m_gameScene)
        {
            if (startMatchEvent != null)
            {
                startMatchEvent(m_mainPlayerName, m_matchName);
            }
        }*/

        // Call gameLoadedEvent when game scene is loaded
        if (scene.name == m_gameScene)
        {
            if (gameLoadedEvent != null)
            {
                gameLoadedEvent(m_mainPlayerName, m_matchName);
            }
        }

        // Call dropMatchEvent and reset game manager when end scene is loaded
        if (scene.name == m_endScene)
        {
            if (dropMatchEvent != null)
            {
                dropMatchEvent(m_mainPlayerName, m_matchName);
            }
            ResetGameManager();
        }
    }


    void Update()
    {
		// Move player
		m_yRotation -= horizontalCameraSensitivity * Input.GetAxis("Mouse X");
		rotation = new Vector3 (0, m_yRotation, 0);

		forwardBackMovement = new Vector3(Mathf.Sin(m_yRotation * Mathf.PI / 180), 0, Mathf.Cos(m_yRotation * Mathf.PI / 180));
		forwardBackMovement *= Input.GetAxis ("Vertical");
		leftRightMovement = new Vector3(Mathf.Sin((m_yRotation + 90) * Mathf.PI / 180), 0, Mathf.Cos((m_yRotation + 90) * Mathf.PI / 180));
		leftRightMovement *= Input.GetAxis ("Horizontal");

        // TEST CODE
        if (m_inMatch)
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

    // Getters and setters
	public bool MenuOpen
    {
		get
        {
			return m_menuOpen;
		}
		set
        {
			m_menuOpen = value;
		}
	}
    public bool InMatch
    {
        get
        {
            return m_inMatch;
        }
        set
        {
            m_inMatch = value;
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
    public List<string> PlayerIds
    {
        get
        {
            return m_players.Keys.ToList();
        }
    }
    public Dictionary<string, GameObject> Players
    {
        get
        {
            return m_players;
        }
        set
        {
            m_players = value;
        }
    }

    // Creates a new match. Fails if match name or player name is not set.
    // Returns true if the match is created. Returns false otherwise
    public bool CreateMatch()
    {
        if(!isServer && m_matchNameIsSet && m_mainPlayerNameIsSet)
        {
            m_players.Clear();
            AddPlayer(m_mainPlayerName);
            if (createMatchEvent != null)
            {
                createMatchEvent(m_mainPlayerName, m_matchName);
            }
            return true;
        }
        return false;
    }

    // Join a match. Fails if match name or player name is not set.
    // Returns true if successful. Returns false otherwise
    public bool JoinMatch()
    {
        if (m_mainPlayerNameIsSet && m_matchNameIsSet)
        {
            AddPlayer(m_mainPlayerName);
            if (joinMatchEvent != null)
            {
                joinMatchEvent(m_mainPlayerName, m_matchName);
            }
            return true;
        }
        return false;
    }

    // Leaves the match lobby
    public void LeaveMatchLobby()
    {
        if (leaveMatchLobbyEvent != null)
        {
            leaveMatchLobbyEvent(m_mainPlayerName, m_matchName);       
        }
        ResetGameManager();
    }

    // Starts a new match in a given scene
    public void StartMatch(string scene)
    {
        m_inMatch = true;
        m_gameScene = scene;
        SceneManager.LoadScene(scene);
        // startMatchEvent will be called after the scene is loaded
    }


    // Drop out of match and return to a given scene
    public void DropMatch(string scene)
    {
        m_inMatch = false;
        m_endScene = scene;
        SceneManager.LoadScene(scene);
        // dropMatchEvent and ResetGameManager will be called after the scene is loaded
    }


    // Resets player and match name and removes main player from player list
    private void ResetGameManager()
    {
        List<string> playerIds = new List<string>(m_players.Keys);
        foreach (string playerId in playerIds)
        {
            RemovePlayer(playerId);
        }
        MainPlayerName = "";
        MatchName = "";
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
        return false;
    }

    // Removes player from match
    // Returns true if the player exists and was destroyed
    // Returns false if the player did not exist
    public bool RemovePlayer(string playerId)
    {
        // If the player exists, destroy them and remove them from list
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
    // If the playerId is equal to the main player ID, then spawn a main player
    public void SpawnPlayer(string playerId, Vector3 spawnPosition)
    {
        // If the player exists, spawn them in
        if (m_players.ContainsKey(playerId))
        {
            // If the player object doesn't exist, create it
            if (!m_players[playerId])
            { 
                if (playerId == m_mainPlayerName)
                {
                    m_players[playerId] = Instantiate(mainPlayerPrefab);
                }
                else
                {
                    m_players[playerId] = Instantiate(playerPrefab);
                }
            }
            m_players[playerId].transform.position = spawnPosition;
        }
    }

    // Moves a player with the given ID to the given new position
	public void MovePlayer(string playerId, Vector3 newFB, Vector3 newLR)
    {
		Debug.Log ("About to move: " + playerId);
        if (m_players.ContainsKey(playerId))
        {
			m_players[playerId].GetComponent<Rigidbody>().velocity = moveSpeed * (newFB + newLR);   
        }
    }

    // Rotates a player with the given ID to the given new rotation
    // Only rotates along y-axis
    public void RotatePlayer(string playerId, Vector3 newRotation)
    {
        if (m_players.ContainsKey(playerId))
        {
            m_players[playerId].transform.eulerAngles = new Vector3(0, newRotation[1], 0);
        }
    }


}
