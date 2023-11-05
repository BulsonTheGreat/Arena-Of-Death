using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerController : MonoBehaviour
{
    CharacterController characterController;
    Animator animator;
    AssetStart playerInput;
    Rigidbody rb;

    public int maxHp = 100;
    public int hp;
    private Vector2 movementInput;
    private Vector2 cameraInput;
    private bool movementPressed;
    private Vector3 currentMovement;
    
    private const float _threshold = 0.01f;
    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private bool jumpPressed;
    private bool isAttacking = false;

    [SerializeField] private float movementSpeed = 5f;
    private float velocity = 0f;
    private float acceleration = 0.4f;
    private float deceleration = 1.4f;
    [SerializeField] private float rotationFactorPerFrame = 15f;

    private float gravity = -9.8f;
    private float groundedGravity = -0.85f;

    //combat variables
    [SerializeField] private List<AttackSO> combo;
    private float lastClickedTime; //makes sure you can't spam attacks too quickly
    private float lastComboEnd; //adds a delay between combos
    private int comboCounter; //tracks how far we are into the combo

    [SerializeField] Weapon weapon;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    public HealthBar healthBar;

    private Vector3 knockbackDirection;
    private float knockbackForce;
    private float knockbackDuration;
    private float knockbackTimer;

    private void Awake()
    {
        playerInput = new AssetStart();
        rb = GetComponent<Rigidbody>();
        //_cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        healthBar.SetMaxHealth(maxHp);
        hp = maxHp;
        //set input callbaacks for movemnt
        playerInput.Player.Move.started += HandleMovementInput;
        playerInput.Player.Move.canceled += HandleMovementInput;
        playerInput.Player.Move.performed += HandleMovementInput;
        //set input callbacks for camera control
        playerInput.Player.Look.started += HandleCameraInput;
        playerInput.Player.Look.performed += HandleCameraInput;
        playerInput.Player.Look.canceled += HandleCameraInput;
        //set input callbacks for jumping
        playerInput.Player.Jump.started += HandleJumpingInput;
        playerInput.Player.Jump.canceled += HandleJumpingInput;
        //set input callbacks for attacking
        playerInput.Player.BasicAttack.started += HandleAttackingInput;
        playerInput.Player.BasicAttack.canceled += HandleAttackingInput;

        playerInput.Player.Warp.started += HandleWarpingInput;
        playerInput.Player.Warp.canceled += HandleWarpingInput;

        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void HandleMovementInput(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
        currentMovement.x = movementInput.x;
        currentMovement.z = movementInput.y;
        movementPressed = movementInput.x != 0 || movementInput.y != 0;
    }

    public void HandleCameraInput(InputAction.CallbackContext context)
    {
        cameraInput = context.ReadValue<Vector2>();
    }

    private void HandleJumpingInput(InputAction.CallbackContext context)
    {
        jumpPressed = context.ReadValueAsButton();
        if (context.started)
        {
            animator.SetTrigger("jump");
        }
    }

    private void HandleAttackingInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            HandleAttacking();
        }
    }

    private void HandleWarpingInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            animator.SetTrigger("warp");
        }
    }
    
    private void HandleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = 0f;
        positionToLookAt.z = currentMovement.z;

        Quaternion currentRotation = transform.rotation;
        if (movementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    private void HandleAttacking()
    {
        
        if (Time.time - lastComboEnd > 0.5f && comboCounter <= combo.Count)
        {
            CancelInvoke("EndCombo");
            isAttacking = true;
            if (Time.time - lastClickedTime >= 0.2f)
            {
                Debug.Log(comboCounter);
                animator.runtimeAnimatorController = combo[comboCounter].animatorOV;
                animator.Play("DemoAttack", 0, 0);
                weapon.EnableCollidder(true);
                weapon.damage = combo[comboCounter].damage;
                weapon.knockback = combo[comboCounter].knockback;
                comboCounter++;
                lastClickedTime = Time.time;

                if (comboCounter >= combo.Count)
                {
                    comboCounter = 0;
                }
            }
        }
    }

    private void ExitAttack()
    {
        //check if it's 90 procent done and is an attack animation
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f && animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            //end the combo but still give you a chance to extend it
            Invoke("EndCombo", 1);
        }
    }

    private void EndCombo()
    {
        comboCounter = 0;
        lastComboEnd = Time.time;
        isAttacking = false;
        weapon.EnableCollidder(false);
    }

    private void HandleGravity()
    {
        bool isFalling = currentMovement.y <= 0.0f || !jumpPressed;
        float fallMultiplier = 2.0f;

        if (characterController.isGrounded)
        {
            currentMovement.y = groundedGravity;
        }
        else if (isFalling)
        {
            float previousYvelocity = currentMovement.y;
            float newYvelocity = currentMovement.y + (gravity * fallMultiplier * Time.deltaTime);
            float nextYvelocity = (previousYvelocity + newYvelocity) * 0.5f;
            currentMovement.y = nextYvelocity;
        }
        else
        {
            float previousYvelocity = currentMovement.y;
            float newYvelocity = currentMovement.y + (gravity * Time.deltaTime);
            float nextYvelocity = (previousYvelocity + newYvelocity) * 0.5f;
            currentMovement.y = nextYvelocity;
        }
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

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void HandleMovement()
    {
        var moveVec = (movementSpeed * Time.deltaTime * currentMovement);
        if (isAttacking)
        {
            moveVec.y = 0;
            moveVec.x = 0;
            moveVec.z = 0;
            velocity = 0;
        }
        characterController.Move(moveVec);
        if(movementPressed & velocity < 1)
        {
            velocity += acceleration * Time.deltaTime;
        }
        if (!movementPressed && velocity > 0)
        {
            velocity -= deceleration * Time.deltaTime;
        }
        if (velocity < 0)
        {
            velocity = 0;
        }
        animator.SetFloat("Velocity", velocity);
    }
    
    public IEnumerator Warp()
    {
        var target = GameObject.FindGameObjectWithTag("Enemy");
        if(target != null)
        {
            Debug.Log(target);
            Debug.Log("Your position: " + transform.position);
            Debug.Log("Target position: " + target.transform.position);
            weapon.damage = 5;
            weapon.knockback = 10;
            weapon.EnableCollidder(true);
            transform.LookAt(target.transform.position);
            ShowBody(false);
            animator.enabled = false;
            transform.DOMove(target.transform.position, 1f).OnComplete(FinishWarp);
            yield return new WaitForEndOfFrame();
        }
        //else just in case
    }

    void ShowBody(bool state)
    {
        weapon.GetComponent<Renderer>().enabled = state;
        SkinnedMeshRenderer[] skinMeshList = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach(SkinnedMeshRenderer smr in skinMeshList)
        {
            smr.enabled = state;
        }
    }

    void FinishWarp()
    {
        ShowBody(true);
        animator.enabled = true;
        weapon.EnableCollidder(false);
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        healthBar.SetHealth(hp);
        animator.SetTrigger("takeDamage");
        if (hp <= 0)
        {
            //play death animation
            animator.SetTrigger("playerDeath");
            //display ending scene;
        }
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        knockbackDirection = direction.normalized;
        knockbackForce = force;
        knockbackDuration = duration;
        knockbackTimer = knockbackDuration;
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

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleGravity();
        ExitAttack();
        HandleKnockback();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void OnEnable()
    {
        playerInput.Player.Enable();
    }

    private void OnDisable()
    {
        playerInput.Player.Disable();
    }

    private bool IsCurrentDeviceMouse
    {
        get
        {
            return false;
        }
    }
}
