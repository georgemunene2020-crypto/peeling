using UnityEngine;

// bridges PlayerMovement state to the Animator
// attach this alongside PlayerMovement on the player GameObject
//
// animator parameters needed (names must match exactly):
//   Float   "Speed"       - drives run blend tree
//   Bool    "IsGrounded"  - true when on the ground
//   Bool    "IsSliding"   - true during slide
//   Trigger "Jump"        - fires when the player leaves the ground
//   Trigger "Stumble"     - fires on obstacle hit
//   Trigger "Die"         - fires on game over
[RequireComponent(typeof(PlayerMovement))]
public class AnimationController : MonoBehaviour
{
    // hashes are faster than string lookups — computed once in Awake
    private static readonly int SpeedHash      = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsSlidingHash  = Animator.StringToHash("IsSliding");
    private static readonly int JumpHash       = Animator.StringToHash("Jump");
    private static readonly int StumbleHash    = Animator.StringToHash("Stumble");
    private static readonly int DieHash        = Animator.StringToHash("Die");

    private PlayerMovement playerMovement;
    private Animator       animator;

    private bool wasGrounded   = true;
    private bool gameOverFired = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        // Animator won't be there until a character model is imported — that's fine, we just do nothing until then
        animator = GetComponent<Animator>();

        if (animator == null)
            Debug.LogWarning("[AnimationController] No Animator found. Add one once your character model is imported.");
    }

    void Update()
    {
        if (animator == null || playerMovement == null) return;

        animator.SetFloat(SpeedHash,      playerMovement.CurrentSpeed);
        animator.SetBool (IsGroundedHash, playerMovement.IsGrounded);
        animator.SetBool (IsSlidingHash,  playerMovement.IsSliding);

        // detect the moment the player leaves the ground and fire the jump trigger
        if (wasGrounded && !playerMovement.IsGrounded)
            animator.SetTrigger(JumpHash);

        // fire the die trigger once when game over happens
        if (!gameOverFired && GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            animator.SetTrigger(DieHash);
            gameOverFired = true;
        }

        wasGrounded = playerMovement.IsGrounded;
    }

    // call this from wherever a stumble event happens (e.g. Obstacle.HandlePlayerHit)
    public void TriggerStumble()
    {
        if (animator != null)
            animator.SetTrigger(StumbleHash);
    }
}
