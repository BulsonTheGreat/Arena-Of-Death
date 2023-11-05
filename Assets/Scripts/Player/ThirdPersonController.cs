 using UnityEngine;
using Cinemachine;

using UnityEngine.InputSystem;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]

    [RequireComponent(typeof(PlayerInput))]

    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        //combat variables
        [SerializeField] Weapon weapon;
        public HealthBar healthBar;
        public HealthBar manaBar;
        private float attackCD = 1f;
        private Vector3 knockbackDirection;
        private float knockbackForce;
        private float knockbackDuration;
        private float knockbackTimer;
        private bool isAttacking = false;
        public bool isInvulnerable = false;
        //Warp variables
        [HideInInspector] public List<Transform> screenTargets = new List<Transform>();
        [HideInInspector] public Transform target;
        [SerializeField] private Material glowMaterial;

        //other movement variables
        private Vector2 movementInput;
        private Vector3 currentMovement;
        private Vector2 cameraInput;
        private bool isSprinting = false;

        [SerializeField] public int maxHp = 120;
        [SerializeField] public float maxMana = 100;
        [HideInInspector] public int hp;
        [HideInInspector] public float mana;

        private AssetStart inputAction;

        private Animator animator;
        private CharacterController characterController;
        //private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private Camera cam;

        //for shooting a projectile
        private Vector3 destination;
        public GameObject projectile;
        public Transform firepoint;
        public float fireRate = 4;
        private AirSlash airSlashScript;
        private float timeTofire;

        private bool _shouldJump; // Flag to indicate if the character should jump
        private const float _threshold = 0.01f;

        private bool isClone = false;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            inputAction = new AssetStart();
            if (isClone)
            {
                return;
            }
            else
            {
                healthBar.SetMaxHealth(maxHp);
                manaBar.SetMaxMana(maxMana);
                hp = maxHp;
                mana = maxMana;
            }
            cam = Camera.main;
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            inputAction.Player.Move.started += HandleMovementInput;
            inputAction.Player.Move.performed += HandleMovementInput;
            inputAction.Player.Move.canceled += HandleMovementInput;

            inputAction.Player.Look.started += HandleCameraInput;
            inputAction.Player.Look.performed += HandleCameraInput;
            inputAction.Player.Look.canceled += HandleCameraInput;

            inputAction.Player.Sprint.started += HandleSprintingInput;
            inputAction.Player.Sprint.performed += HandleSprintingInput;
            inputAction.Player.Sprint.canceled += HandleSprintingInput;

            inputAction.Player.Jump.started += HandleJumpingInput;
            inputAction.Player.Jump.performed += HandleJumpingInput;
            inputAction.Player.Jump.canceled += HandleJumpingInput;

            inputAction.Player.BasicAttack.started += HandleAttackingInput;
            inputAction.Player.BasicAttack.canceled += HandleAttackingInput;
            
            inputAction.Player.Warp.started += HandleWarpingInput;
            inputAction.Player.Warp.canceled += HandleWarpingInput;

            inputAction.Player.Slash.started += HandleSlashingInput;
            inputAction.Player.Slash.canceled += HandleSlashingInput;
        }

        private void HandleMovementInput(InputAction.CallbackContext context)
        {
            movementInput = context.ReadValue<Vector2>();
            currentMovement.x = movementInput.x;
            currentMovement.z = movementInput.y;
        }

        private void HandleCameraInput(InputAction.CallbackContext context)
        {
            cameraInput = context.ReadValue<Vector2>();
        }

        private void HandleSprintingInput(InputAction.CallbackContext context)
        {
            isSprinting = context.ReadValueAsButton();
        }

        private void HandleJumpingInput(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _shouldJump = true; // Set the jump flag when the jump input is triggered
            }
        }

        private void HandleAttackingInput(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if(Time.time >= attackCD)
                {
                    StartCoroutine(AttackInput());
                }
            }
        }

        private void HandleWarpingInput(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                target = screenTargets[TargetIndex()];
                if (target != null && mana >= 35)
                {
                    animator.SetTrigger("warp");

                }
                else
                {
                    return;
                }
            }
        }

        private void HandleSlashingInput(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if(mana >= 25)
                {
                    StartCoroutine(SlashingInput());
                }
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            characterController = GetComponent<CharacterController>();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Move();
            HandleKnockback();
            ManageMana();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private IEnumerator AttackInput()
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Attack Layer"), 1);
            weapon.EnableCollidder(true);
            isAttacking = true;
            int r = Random.Range(4, 7);
            weapon.damage = 2* r;
            weapon.knockback = (2*r);
            animator.SetTrigger("attack");

            yield return new WaitForSeconds(0.9f);
            weapon.EnableCollidder(false);
            isAttacking = false;
            animator.SetLayerWeight(animator.GetLayerIndex("Attack Layer"), 0);
        }

        private IEnumerator SlashingInput()
        {
            if (Time.time >= timeTofire)
            {
                animator.SetLayerWeight(animator.GetLayerIndex("Attack Layer"), 1);
                timeTofire = Time.time + 1 / fireRate;
                mana -= 25;
                manaBar.SetMana(mana);
                animator.SetTrigger("slash");
                isAttacking = true;
                ShootProjectile();

                yield return new WaitForSeconds(0.9f);
                isAttacking = false;
                animator.SetLayerWeight(animator.GetLayerIndex("Attack Layer"), 0);
            }
        }

        public IEnumerator Warp()
        {
            isAttacking = true;
            isInvulnerable = true;
            transform.LookAt(target.transform.position);
            mana -= 35;
            manaBar.SetMana(mana);
            weapon.damage = 12;
            weapon.knockback = 7;
            weapon.EnableCollidder(true);
            CloneVFX();
            ShowBody(false);
            animator.enabled = false;
            transform.DOMove(target.transform.position, 1f).OnComplete(FinishWarp);
            yield return new WaitForEndOfFrame();
        }

        private void CloneVFX()
        {
            healthBar.DisableBar(true);
            manaBar.DisableBar(true);
            GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
            clone.GetComponent<ThirdPersonController>().isClone = true;
            Destroy(clone.GetComponent<ThirdPersonController>().weapon.gameObject);
            Destroy(clone.GetComponent<Animator>());
            Destroy(clone.GetComponent<ThirdPersonController>());
            Destroy(clone.GetComponent<CharacterController>());

            SkinnedMeshRenderer[] skinMeshList = clone.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer smr in skinMeshList)
            {
                smr.material = glowMaterial;
                smr.material.DOFloat(2, "_AlphaThreshold", 5f).OnComplete(() => Destroy(clone));
            }
        }


        void GlowAmount(float x)
        {
            SkinnedMeshRenderer[] skinMeshList = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer smr in skinMeshList)
            {
                smr.material.SetVector("_FresnelAmount", new Vector4(x, x, x, x));
            }
        }

        void ShowBody(bool state)
        {
            weapon.GetComponent<Renderer>().enabled = state;
            SkinnedMeshRenderer[] skinMeshList = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer smr in skinMeshList)
            {
                smr.enabled = state;
            }
        }

        void FinishWarp()
        {
            ShowBody(true);
            healthBar.DisableBar(true);
            manaBar.DisableBar(true);
            healthBar.SetMaxHealth(maxHp);
            healthBar.SetHealth(hp);
            manaBar.SetMaxMana(maxMana);
            manaBar.SetMana(mana);
            animator.enabled = true;
            isInvulnerable = false;
            weapon.EnableCollidder(false);

            SkinnedMeshRenderer[] skinMeshList = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer smr in skinMeshList)
            {
                GlowAmount(30);
                DOVirtual.Float(30, 0, .5f, GlowAmount);
            }
            isAttacking = false;
        }


        public int TargetIndex()
        {
            float[] distances = new float[screenTargets.Count];

            for (int i = 0; i < screenTargets.Count; i++)
            {
                distances[i] = Vector2.Distance(Camera.main.WorldToScreenPoint(screenTargets[i].position), new Vector2(Screen.width / 2, Screen.height / 2));
            }

            float minDistance = Mathf.Min(distances);
            int index = 0;

            for (int i = 0; i < distances.Length; i++)
            {
                if (minDistance == distances[i])
                    index = i;
            }

            return index;

        }

        private void ShootProjectile()
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
                destination = hit.point;
            }
            else
            {
                destination = ray.GetPoint(1000);
            }
            Invoke(nameof(InstantiateProjectile), 0.5f);
        }

        void InstantiateProjectile()
        {
            var projectileObj = Instantiate(projectile, firepoint.position, Quaternion.Euler(0f, 270f, 0f)) as GameObject;

            airSlashScript = projectileObj.GetComponentInChildren<AirSlash>();
            RotateToDestination(projectileObj, destination, true);
            projectileObj.GetComponent<Rigidbody>().velocity = transform.forward * airSlashScript.speed;
        }

        void RotateToDestination(GameObject obj, Vector3 destination, bool onlyY)
        {
            var direction = destination - obj.transform.position;
            var rotation = Quaternion.LookRotation(direction);

            if (onlyY)
            {
                // Keep only the Y-axis rotation
                rotation = Quaternion.Euler(0f, rotation.eulerAngles.y + 270f, 0f);
            }

            // Use Quaternion.LookRotation to directly set the rotation
            obj.transform.rotation = rotation;
        }


        public void TakeDamage(int damage)
        {
            hp -= damage;
            healthBar.SetHealth(hp);
            animator.SetTrigger("takeDamage");
            if (hp <= 0)
            {
                //play death animation
                characterController.enabled = false;
                animator.SetTrigger("playerDeath");
                //inputAction.Player.Disable();
                //display ending scene;
                StartCoroutine(RestartGame());
            }
        }

        private IEnumerator RestartGame()
        {
            yield return new WaitForSeconds(3f);
            SceneManager.LoadScene(2);
        }

        public void RestoreHealth(int health)
        {
            hp += health;
            hp = Mathf.Clamp(hp, 0, maxHp);
            healthBar.SetHealth(hp);
        }

        public void FullHealth()
        {
            hp = maxHp;
            healthBar.SetHealth(hp);
            mana = maxMana;
            manaBar.SetMana(mana);
        }

        private void ManageMana()
        {
            if (mana >= maxMana)
            {
                return;
            }
            float manaToRecover = 5 * Time.deltaTime;
            mana += manaToRecover;
            manaBar.SetMana(mana);
            mana = Mathf.Clamp(mana, 0f, maxMana);
        }

        private void HandleKnockback()
        {
            if (knockbackTimer > 0)
            {
                // Apply knockback force in the desired direction
                characterController.Move(knockbackDirection * knockbackForce * Time.deltaTime);

                // Reduce the knockback timer
                knockbackTimer -= Time.deltaTime;
            }
        }

        public void ApplyKnockback(Vector3 direction, float force, float duration)
        {
            knockbackDirection = direction.normalized;
            knockbackForce = force;
            knockbackDuration = duration;
            knockbackTimer = knockbackDuration;
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            animator.SetBool("Grounded", Grounded);
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (cameraInput.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += cameraInput.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += cameraInput.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (!isAttacking)
            {
                // set target speed based on move speed, sprint speed and if sprint is pressed
                float targetSpeed = isSprinting ? SprintSpeed : MoveSpeed;

                if (movementInput == Vector2.zero) targetSpeed = 0.0f;

                // a reference to the players current horizontal velocity
                float currentHorizontalSpeed = new Vector3(characterController.velocity.x, 0.0f, characterController.velocity.z).magnitude;

                float speedOffset = 0.1f;
                float inputMagnitude = /*_input.analogMovement ?*/ movementInput.magnitude;

                // accelerate or decelerate to target speed
                if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                    currentHorizontalSpeed > targetSpeed + speedOffset)
                {
                    // creates curved result rather than a linear one giving a more organic speed change
                    // note T in Lerp is clamped, so we don't need to clamp our speed
                    _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                        Time.deltaTime * SpeedChangeRate);

                    // round speed to 3 decimal places
                    _speed = Mathf.Round(_speed * 1000f) / 1000f;
                }
                else
                {
                    _speed = targetSpeed;
                }

                _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
                if (_animationBlend < 0.01f) _animationBlend = 0f;

                // normalise input direction
                Vector3 inputDirection = new Vector3(movementInput.x, 0.0f, movementInput.y).normalized;

                // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
                // if there is a move input rotate player when the player is moving
                if (movementInput != Vector2.zero)
                {
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                        _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                        RotationSmoothTime);

                    // rotate to face input direction relative to camera position
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }


                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

                // move the player
                characterController.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                                    new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

                animator.SetFloat("Speed", _animationBlend);
                animator.SetFloat("MotionSpeed", inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // Reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                animator.SetBool("Jump", false);
                animator.SetBool("FreeFall", false);

                // Stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_shouldJump && _jumpTimeoutDelta <= 0.0f)
                {
                    // The square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -1.5f * Gravity);

                    animator.SetBool("Jump", true);
                }

                // Jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // Reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // Fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    animator.SetBool("FreeFall", true);
                }

                // If we are not grounded, do not jump
                _shouldJump = false;
            }

            // Apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(characterController.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(characterController.center), FootstepAudioVolume);
            }
        }

        private void OnEnable()
        {
            inputAction.Player.Enable();
        }

        private void OnDisable()
        {
            inputAction.Player.Disable();
        }
    }
}