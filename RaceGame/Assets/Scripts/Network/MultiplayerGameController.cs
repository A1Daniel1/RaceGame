using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownRace
{
    public class MultiplayerGameController : MonoBehaviour
    {
        public static MultiplayerGameController Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject playerCarPrefab;
        [SerializeField] private GameObject rivalCarPrefab;

        [Header("Configuracion")]
        [SerializeField] private int totalLaps = 3;

        private GameObject playerCar;
        private Dictionary<string, GameObject> allCars = new Dictionary<string, GameObject>();
        private bool raceStarted;
        private bool raceFinished;
        private int startTimer;

        public int PlayerPosition { get; private set; }
        public int TotalLaps => totalLaps;
        public bool RaceStarted => raceStarted;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GameNetworkManager.Instance.OnRaceStarted += OnRaceStarted;
            GameNetworkManager.Instance.OnRaceFinished += OnRaceFinished;
            GameNetworkManager.Instance.OnGameStateReceived += OnGameStateReceived;

            GameNetworkManager.Instance.ConnectToGameServer();
        }

        private void OnDestroy()
        {
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.OnRaceStarted -= OnRaceStarted;
                GameNetworkManager.Instance.OnRaceFinished -= OnRaceFinished;
                GameNetworkManager.Instance.OnGameStateReceived -= OnGameStateReceived;
            }
        }

        private void OnRaceStarted()
        {
            StartCoroutine(Co_StartRace());
        }

        private void OnRaceFinished(string winnerId, string winnerUsername)
        {
            raceFinished = true;

            bool won = winnerId == GameNetworkManager.Instance.PlayerId;

            PlayerCar player = PlayerCar.m_Current;
            if (player != null) player.m_Control = false;

            if (won)
            {
                UISystem.ShowUI("win-ui");
            }
            else
            {
                UISystem.ShowUI("lose-ui");
            }

            StartCoroutine(Co_ReturnToLobby());
        }

        private void OnGameStateReceived(Dictionary<string, PlayerPosition> positions)
        {
            if (MultiplayerSync.Instance != null)
            {
                MultiplayerSync.Instance.OnGameStateReceived(positions);
            }
        }

        IEnumerator Co_StartRace()
        {
            startTimer = 3;
            yield return new WaitForSeconds(1.5f);
            startTimer--;
            yield return new WaitForSeconds(1f);
            startTimer--;
            yield return new WaitForSeconds(1f);
            startTimer--;
            raceStarted = true;

            if (playerCar != null)
            {
                PlayerCar pc = playerCar.GetComponent<PlayerCar>();
                if (pc != null) pc.m_Control = true;
            }
        }

        IEnumerator Co_ReturnToLobby()
        {
            yield return new WaitForSeconds(5f);

            if (MultiplayerSync.Instance != null)
            {
                MultiplayerSync.Instance.RemoveAllRemoteCars();
            }

            SceneManager.LoadScene("LobbyWaitting");
        }

        public int GetStartTimer()
        {
            return startTimer;
        }
    }
}
