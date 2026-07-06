using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// put this trigger at the end of an endless section to kick off the maze transition
public class LevelTransitionTrigger : MonoBehaviour
{
    [Header("Transition Target")]
    public string targetSceneName = "Maze_1";

    [Header("Effects")]
    public GameObject transitionEffectPrefab;

    [Header("Timing")]
    [Tooltip("Delay before loading starts — gives the VFX time to play.")]
    public float transitionDelay = 0.5f;

    // makes sure the trigger only fires once even if the player re-enters the collider
    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        _hasTriggered = true;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        if (transitionEffectPrefab != null)
            Instantiate(transitionEffectPrefab, transform.position, Quaternion.identity);

        // wait for the VFX/screen flash before starting the load
        yield return new WaitForSeconds(transitionDelay);

        // async load so there's no hard freeze
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);

        if (op == null)
        {
            Debug.LogError($"[LevelTransitionTrigger] Scene '{targetSceneName}' not found. " +
                           "Make sure it's added in Build Settings.");
            _hasTriggered = false; // let them try again after fixing it
            yield break;
        }

        // hold at 90% so we can show a loading screen here later if needed
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
    }
}
