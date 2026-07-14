using System;
using System.Collections;
using System.Text;
using UnityEngine;

[Serializable]
public class MatchmakingJoinResponse
{
    public bool success;
    public MatchmakingData data;
}

[Serializable]
public class MatchmakingData
{
    public string status;
    public int position;
    public string lobbyId;
    public string gameServerUrl;
}

[Serializable]
public class MatchmakingLeaveRequest
{
    public string playerId;
}

public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance { get; private set; }

    public event Action OnMatchFound;
    public event Action<int> OnQueueUpdate;
    public event Action<string> OnMatchError;

    public string LobbyId { get; private set; }
    public string GameServerUrl { get; private set; }
    public bool IsSearching { get; private set; }

    private string _playerId;
    private Coroutine _pollCoroutine;

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

    public void JoinQueue(string playerId, string username)
    {
        _playerId = playerId;
        IsSearching = true;

        string json = JsonUtility.ToJson(new MatchmakingJoinRequest { playerId = playerId, username = username });
        ApiClient.Instance.Post("/api/matchmaking/join", json, OnJoinResponse, (err) =>
        {
            IsSearching = false;
            OnMatchError?.Invoke(err);
        });
    }

    public void LeaveQueue()
    {
        if (string.IsNullOrEmpty(_playerId)) return;

        IsSearching = false;
        if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);

        string json = JsonUtility.ToJson(new MatchmakingLeaveRequest { playerId = _playerId });
        ApiClient.Instance.Delete("/api/matchmaking/leave", json, null);
    }

    private void OnJoinResponse(string response)
    {
        MatchmakingJoinResponse joinResponse = JsonUtility.FromJson<MatchmakingJoinResponse>(response);

        if (!joinResponse.success)
        {
            IsSearching = false;
            OnMatchError?.Invoke("Error al unirse a la cola");
            return;
        }

        if (joinResponse.data.status == "ready")
        {
            LobbyId = joinResponse.data.lobbyId;
            GameServerUrl = joinResponse.data.gameServerUrl;
            IsSearching = false;
            OnMatchFound?.Invoke();
        }
        else
        {
            OnQueueUpdate?.Invoke(joinResponse.data.position);
            _pollCoroutine = StartCoroutine(PollStatus());
        }
    }

    private IEnumerator PollStatus()
    {
        while (IsSearching)
        {
            yield return new WaitForSeconds(2f);

            string url = $"/api/matchmaking/status/{_playerId}";
            ApiClient.Instance.Get(url, OnStatusResponse, (err) =>
            {
                Debug.LogError("Error polling matchmaking: " + err);
            });
        }
    }

    private void OnStatusResponse(string response)
    {
        MatchmakingJoinResponse statusResponse = JsonUtility.FromJson<MatchmakingJoinResponse>(response);

        if (!statusResponse.success) return;

        if (statusResponse.data.status == "ready")
        {
            LobbyId = statusResponse.data.lobbyId;
            GameServerUrl = statusResponse.data.gameServerUrl;
            IsSearching = false;
            if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);
            OnMatchFound?.Invoke();
        }
        else if (statusResponse.data.status == "waiting")
        {
            OnQueueUpdate?.Invoke(statusResponse.data.position);
        }
        else if (statusResponse.data.status == "not_found")
        {
            IsSearching = false;
            if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);
            OnMatchError?.Invoke("Saliste de la cola");
        }
    }

    [Serializable]
    private class MatchmakingJoinRequest
    {
        public string playerId;
        public string username;
    }
}
