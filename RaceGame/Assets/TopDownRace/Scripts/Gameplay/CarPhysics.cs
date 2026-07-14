using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TopDownRace
{
    public class CarPhysics : MonoBehaviour
    {
        [HideInInspector]
        public Rigidbody2D m_Body;

        [HideInInspector]
        public float m_InputAccelerate = 0;
        [HideInInspector]
        public float m_InputSteer = 0;

        public float m_SpeedForce = 80;

        public GameObject m_TireTracks;
        public Transform m_TireMarkPoint;
        // Start is called before the first frame update
        void Start()
        {
            m_Body = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            Vector2 velocity = m_Body.linearVelocity;
            Vector2 forward = Helper.ToVector2(transform.right);
            float delta = Vector2.SignedAngle(forward, velocity);
            if (velocity.magnitude > 10 && Mathf.Abs(delta) > 20)
            {
                GameObject obj = Instantiate(m_TireTracks);
                obj.transform.position = m_TireMarkPoint.position;
                obj.transform.rotation = m_TireMarkPoint.rotation;
                Destroy(obj, 2);
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            Vector3 forward = Quaternion.Euler(0, 0, m_Body.rotation) * Vector3.right;

            m_Body.AddForce(m_InputAccelerate * m_SpeedForce * Helper.ToVector2(forward), ForceMode2D.Impulse);

            Vector3 right = Quaternion.Euler(0, 0, 90) * forward;
            Vector3 project1 = Vector3.Project(Helper.ToVector3(m_Body.linearVelocity), right);

            m_Body.linearVelocity -= .02f * Helper.ToVector2(project1);

            m_Body.angularVelocity += 40 * m_InputSteer;



            m_InputAccelerate = 0;
            m_InputSteer = 0;
        }
    }
}