using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Singleton Game Manager
public class GameManager : MonoBehaviour {

    public GameObject mainPlayerPrefab;
    public GameObject playerPrefab;

    private Dictionary<string, GameObject> m_players;
    private List<string> m_playersInMatchList;

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
        m_playersInMatchList = new List<string>();

        // Load into main menu at start of game
        SceneManager.LoadScene("MainMenu");
    }

    void Update()
    {
        // TEST CODE
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SpawnPlayer("noob", new Vector3(10, 0, 10));
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            RemovePlayer("noob");
        }
        // END TEST CODE
    }

    // Starts a new match in a given scene
    public void StartMatch(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    // Drop out of match and return to a given scene
    public void DropMatch(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    // Returns the players in the matchmaking list
    public List<string> PlayersInMatchList
    {
        get
        {
            return m_playersInMatchList;
        }
    }

    // Adds player to list during matchmaking
    public void AddPlayerToMatchList(string playerId)
    {
        m_playersInMatchList.Add(playerId);
    }

    // Remove player from list during matchmaking
    public void RemovePlayerFromMatchList(string playerId)
    {
        m_playersInMatchList.Remove(playerId);
    }

    // Instantiates a player with a given ID at a given spawn point
    public void SpawnPlayer(string playerId, Vector3 spawnPosition)
    {
        // If the player doesn't exist, add them in
        if (!m_players.ContainsKey(playerId))
        {
            GameObject newPlayer = Instantiate(playerPrefab);
            m_players.Add(playerId, newPlayer);
            newPlayer.transform.position = spawnPosition;
        }
        // Otherwise destroy the player and spawn a new one
        else
        {
            Destroy(m_players[playerId]);
            m_players.Remove(playerId);
            GameObject newPlayer = Instantiate(playerPrefab);
            m_players.Add(playerId, newPlayer);
            newPlayer.transform.position = spawnPosition;
        }
    }

    // Destroys a player with the given ID
    public void RemovePlayer(string playerId)
    {
        // If the player exists, destroy them
        if (m_players.ContainsKey(playerId))
        {
            Destroy(m_players[playerId]);
            m_players.Remove(playerId);
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
