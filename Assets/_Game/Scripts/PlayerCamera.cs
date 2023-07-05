/*
 * Author: Leonhard Robin Schnaitl
 * GitHub: https://github.com/leonhardrobin
*/ 
using UnityEngine;

#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace LRS
{
    public class PlayerCamera : MonoBehaviour
    {
        #region PRIVATE MEMBERS
        
        [SerializeField] private Transform _cam;
        [SerializeField] private float _sensitivity = 10;
        [SerializeField] private float clampAngle = 80f;

        private Rigidbody _rb;
        private float _xRotation;
        private float _yRotation;
        
        #if ENABLE_INPUT_SYSTEM 
        private InputAction _look;
        #else
        private const string MOUSE_X_AXIS = "Mouse X";
        private const string MOUSE_Y_AXIS = "Mouse Y";
        #endif
        
        #endregion

        #region PUBLIC MEMBERS

        public bool pauseCameraMovement;

        #endregion

        #region UNITY MESSAGES
        
        // Start is called before the first frame update
        private  void Start()
        {
            #if ENABLE_INPUT_SYSTEM
            _look = GetComponent<PlayerInput>().actions["Look"];
            #endif
            _rb = GetComponent<Rigidbody>();
            if (Camera.main != null)
                _cam ??= Camera.main.transform;
        }

        // Update is called once per frame
        private void Update()
        {
            Rotation();
        }
        
        #endregion

        #region PRIVATE METHODS
        
        private Vector2 GetMouseInput()
        {
            // Get the x and y movement of the mouse and combine it in one variable
            #if ENABLE_INPUT_SYSTEM
            float mouseX = _look.ReadValue<Vector2>().x;
            float mouseY = _look.ReadValue<Vector2>().y;
            #else
            float mouseX = Input.GetAxis(MOUSE_X_AXIS);
            float mouseY = Input.GetAxis(MOUSE_Y_AXIS);
            #endif
            return new Vector2(mouseX, mouseY);
        }

        private void Rotation()
        {
            // pause movement
            if (pauseCameraMovement) return;
            
            // Y Rotation
            _yRotation += GetMouseInput().x * _sensitivity * Time.deltaTime;
            _rb.rotation = Quaternion.Euler(0f, _yRotation, 0f);

            // X Rotation
            _xRotation -= GetMouseInput().y * _sensitivity * Time.deltaTime;
            Vector3 camEulerAngles = _cam.rotation.eulerAngles;
            _xRotation = Mathf.Clamp(_xRotation, -clampAngle, clampAngle);
            _cam.rotation = Quaternion.Euler(_xRotation, camEulerAngles.y, camEulerAngles.z);
        }

        #endregion
    }
    
}

