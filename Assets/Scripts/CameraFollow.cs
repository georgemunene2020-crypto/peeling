using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3   offset              = new Vector3(0f, 4f, -7f);
    public float     smoothSpeed         = 10f;
    public float     rotationSmoothSpeed = 10f;

    // using LateUpdate so we always read the player's position after it's moved this frame
    void LateUpdate()
    {
        if (player == null) return;

        bool isMazeMode = (GameManager.Instance != null &&
                           GameManager.Instance.CurrentMode == GameManager.GameMode.Maze);

        if (isMazeMode)
        {
            // follow behind wherever the player is facing, so the camera turns with them at corners
            Vector3 targetPosition = player.position + player.TransformDirection(offset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

            // aim slightly above the player so they don't get cut off when turning
            Quaternion targetRotation = Quaternion.LookRotation(
                player.position + Vector3.up * 2f - transform.position
            );
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
        else
        {
            // endless mode - keep X locked to centre, lane switching is handled by PlayerMovement
            Vector3 targetPosition = new Vector3(
                0f,
                player.position.y + offset.y,
                player.position.z + offset.z
            );

            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

            // fixed angle looking slightly down at the track
            transform.rotation = Quaternion.Euler(20f, 0f, 0f);
        }
    }
}