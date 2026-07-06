using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    // ─── Movement ────────────────────────────────────────────────────────────
    [Header("Movement")]
    public float baseForwardSpeed = 10f;
    public float laneDistance     = 2.5f;
    public float laneChangeSpeed  = 20f;  // how fast the player slides between lanes
    public float jumpForce        = 7f;

    [Header("Mobile")]
    public float swipeThreshold = 50f;

    // ─── Ground Check ────────────────────────────────────────────────────────
    [Header("Ground Check")]
    [Tooltip("Place a child Transform at the bottom of the capsule (feet level).")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float     groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;

    // ─── References ──────────────────────────────────────────────────────────
    private Rigidbody        rb;
    private CapsuleCollider  capsuleCollider;

    // ─── Lane System (0 = Left, 1 = Middle, 2 = Right) ───────────────────────
    private int currentLane = 1;

    // ─── State ───────────────────────────────────────────────────────────────
    private bool isGrounded = true;
    private bool isSliding  = false;

    // ─── Touch Input ─────────────────────────────────────────────────────────
    private Vector2 touchStartPos;

    // ─── Collider Backup ─────────────────────────────────────────────────────
    private float originalHeight;
    private float originalCenterY;

    // ─── Power-Up / Stumble ───────────────────────────────────────────────────
    private float     speedMultiplier  = 1f;
    private bool      isInvincible     = false;
    private bool      isMagnetActive   = false;
    private Coroutine stumbleCoroutine;

    [Header("Magnet")]
    public float magnetRadius    = 10f;
    public float magnetPullSpeed = 15f;

    // ─── Maze ────────────────────────────────────────────────────────────────
    private Quaternion targetRotation;

    // read by AnimationController
    public bool  IsGrounded   => isGrounded;
    public bool  IsSliding    => isSliding;
    public float CurrentSpeed => baseForwardSpeed * speedMultiplier;

    public bool IsInvincible()           => isInvincible;
    public void SetInvincible(bool s)    { isInvincible   = s; }
    public bool IsMagnetActive()         => isMagnetActive;
    public void SetMagnetActive(bool s)  { isMagnetActive = s; }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    void Awake()
    {
        // let the GameManager know about us so other scripts can find the player
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayer(this);
    }

    void Start()
    {
        rb              = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            originalHeight  = capsuleCollider.height;
            originalCenterY = capsuleCollider.center.y;
        }
        else
        {
            Debug.LogError("[PlayerMovement] No CapsuleCollider found!");
        }

        targetRotation = transform.rotation;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)   return;

        HandleKeyboardInput();
        HandleTouchInput();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)   return;

        // sphere cast is more reliable than OnCollisionEnter, especially across chunk seams
        if (groundCheckPoint != null)
            isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundMask);

        float currentForwardSpeed = baseForwardSpeed * speedMultiplier;
        Vector3 forwardMove       = transform.forward * currentForwardSpeed * Time.fixedDeltaTime;

        if (GameManager.Instance != null && GameManager.Instance.CurrentMode == GameManager.GameMode.Maze)
        {
            // smoothly rotate toward wherever we're supposed to be facing after a turn
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 15f);
            rb.MovePosition(rb.position + forwardMove);
        }
        else
        {
            // MoveTowards gives a consistent speed regardless of frame rate
            float targetX = (currentLane - 1) * laneDistance;
            float newX    = Mathf.MoveTowards(rb.position.x, targetX, laneChangeSpeed * Time.fixedDeltaTime);

            rb.MovePosition(new Vector3(newX, rb.position.y, rb.position.z) + forwardMove);
        }
    }

    // =========================================================================
    // Input Handling
    // =========================================================================

    void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) HandleRightInput();
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)  HandleLeftInput();

        // don't let the player jump while sliding, collider is half height
        if (Keyboard.current.upArrowKey.wasPressedThisFrame && isGrounded && !isSliding)
            Jump();

        if (Keyboard.current.downArrowKey.wasPressedThisFrame && isGrounded)
            StartSlide();

        if (Keyboard.current.downArrowKey.wasReleasedThisFrame)
            StopSlide();
    }

    void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        if (!touch.press.isPressed) return;

        if (touch.press.wasPressedThisFrame)
            touchStartPos = touch.position.ReadValue();

        if (touch.press.wasReleasedThisFrame)
        {
            Vector2 swipeDelta = touch.position.ReadValue() - touchStartPos;

            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                // horizontal swipe
                if      (swipeDelta.x >  swipeThreshold) HandleRightInput();
                else if (swipeDelta.x < -swipeThreshold) HandleLeftInput();
            }
            else
            {
                // vertical swipe
                if (swipeDelta.y > swipeThreshold && isGrounded && !isSliding)
                {
                    Jump();
                }
                else if (swipeDelta.y < -swipeThreshold && isGrounded)
                {
                    // cancel any pending StopSlide so swiping down again mid-slide doesn't break things
                    CancelInvoke(nameof(StopSlide));
                    StartSlide();
                    Invoke(nameof(StopSlide), 0.7f);
                }
            }
        }
    }

    private void HandleRightInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentMode == GameManager.GameMode.Maze)
            targetRotation *= Quaternion.Euler(0, 90, 0);
        else
            currentLane = Mathf.Clamp(currentLane + 1, 0, 2);
    }

    private void HandleLeftInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentMode == GameManager.GameMode.Maze)
            targetRotation *= Quaternion.Euler(0, -90, 0);
        else
            currentLane = Mathf.Clamp(currentLane - 1, 0, 2);
    }

    // =========================================================================
    // Movement Actions
    // =========================================================================

    void Jump()
    {
        if (isSliding) StopSlide(); // restore full height before going airborne
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    void StartSlide()
    {
        if (isSliding || capsuleCollider == null) return;
        isSliding = true;

        capsuleCollider.height = originalHeight / 2f;
        capsuleCollider.center = new Vector3(
            capsuleCollider.center.x,
            originalCenterY / 2f,
            capsuleCollider.center.z
        );
    }

    void StopSlide()
    {
        if (capsuleCollider == null) return;
        capsuleCollider.height = originalHeight;
        capsuleCollider.center = new Vector3(
            capsuleCollider.center.x,
            originalCenterY,
            capsuleCollider.center.z
        );
        isSliding = false;
    }

    // =========================================================================
    // Power-Up / Speed Interfaces
    // =========================================================================

    // 1f = back to normal speed
    public void ApplySpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    // called by GameManager as the run goes on - doesn't mess with the power-up multiplier
    public void SetBaseSpeed(float newSpeed)
    {
        baseForwardSpeed = newSpeed;
    }

    // stumble lives here (not on the obstacle) so it doesn't get cancelled when the obstacle is destroyed
    public void StartStumble(float slowdownMultiplier, float duration)
    {
        if (stumbleCoroutine != null) StopCoroutine(stumbleCoroutine);
        stumbleCoroutine = StartCoroutine(StumbleRoutine(slowdownMultiplier, duration));
    }

    private IEnumerator StumbleRoutine(float slowdownMultiplier, float duration)
    {
        speedMultiplier = slowdownMultiplier;
        yield return new WaitForSeconds(duration);
        speedMultiplier  = 1f;
        stumbleCoroutine = null;
    }

    // =========================================================================
    // Editor Gizmos
    // =========================================================================

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}