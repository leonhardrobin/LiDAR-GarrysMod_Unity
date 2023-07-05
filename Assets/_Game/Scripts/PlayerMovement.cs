/*
 * Author: Leonhard Robin Schnaitl
 * GitHub: https://github.com/leonhardrobin
*/ 
using UnityEngine;
using System;
using System.Linq;


namespace LRS
{
    #if ENABLE_INPUT_SYSTEM 
    using UnityEngine.InputSystem;
    [RequireComponent(typeof(PlayerInput))]
    #endif
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMovement : MonoBehaviour
    {
        #region PUBLIC MEMBERS
        
        [Serializable]
        public class MovementSettings
        {
            #if !ENABLE_INPUT_SYSTEM
            public KeyCode sprintKey = KeyCode.LeftShift;
            public KeyCode jumpKey = KeyCode.Space;
            public KeyCode sneakKey = KeyCode.LeftAlt;
            #endif
            public float walkSpeed = 25;
            [Range(0,2f)]
            public float sprintSpeedMultiplier = 1.5f;
            [Range(0, 0.99f)]
            public float sneakSpeedMultiplier = 0.5f;
            public float jumpHeight = 0.5f;
            public float colliderSneakHeightPercentage = .5f;
        }

        [Serializable]
        public class AdvancedSettings
        {
            [Header("Stuck Prevention")]
            public bool enableCapsuleCast;
            public bool enablePhysicsMaterial = true;
            public float capsuleCastDistance = 0.5f;
            public float capsuleCastRadiusMultiplier = 0.95f;

            [Header("Ground Checking")]
            public float sphereCheckRadiusMultiplier = 0.95f;
            public float groundCheckDistance = 0.03f;

            [Header("Other")] 
            public float crouchSmoothing = 0.1f;
        }

        [Serializable]
        public class AudioSettings
        {
            public AudioSource audioSource;
            public bool enableAudio = true;
            public float baseStepSpeed = 0.5f;
            public float crouchSpeedMultiplier = 1.5f;
            public float sprintSpeedMultiplier = 0.6f;
            public float footstepMinSpeed = 0.01f;
            public AudioClip[] footstepSounds;
        }
        
        public bool pauseMovement;
        public bool isSprinting { get; private set; }
        public bool isCrouching { get; private set; }
        public bool didJump { get; private set; }

        #endregion
        
        #region PRIVTE MEMBERS
        
        private class References
        {
            public Rigidbody rigidbody;
            public CapsuleCollider collider;
        }

        private readonly References _references = new();
        [SerializeField] private MovementSettings _movementSettings = new();
        [SerializeField] private AdvancedSettings _advancedSettings = new();
        [SerializeField] private AudioSettings _audioSettings = new();
        
        // Movement Settings
        #if ENABLE_INPUT_SYSTEM
        private InputAction _move;
        private InputAction _sprint;
        private InputAction _jump;
        private InputAction _sneak;
        #else
        private const string HORIZONTAL_AXIS = "Horizontal";
        private const string VERTICAL_AXIS = "Vertical";
        #endif
        
        private Vector3 _moveDirection;
        private const float BASE_SPEED = 10f;
        private float _normalColliderHeight;
        private bool _isCrouch;
        private const string PLAYER_TAG = "Player";
        
        // Audio Settings
        private float _footstepTimer;
        private float GetCurrentStepSpeed => 
            isCrouching ? _audioSettings.baseStepSpeed * _audioSettings.crouchSpeedMultiplier : 
            isSprinting ? _audioSettings.baseStepSpeed * _audioSettings.sprintSpeedMultiplier : 
            _audioSettings.baseStepSpeed;

        #endregion
        
        #region UNITY MESSAGES
        
        // Start is called before the first frame update
        private void Start()
        {
            // get all references
            _references.rigidbody = GetComponent<Rigidbody>();
            _references.collider = GetComponent<CapsuleCollider>();
            _normalColliderHeight = _references.collider.height;
            _audioSettings.audioSource ??= GetComponent<AudioSource>();
            #if ENABLE_INPUT_SYSTEM
            PlayerInput playerInput = GetComponent<PlayerInput>();
            _move = playerInput.actions["Move"];
            _sprint = playerInput.actions["Sprint"];
            _jump = playerInput.actions["Jump"];
            _sneak = playerInput.actions["Crouch"];
            #endif

            // make physics material
            CreateNonFrictionPhysicsMaterial();
            
            // handle the cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // make sure rigidbody rotations are frozen
            _references.rigidbody.freezeRotation = true;
        }

        // Update is called once per frame
        private void Update()
        {
            Jumping();
            Sneaking();
            FootstepSound();
        }

        private void FixedUpdate()
        {
            Movement();
        }
        
        #endregion

        #region PRIVATE METHODS

        private void CreateNonFrictionPhysicsMaterial()
        {
            if (!_advancedSettings.enablePhysicsMaterial) return;
            PhysicMaterial physicsMaterial = new()
            {
                dynamicFriction = 0,
                staticFriction = 0,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
            _references.collider.material = physicsMaterial;
        }
        
        private void GetMovementDirection()
        {
            #if ENABLE_INPUT_SYSTEM
            // get the movement input axis
            float horizontalMovement = _move.ReadValue<Vector2>().x;
            float verticalMovement = _move.ReadValue<Vector2>().y;
            #else
            float horizontalMovement = Input.GetAxis(HORIZONTAL_AXIS);
            float verticalMovement = Input.GetAxis(VERTICAL_AXIS);
            #endif
            // combine into single direction
            Transform t = transform;
            _moveDirection = (horizontalMovement * t.right + verticalMovement * t.forward).normalized;
        }

        private void Movement()
        {
            // pause if wanted
            if (pauseMovement) return;

            // get the movement direction and rotate the player
            GetMovementDirection();

            // only move if not in front of a wall to prevent getting stuck
            if (!CanMove(_moveDirection) && _advancedSettings.enableCapsuleCast) return;

            // only multiply if the sprint key is being pressed
            #if ENABLE_INPUT_SYSTEM
            float speedMultiplier;
            if (_sprint.IsPressed())
                speedMultiplier = _movementSettings.sprintSpeedMultiplier;
            else if (_sneak.IsPressed())
                speedMultiplier = _movementSettings.sneakSpeedMultiplier;
            else
                speedMultiplier = 1;
            #else
            float speedMultiplier;
            if (Input.GetKey(_movementSettings.sprintKey))
                speedMultiplier = _movementSettings.sprintSpeedMultiplier;
            else if (Input.GetKey(_movementSettings.sneakKey))
                speedMultiplier = _movementSettings.sneakSpeedMultiplier;
            else
                speedMultiplier = 1;
            
            #endif
            
            // set isSprinting and isSneaking bool to give other scripts that information
            isSprinting = speedMultiplier > 1;
            isCrouching = speedMultiplier < 1;

            // combine the direction with the speed modifiers
            Vector3 scaledMovementDirection = new Vector3(
                _moveDirection.x * _movementSettings.walkSpeed * speedMultiplier * Time.deltaTime * BASE_SPEED,
                _references.rigidbody.velocity.y,
                _moveDirection.z * _movementSettings.walkSpeed * speedMultiplier * Time.deltaTime * BASE_SPEED
            );

            // apply the scaled velocity direction and increase it to make it more modifiable
            _references.rigidbody.velocity = scaledMovementDirection;
        }

        private void Jumping()
        {
            // if wanted pause the movement
            if (pauseMovement) return;
            
            // make the jump available to other scripts
            // is the jump button pressed and the player is grounded
            didJump = GetJump() && IsGrounded();

            if (didJump)
            {
                // add a positive y velocity to the rigidbody to make it jump
                _references.rigidbody.velocity += new Vector3(0, Mathf.Sqrt(_movementSettings.jumpHeight * -2f * Physics.gravity.y), 0);
            }
        }

        private void Sneaking()
        {
            // if wanted pause the movement
            if (pauseMovement) return;
            
            #if ENABLE_INPUT_SYSTEM
            if (_sneak.IsPressed())
            #else
            if (Input.GetKeyDown(_movementSettings.sneakKey))
            #endif
            {
                // reduce the size of the player collider
                _references.collider.height =
                    Mathf.Lerp(_normalColliderHeight * _movementSettings.colliderSneakHeightPercentage,
                        _normalColliderHeight, Time.deltaTime * _advancedSettings.crouchSmoothing);

                if (!_isCrouch)
                {
                    // move the player down to compat falling (crouching into the air)
                    Vector3 position = transform.position;
                    position = new Vector3(position.x,
                        position.y - _normalColliderHeight * _movementSettings.colliderSneakHeightPercentage / 2,
                        position.z);
                    transform.position = position;

                    _isCrouch = true;
                }
                
            }
            else // do the opposite
            {
                _references.collider.height = Mathf.Lerp(_normalColliderHeight,
                    _normalColliderHeight * _movementSettings.colliderSneakHeightPercentage,
                    Time.deltaTime * _advancedSettings.crouchSmoothing);

                if (_isCrouch)
                {
                    Vector3 position = transform.position;
                    position = new Vector3(position.x,
                        position.y + _normalColliderHeight * _movementSettings.colliderSneakHeightPercentage / 2,
                        position.z);
                    transform.position = position;

                    _isCrouch = false;
                }
                
            }
        }

        private void FootstepSound()
        {
            if (!_audioSettings.enableAudio) return;
            if (_audioSettings.footstepSounds.Length == 0) return;
            
            // only play the sound if the player is moving on the ground
            if (!IsGrounded()) return;
            if (_references.rigidbody.velocity.magnitude < _audioSettings.footstepMinSpeed) return;

            // start the timer
            _footstepTimer -= Time.deltaTime;
            
            // if the timer is 0 or less, play the sound and reset the timer
            if (_footstepTimer <= 0)
            {
                _audioSettings.audioSource.PlayOneShot(
                    _audioSettings.footstepSounds[UnityEngine.Random.Range(0, _audioSettings.footstepSounds.Length)]
                    );
                _footstepTimer = GetCurrentStepSpeed;
            }
        }

        private bool GetJump()
        {
            #if ENABLE_INPUT_SYSTEM
            return _jump.WasPressedThisFrame();
            #else
            return Input.GetKeyDown(_movementSettings.jumpKey);
            #endif
        }
        
        #region CALCULATIONS

        private bool IsGrounded()
        {
            // calculate the bottom point of the capsule (where the radius is applied)
            float distanceToPoints = _references.collider.height / 2 - _references.collider.radius;
            Vector3 spherePos = transform.position + _references.collider.center - Vector3.up * distanceToPoints;

            // add the offset to the sphere position
            spherePos -= new Vector3(0, _advancedSettings.groundCheckDistance, 0);

            // reduce the radius to not detect ground next to the player
            float radius = _references.collider.radius * _advancedSettings.sphereCheckRadiusMultiplier;

            // do a sphere cast to match the capsules bottom part and offset its y (to make it intersect with the ground)
            RaycastHit[] hits =
                Physics.SphereCastAll(spherePos, radius, Vector3.down, _advancedSettings.groundCheckDistance);

            return hits.Any(raycastHit => raycastHit.collider.enabled && !raycastHit.collider.CompareTag(PLAYER_TAG));
        }

        private bool CanMove(Vector3 direction)
        {
            // calculate the points for a capsule cast
            float distanceToPoints = _references.collider.height / 2 - _references.collider.radius;

            Vector3 position = transform.position;
            Vector3 center = _references.collider.center;
            Vector3 point1 = position + center + Vector3.up * distanceToPoints;
            Vector3 point2 = position + center - Vector3.up * distanceToPoints;

            // reduce the radius a little to prevent colliding with object on the side and get the cast distance
            float radius = _references.collider.radius * _advancedSettings.capsuleCastRadiusMultiplier;
            float castDistance = _advancedSettings.capsuleCastDistance;

            // casting the capsule with the calculated points
            RaycastHit[] hits = Physics.CapsuleCastAll(point1, point2, radius, direction, castDistance);

            foreach (RaycastHit raycastHit in hits)
            {
                // return false if the hit object is not the player and has no rigidbody
                if (!raycastHit.collider.CompareTag(PLAYER_TAG) &&
                    raycastHit.collider.attachedRigidbody == null)
                {
                    return false;
                }
                // also return false if the hit object has a rigidbody but is kinematic
                // because the rigidbody will not move
                if (raycastHit.collider.attachedRigidbody != null && raycastHit.collider.attachedRigidbody.isKinematic)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
        #endregion
    }
}

