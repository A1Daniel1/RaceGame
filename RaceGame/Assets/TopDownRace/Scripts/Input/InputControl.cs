using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownRace
{
    public class InputControl : MonoBehaviour
    {
        [HideInInspector]
        public Vector3 m_Movement;

        public static InputControl m_Main;

        private Keyboard m_Keyboard;
        private Gamepad m_Gamepad;

        void Awake()
        {
            m_Main = this;
        }

        void OnEnable()
        {
            // Suscribirse a los cambios de dispositivos de entrada
            InputSystem.onDeviceChange += OnDeviceChange;
            UpdateInputDevices();
        }

        void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            UpdateInputDevices();
        }

        private void UpdateInputDevices()
        {
            m_Keyboard = Keyboard.current;
            m_Gamepad = Gamepad.current;
        }

        void Update()
        {
            m_Movement = Vector3.zero;

            // Leer entrada de teclado
            if (m_Keyboard != null)
            {
                // Movimiento horizontal (A/D o flechas izquierda/derecha)
                if (m_Keyboard.dKey.isPressed || m_Keyboard.rightArrowKey.isPressed)
                    m_Movement.x = 1f;
                else if (m_Keyboard.aKey.isPressed || m_Keyboard.leftArrowKey.isPressed)
                    m_Movement.x = -1f;

                // Movimiento vertical (W/S o flechas arriba/abajo)
                if (m_Keyboard.wKey.isPressed || m_Keyboard.upArrowKey.isPressed)
                    m_Movement.y = 1f;
                else if (m_Keyboard.sKey.isPressed || m_Keyboard.downArrowKey.isPressed)
                    m_Movement.y = -1f;
            }

            // Leer entrada del Gamepad/Joystick (reemplaza la entrada de teclado si se está usando)
            if (m_Gamepad != null)
            {
                Vector2 gamepadInput = m_Gamepad.leftStick.ReadValue();
                if (gamepadInput.magnitude > 0.1f)
                {
                    m_Movement.x = gamepadInput.x;
                    m_Movement.y = gamepadInput.y;
                }
            }

            // Normalizar el vector de movimiento para evitar velocidad diagonal aumentada
            m_Movement = Vector3.ClampMagnitude(m_Movement, 1.0f);
        }
    }
}