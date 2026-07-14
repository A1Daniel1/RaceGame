using System.Collections.Generic;
using UnityEngine;

namespace TopDownRace
{
    public class MultiplayerSync : MonoBehaviour
    {
        public static MultiplayerSync Instance { get; private set; }

        [Header("Referencias")]
        [SerializeField] private GameObject rivalCarPrefab;

        private string myPlayerId;
        private Dictionary<string, GameObject> remoteCars = new Dictionary<string, GameObject>();
        private Dictionary<string, Vector3> targetPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, float> targetRotations = new Dictionary<string, float>();

        private float sendTimer;
        private float sendInterval = 0.033f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            myPlayerId = GameManager.Instance.PlayerId;
        }

        private void Update()
        {
            SendLocalPosition();

            foreach (var kvp in targetPositions)
            {
                if (remoteCars.TryGetValue(kvp.Key, out GameObject car))
                {
                    car.transform.position = Vector3.Lerp(car.transform.position, kvp.Value, Time.deltaTime * 15f);

                    if (targetRotations.TryGetValue(kvp.Key, out float rot))
                    {
                        float currentRot = car.transform.eulerAngles.z;
                        car.transform.eulerAngles = new Vector3(0, 0, Mathf.LerpAngle(currentRot, rot, Time.deltaTime * 15f));
                    }
                }
            }
        }

        private void SendLocalPosition()
        {
            sendTimer += Time.deltaTime;
            if (sendTimer < sendInterval) return;
            sendTimer = 0f;

            if (GameNetworkManager.Instance == null || !GameNetworkManager.Instance.InRace) return;

            PlayerCar player = PlayerCar.m_Current;
            if (player == null) return;

            Vector3 pos = player.transform.position;
            float rot = player.transform.eulerAngles.z;

            GameNetworkManager.Instance.SendPosition(pos.x, pos.y, rot);
        }

        public void OnGameStateReceived(Dictionary<string, PlayerPosition> players)
        {
            foreach (var kvp in players)
            {
                if (kvp.Key == myPlayerId) continue;

                Vector3 newPos = new Vector3(kvp.Value.posicionX, kvp.Value.posicionY, 0f);
                float newRot = kvp.Value.posicionZ;

                targetPositions[kvp.Key] = newPos;
                targetRotations[kvp.Key] = newRot;

                if (!remoteCars.ContainsKey(kvp.Key))
                {
                    SpawnRemoteCar(kvp.Key, newPos);
                }
            }
        }

        private void SpawnRemoteCar(string playerId, Vector3 position)
        {
            if (rivalCarPrefab == null)
            {
                Debug.LogError("[MultiplayerSync] rivalCarPrefab no asignado");
                return;
            }

            GameObject car = Instantiate(rivalCarPrefab, position, Quaternion.identity);
            car.tag = "Rival";
            car.name = "RemoteCar_" + playerId;

            CarPhysics physics = car.GetComponent<CarPhysics>();
            if (physics != null)
            {
                physics.m_SpeedForce = 0;
            }

            Rivals rivals = car.GetComponent<Rivals>();
            if (rivals != null)
            {
                rivals.m_Control = false;
            }

            remoteCars[playerId] = car;
        }

        public void RemoveAllRemoteCars()
        {
            foreach (var car in remoteCars.Values)
            {
                if (car != null) Destroy(car);
            }
            remoteCars.Clear();
            targetPositions.Clear();
            targetRotations.Clear();
        }
    }
}
