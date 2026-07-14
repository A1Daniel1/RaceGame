using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TopDownRace
{
    public class CameraFollow : MonoBehaviour
    {
        private Vector3 m_Offset = new Vector3(0, 0, -10);
        public float m_SmoothTime;
        private Vector3 m_Velocity = Vector3.zero;
        [SerializeField]
        private Transform m_Target;

        void Start()
        {

        }
        void FixedUpdate()
        {
            m_Target = PlayerCar.m_Current.transform;

            Vector3 targetPosition = m_Target.position + m_Offset;
            Vector3 forwardOffset = 20.0f * (m_Target.rotation * Vector3.right);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition + forwardOffset, ref m_Velocity, m_SmoothTime);
        }
    }
}