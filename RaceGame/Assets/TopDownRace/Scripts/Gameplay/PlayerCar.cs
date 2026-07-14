using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace TopDownRace
{
    public class PlayerCar : MonoBehaviour
    {

        [HideInInspector]
        public float m_Speed;

        [HideInInspector]
        public int m_CurrentCheckpoint;


        [HideInInspector]
        public bool m_Control = false;

        public static PlayerCar m_Current;

        void Awake()
        {
            m_Current = this;
        }
        // Start is called before the first frame update
        void Start()
        {
            m_CurrentCheckpoint = 1;
            m_Control = true;
            m_Speed = 80;
        }

        // Update is called once per frame
        // Update is called once per frame
        void Update()
        {
            if (GameControl.m_Current != null && GameControl.m_Current.m_StartRace)
            {
                if (m_Control && InputControl.m_Main != null)
                {
                    // Leer el vector limpio que procesó el InputControl con el nuevo sistema
                    Vector3 move = InputControl.m_Main.m_Movement;

                    GetComponent<CarPhysics>().m_InputAccelerate = move.y;

                    if (Mathf.Abs(move.y) > 0.05f || Mathf.Abs(move.x) > 0.05f)
                    {
                        GetComponent<CarPhysics>().m_InputSteer = move.x;
                    }
                    else
                    {
                        GetComponent<CarPhysics>().m_InputSteer = 0f;
                    }
                }
            }
        }

    }

}
