using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ChaserAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public float catchDistance = 1.5f;

    [Header("Endless Mode Settings")]
    public float followDistance  = 5f;
    public float followSpeed     = 10f;
    public Vector3 endlessOffset = new Vector3(0, 0, -5f);

    private NavMeshAgent agent;
    private Rigidbody    rb;         // optional — only present if the chaser uses physics
    private bool         isMazeMode;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb    = GetComponent<Rigidbody>(); // may be null, that's fine

        isMazeMode = (GameManager.Instance != null &&
                      GameManager.Instance.CurrentMode == GameManager.GameMode.Maze);

        if (!isMazeMode)
        {
            // in endless mode we move manually, so turn the NavMeshAgent off
            // note: if the Rigidbody is non-kinematic, use rb.MovePosition() below to avoid jitter
            agent.enabled = false;
        }

        if (player == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.Player != null)
                player = GameManager.Instance.Player.transform;
            else
            {
                GameObject pGo = GameObject.FindWithTag("Player");
                if (pGo != null) player = pGo.transform;
            }
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            if (agent.enabled) agent.isStopped = true;
            return;
        }

        if (player == null) return;

        if (isMazeMode)
            HandleMazeChase();
        else
            HandleEndlessFollow();

        CheckCatchPlayer();
    }

    private void HandleMazeChase()
    {
        if (agent.enabled)
            agent.SetDestination(player.position);
    }

    private void HandleEndlessFollow()
    {
        Vector3 targetPosition = player.position + endlessOffset;
        targetPosition.x = player.position.x; // match the player's lane

        if (rb != null && !rb.isKinematic)
            rb.MovePosition(Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed));
        else
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        transform.LookAt(player);
    }

    private void CheckCatchPlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > catchDistance) return;

        // don't trigger game over if the player has a power-up or post-hit invincibility
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null && pm.IsInvincible()) return;

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver(gameObject.name + " caught you!");
    }
}
