using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI highScoreText;

    void Start()
    {
        int best = PlayerPrefs.GetInt("HighScore", 0);
        if (highScoreText != null)
            highScoreText.text = "Best Score: " + best;
    }

    public void LoadEndlessMode()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("EndlessMode");
    }

    public void LoadMaze1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Maze_1");
    }

    public void LoadMaze2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Maze_2");
    }

    public void QuitGame()
    {
        Debug.Log("Quit game requested.");
        Application.Quit();
    }
}
