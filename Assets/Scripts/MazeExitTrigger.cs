using UnityEngine;

/// <summary>
/// Trigger placed at the escape point of the final maze.
/// Completes the game loop with a successful escape message.
/// </summary>
public class MazeExitTrigger : MonoBehaviour
{
    [Header("Victory Message")]
    [Tooltip("Message displayed when the player successfully escapes the maze.")]
    public string victoryMessage = "You Escaped! The farmer was reported to the police.";

    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        _hasTriggered = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(victoryMessage);
        }
        else
        {
            Debug.Log("Escaped the maze successfully! GameManager not present.");
        }
    }
}