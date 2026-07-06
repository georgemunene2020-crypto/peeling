using UnityEngine;

// attach to any obstacle prefab — collider MUST have "Is Trigger" checked
public class Obstacle : MonoBehaviour
{
    public enum ObstacleType
    {
        InstantGameOver, // deducts a life, or triggers game over if lives = 0
        Stumble          // slows the player temporarily, no life lost
    }

    [Header("Obstacle Type")]
    public ObstacleType type = ObstacleType.InstantGameOver;

    [Header("Stumble Settings")]
    public float slowdownMultiplier = 0.5f;
    public float slowdownDuration   = 1.5f;

    [Header("Effects")]
    public GameObject crashEffectPrefab;
    public AudioClip  crashSound;

    // stops the trigger from firing twice (can happen with multi-collider prefabs)
    private bool _hasHit = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;
        if (!other.CompareTag("Player")) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null) return;

        // if the player has a power-up or post-hit grace period, just smash through
        if (player.IsInvincible())
        {
            Destroy(gameObject);
            return;
        }

        _hasHit = true;
        PlayEffects();
        HandlePlayerHit(player);
    }

    private void HandlePlayerHit(PlayerMovement player)
    {
        if (type == ObstacleType.InstantGameOver)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TakeDamage("Crashed into an obstacle!");
        }
        else if (type == ObstacleType.Stumble)
        {
            // stumble lives in PlayerMovement so it doesn't get cancelled when this object is destroyed
            player.StartStumble(slowdownMultiplier, slowdownDuration);
            Destroy(gameObject);
        }
    }

    private void PlayEffects()
    {
        if (crashEffectPrefab != null)
            Instantiate(crashEffectPrefab, transform.position, Quaternion.identity);

        if (crashSound != null)
            AudioSource.PlayClipAtPoint(crashSound, transform.position);
    }
}
