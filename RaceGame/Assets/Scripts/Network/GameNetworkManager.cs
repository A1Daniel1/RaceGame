using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JoinLobbyData
{
    public string playerId;
    public string lobbyId;
    public string username;
}

[Serializable]
public class GameStateData
{
    public GameStatePlayerEntry[] entries;
}

[Serializable]
public class GameStatePlayerEntry
{
    public string playerId;
    public float posicionX;
    public float posicionY;
    public float posicionZ;
}

[Serializable]
public class RaceFinishedData
{
    public string winnerId;
    public string winnerUsername;
}

[Serializable]
public class PlayerPosition
{
    public float posicionX;
    public float posicionY;
    public float posicionZ;
}

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    public event Action OnConnected;
    public event Action OnRaceStarted;
    public event Action<string, string> OnRaceFinished;
    public event Action<Dictionary<string, PlayerPosition>> OnGameStateReceived;
    public event Action<string> OnError;

    public string PlayerId { get; private set; }
    public string LobbyId { get; private set; }
    public bool InRace { get; private set; }

    private WebSocketClient _wsClient;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _wsClient = GetComponent<WebSocketClient>();
        if (_wsClient == null) _wsClient = gameObject.AddComponent<WebSocketClient>();

        _wsClient.OnConnected += HandleConnected;
        _wsClient.OnMessage += HandleMessage;
        _wsClient.OnDisconnected += HandleDisconnected;
        _wsClient.OnError += HandleError;
    }

    public void ConnectToGameServer(string gameServerUrl = null)
    {
        string url = gameServerUrl ?? NetworkConfig.GameServerUrl;
        Debug.Log("[GameNetwork] Conectando a " + url);
        _wsClient.Connect(url);
    }

    public void JoinLobby(string lobbyId, string playerId, string username)
    {
        LobbyId = lobbyId;
        PlayerId = playerId;

        JoinLobbyData data = new JoinLobbyData
        {
            playerId = playerId,
            lobbyId = lobbyId,
            username = username
        };

        _wsClient.SendEvent("JOIN_LOBBY", JsonUtility.ToJson(data));
    }

    public void SendPosition(float x, float y, float rotation)
    {
        if (!InRace || string.IsNullOrEmpty(PlayerId)) return;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "playerId", PlayerId },
            { "posicionX", x },
            { "posicionY", y },
            { "posicionZ", rotation }
        };

        string json = JsonUtility.ToJson(new PlayerPosition
        {
            posicionX = x,
            posicionY = y,
            posicionZ = rotation
        });

        _wsClient.SendEvent("UPDATE_INPUT", json);
    }

    private void HandleConnected()
    {
        Debug.Log("[GameNetwork] Conectado al GameServer");
        OnConnected?.Invoke();
    }

    private void HandleDisconnected()
    {
        Debug.Log("[GameNetwork] Desconectado del GameServer");
        InRace = false;
    }

    private void HandleError(string error)
    {
        Debug.LogError("[GameNetwork] Error: " + error);
        OnError?.Invoke(error);
    }

    private void HandleMessage(string eventName, string data)
    {
        Debug.Log($"[GameNetwork] Evento: {eventName}");

        switch (eventName)
        {
            case "JOIN_LOBBY":
                HandleJoinLobby(data);
                break;
            case "START_RACE":
                HandleStartRace();
                break;
            case "GAME_STATE":
                HandleGameState(data);
                break;
            case "RACE_FINISHED":
                HandleRaceFinished(data);
                break;
            case "ERROR":
                OnError?.Invoke(data);
                break;
        }
    }

    private void HandleJoinLobby(string data)
    {
        JoinLobbyData lobbyData = JsonUtility.FromJson<JoinLobbyData>(data);
        PlayerId = lobbyData.playerId;
        LobbyId = lobbyData.lobbyId;
        Debug.Log($"[GameNetwork] Unido al lobby {LobbyId} como {PlayerId}");
    }

    private void HandleStartRace()
    {
        InRace = true;
        Debug.Log("[GameNetwork] Carrera iniciada!");
        OnRaceStarted?.Invoke();
    }

    private void HandleGameState(string data)
    {
        try
        {
            Dictionary<string, PlayerPosition> positions = ParseGameState(data);
            OnGameStateReceived?.Invoke(positions);
        }
        catch (Exception e)
        {
            Debug.LogError("[GameNetwork] Error parseando game state: " + e.Message);
        }
    }

    private void HandleRaceFinished(string data)
    {
        InRace = false;
        RaceFinishedData finished = JsonUtility.FromJson<RaceFinishedData>(data);
        Debug.Log($"[GameNetwork] Carrera terminada! Ganador: {finished.winnerUsername}");
        OnRaceFinished?.Invoke(finished.winnerId, finished.winnerUsername);
    }

    private Dictionary<string, PlayerPosition> ParseGameState(string json)
    {
        var result = new Dictionary<string, PlayerPosition>();

        var wrapper = JsonUtility.FromJson<GameStateWrapper>(json);

        if (wrapper.players != null)
        {
            foreach (var entry in wrapper.players)
            {
                result[entry.playerId] = new PlayerPosition
                {
                    posicionX = entry.posicionX,
                    posicionY = entry.posicionY,
                    posicionZ = entry.posicionZ
                };
            }
        }

        return result;
    }

    [Serializable]
    private class GameStateWrapper
    {
        public GameStatePlayerEntry[] players;
    }
}
