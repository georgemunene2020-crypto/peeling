using System.Collections;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { SpeedBoost, Invincibility, Magnet }

    [Header("Settings")]
    public PowerUpType type         = PowerUpType.SpeedBoost;
    public float duration           = 5f;
    public float rotationSpeed      = 100f;
    public float floatAmplitude     = 0.5f;
    public float floatFrequency     = 2f;

    [Header("Effects")]
    public GameObject collectEffectPrefab;
    public AudioClip  collectSound;

    private Vector3  startPos;
    private bool     isCollected = false;

    // using GetComponentInChildren so this works on 3D models where the mesh is on a child object
    private Renderer visualRenderer;

    void Start()
    {
        startPos        = transform.position;
        visualRenderer  = GetComponentInChildren<Renderer>();

        if (visualRenderer == null)
            Debug.LogWarning($"[PowerUp] No Renderer found on {gameObject.name} or its children.");
    }

    void Update()
    {
        if (isCollected) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // bob up and down using a sine wave
        Vector3 newPos = startPos;
        newPos.y += Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = newPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            StartCoroutine(ApplyPowerUp(playerMovement));
    }

    private IEnumerator ApplyPowerUp(PlayerMovement player)
    {
        isCollected = true;

        // hide the pickup visually and turn off the collider so it can't be grabbed again
        if (visualRenderer != null) visualRenderer.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (collectEffectPrefab != null)
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // turn on the effect
        switch (type)
        {
            case PowerUpType.SpeedBoost:    player.ApplySpeedMultiplier(2f);   break;
            case PowerUpType.Invincibility: player.SetInvincible(true);        break;
            case PowerUpType.Magnet:        player.SetMagnetActive(true);      break;
        }

        yield return new WaitForSeconds(duration);

        // turn it back off
        switch (type)
        {
            case PowerUpType.SpeedBoost:    player.ApplySpeedMultiplier(1f);   break;
            case PowerUpType.Invincibility: player.SetInvincible(false);       break;
            case PowerUpType.Magnet:        player.SetMagnetActive(false);     break;
        }

        Destroy(gameObject);
    }
}
