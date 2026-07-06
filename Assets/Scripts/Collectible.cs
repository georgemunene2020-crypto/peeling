using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Settings")]
    public int   scoreValue     = 1;
    public float rotationSpeed  = 100f;
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 2f;

    [Header("Effects")]
    public GameObject collectEffectPrefab;
    public AudioClip  collectSound;

    private Vector3      startPos;
    private PlayerMovement playerMovement;

    // Random offset so each collectible bobs out of sync with its neighbours.
    // Without this, all mangoes rise and fall in perfect unison — looks robotic.
    private float phaseOffset;

    void Start()
    {
        startPos    = transform.position;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        // Prefer getting the player from GameManager (no expensive scene search).
        // Falls back to tag search if GameManager isn't available.
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            playerMovement = GameManager.Instance.Player;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        // Spin
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Magnet pull — move toward player if magnet is active and within range
        if (playerMovement != null && playerMovement.IsMagnetActive())
        {
            float distance = Vector3.Distance(transform.position, playerMovement.transform.position);
            if (distance <= playerMovement.magnetRadius)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerMovement.transform.position,
                    playerMovement.magnetPullSpeed * Time.deltaTime
                );
                // Keep startPos in sync so float animation doesn't snap back when
                // the magnet expires.
                startPos = transform.position;
                return;
            }
        }

        // Bob up and down with a unique per-instance phase offset
        Vector3 newPos = startPos;
        newPos.y += Mathf.Sin(Time.time * floatFrequency + phaseOffset) * floatAmplitude;
        transform.position = newPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            Collect();
    }

    private void Collect()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddMango(scoreValue);

        if (collectEffectPrefab != null)
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        Destroy(gameObject);
    }
}
