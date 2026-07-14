using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TopDownRace
{
    public class Checkpoint : MonoBehaviour
    {

        public int m_ID;
        [HideInInspector]
        public bool isPassed;

        public bool isFinishLine;
        // Start is called before the first frame update
        void Start()
        {
            isPassed = false;

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.tag == "Player")
            {
                if (PlayerCar.m_Current.m_CurrentCheckpoint == m_ID)
                {
                    if (m_ID == 0)
                    {
                        GameControl.m_Current.m_FinishedLaps++;
                        if (!GameControl.m_Current.PlayerLapEndCheck())
                        {
                            PlayerCar.m_Current.m_CurrentCheckpoint = 1;
                        }

                    }
                    else
                    {
                        PlayerCar.m_Current.m_CurrentCheckpoint = m_ID + 1;
                        if (PlayerCar.m_Current.m_CurrentCheckpoint > RaceTrackControl.m_Main.m_Checkpoints.Length - 1)
                        {
                            PlayerCar.m_Current.m_CurrentCheckpoint = 0;
                        }

                    }

                }
            }
            else if (collision.gameObject.tag == "Rival")
            {
                collision.gameObject.GetComponent<Rivals>().Checkpoint(m_ID);
            }
        }
    }
}