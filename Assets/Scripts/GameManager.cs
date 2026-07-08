using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// central game state — persists across scene loads via DontDestroyOnLoad
// handles scoring, lives, speed escalation, maze timer, pause, and high score
// place this in your first scene, it'll carry over to everything else
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameMode { Endless, Maze }

    [Header("Game Mode")]
    [SerializeField] private GameMode _currentMode = GameMode.Endless;
    public GameMode CurrentMode => _currentMode;

    private PlayerMovement _player;
    public PlayerMovement Player => _player;

    // called by PlayerMovement.Awake() each time a new scene loads a player
    public void RegisterPlayer(PlayerMovement player) { _player = player; }

    [Header("Scoring & UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI timerText;

    private int   mangoScore        = 0;
    private float distanceTravelled = 0f;
    private int   highScore         = 0;

    public int   MangoScore => mangoScore;
    public float Distance   => distanceTravelled;

    private const string HIGH_SCORE_KEY = "HighScore";

    [Header("Speed Escalation (Endless Mode)")]
    [Tooltip("Starting forward speed.")]
    public float baseSpeed         = 10f;
    [Tooltip("Maximum speed the game escalates to.")]
    public float maxSpeed          = 30f;
    [Tooltip("How many units/sec the speed grows each second.")]
    public float speedIncreaseRate = 0.05f;

    private float currentSpeed;

    [Header("Lives")]
    public int maxLives = 3;
    [Tooltip("Duration (seconds) the player is invincible after losing a life.")]
    public float invincibilityAfterHitDuration = 2f;

    private int  currentLives;
    private bool _isTemporarilyInvincible;

    [Header("Maze Settings")]
    public float mazeTimeLimit = 60f;
    private float currentMazeTime;

    [Header("Fatigue Effect (Maze Mode)")]
    public Image fatigueOverlay;
    public float maxFatigueAlpha = 0.8f;

    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    public TextMeshProUGUI gameOverTitleText;
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI gameOverHighScoreText;

    private bool _isGameOver = false;
    private bool _isPaused   = false;

    public bool IsGameOver => _isGameOver;
    public bool IsPaused   => _isPaused;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // duplicate — get rid of it
            return;
        }

        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        InitializeSceneUI(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeSceneUI(scene.name);
    }

    public void InitializeSceneUI(string sceneName)
    {
        if (sceneName == "MainMenu")
        {
            _currentMode = GameMode.Endless;
            return;
        }

        // make sure an EventSystem exists so UI buttons actually work
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var eventSystemGo = new GameObject("EventSystem_SelfHealed");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[GameManager] Spawned missing EventSystem to support UI interaction.");
        }

        if (sceneName == "EndlessMode")
            _currentMode = GameMode.Endless;
        else if (sceneName.StartsWith("Maze"))
            _currentMode = GameMode.Maze;

        // find player
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.GetComponent<PlayerMovement>();

        // wire up camera
        if (Camera.main != null)
        {
            CameraFollow follow = Camera.main.GetComponent<CameraFollow>();
            if (follow != null && _player != null)
                follow.player = _player.transform;
        }

        // find UI components from the scene canvas
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            Transform scoreTextTr = canvasGo.transform.Find("HUD_Panel/Score_Text");
            if (scoreTextTr != null) scoreText = scoreTextTr.GetComponent<TextMeshProUGUI>();

            Transform distTextTr = canvasGo.transform.Find("HUD_Panel/Distance_Text");
            if (distTextTr != null) distanceText = distTextTr.GetComponent<TextMeshProUGUI>();

            Transform hsTextTr = canvasGo.transform.Find("HUD_Panel/HighScore_Text");
            if (hsTextTr != null) highScoreText = hsTextTr.GetComponent<TextMeshProUGUI>();

            Transform livesTextTr = canvasGo.transform.Find("HUD_Panel/Lives_Panel/Lives_Text");
            if (livesTextTr == null) livesTextTr = canvasGo.transform.Find("HUD_Panel/Lives_Text");
            if (livesTextTr != null) livesText = livesTextTr.GetComponent<TextMeshProUGUI>();

            // timer only exists in maze scenes
            Transform timerTextTr = canvasGo.transform.Find("HUD_Panel/Timer_Text");
            if (timerTextTr == null) timerTextTr = canvasGo.transform.Find("HUD_Panel/Time_Text");
            if (timerTextTr != null) timerText = timerTextTr.GetComponent<TextMeshProUGUI>();

            Transform fatigueTr = canvasGo.transform.Find("Fatigue_Overlay");
            if (fatigueTr == null) fatigueTr = canvasGo.transform.Find("FatigueOverlay");
            if (fatigueTr != null) fatigueOverlay = fatigueTr.GetComponent<Image>();

            Transform goPanelTr = canvasGo.transform.Find("GameOver_Panel");
            if (goPanelTr != null)
            {
                gameOverPanel = goPanelTr.gameObject;
                gameOverPanel.SetActive(false);

                Transform goTitleTr = goPanelTr.Find("GameOver_Window/GameOver_Title");
                if (goTitleTr != null) gameOverTitleText = goTitleTr.GetComponent<TextMeshProUGUI>();

                Transform goScoreTr = goPanelTr.Find("GameOver_Window/Score_Text");
                if (goScoreTr != null) gameOverScoreText = goScoreTr.GetComponent<TextMeshProUGUI>();

                Transform goHighScoreTr = goPanelTr.Find("GameOver_Window/HighScore_Text");
                if (goHighScoreTr != null) gameOverHighScoreText = goHighScoreTr.GetComponent<TextMeshProUGUI>();

            }

            Transform pPanelTr = canvasGo.transform.Find("Pause_Panel");
            if (pPanelTr != null)
            {
                pausePanel = pPanelTr.gameObject;
                pausePanel.SetActive(false);
            }

            // wire up buttons
            // wire up Pause panel buttons — scoped to pPanelTr so it can't collide with
            // GameOver_Panel's identically-named MainMenu_Button
            Button pauseBtn = FindButtonInHierarchy(canvasGo.transform, "Pause_Button");
            if (pauseBtn != null) { pauseBtn.onClick.RemoveAllListeners(); pauseBtn.onClick.AddListener(TogglePause); }

                if (pPanelTr != null)
            {
                Button resumeBtn = FindButtonInHierarchy(pPanelTr, "Resume_Button");
                if (resumeBtn != null) { resumeBtn.onClick.RemoveAllListeners(); resumeBtn.onClick.AddListener(ResumeGame); }

                Button pauseMainMenuBtn = FindButtonInHierarchy(pPanelTr, "MainMenu_Button");
                if (pauseMainMenuBtn != null) { pauseMainMenuBtn.onClick.RemoveAllListeners(); pauseMainMenuBtn.onClick.AddListener(GoToMainMenu); }
            }

            // wire up GameOver panel buttons — scoped to goPanelTr, same reasoning
                if (goPanelTr != null)
            {
                Button restartBtn = FindButtonInHierarchy(goPanelTr, "Restart_Button");
                if (restartBtn != null) { restartBtn.onClick.RemoveAllListeners(); restartBtn.onClick.AddListener(RestartGame); }

                Button gameOverMainMenuBtn = FindButtonInHierarchy(goPanelTr, "MainMenu_Button");
                if (gameOverMainMenuBtn != null) { gameOverMainMenuBtn.onClick.RemoveAllListeners(); gameOverMainMenuBtn.onClick.AddListener(GoToMainMenu); }
            }        }

                    ResetState();
    }

    private Button FindButtonInHierarchy(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child.GetComponent<Button>();

        for (int i = 0; i < parent.childCount; i++)
        {
            Button btn = FindButtonInHierarchy(parent.GetChild(i), name);
            if (btn != null) return btn;
        }
        return null;
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void Update()
    {
        if (_isGameOver || _isPaused) return;

        // track distance using the player's actual speed
        if (_player != null)
            distanceTravelled += _player.CurrentSpeed * Time.deltaTime;

        // gradually speed up in endless mode
        if (_currentMode == GameMode.Endless)
        {
            currentSpeed = Mathf.Min(currentSpeed + speedIncreaseRate * Time.deltaTime, maxSpeed);
            if (_player != null) _player.SetBaseSpeed(currentSpeed);
        }

        // count down the maze timer
        if (_currentMode == GameMode.Maze)
        {
            HandleMazeTimer();
            if (_player != null)
            {
                float timeRatio   = currentMazeTime / mazeTimeLimit;
                float speedFactor = Mathf.Lerp(0.3f, 1.0f, timeRatio);
                _player.ApplySpeedMultiplier(speedFactor);
            }
        }

        UpdateAllUI();
    }

    // resets all in-session variables — called on game start and before each restart
    // does NOT reset the saved high score
    public void ResetState()
    {
        _isGameOver              = false;
        _isPaused                = false;
        _isTemporarilyInvincible = false;
        mangoScore               = 0;
        distanceTravelled        = 0f;
        currentLives             = maxLives;
        currentMazeTime          = mazeTimeLimit;
        currentSpeed             = baseSpeed;

        Time.timeScale = 1f;

        if (fatigueOverlay != null)
        {
            Color c = fatigueOverlay.color;
            c.a = 0f;
            fatigueOverlay.color = c;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel    != null) pausePanel.SetActive(false);

        UpdateAllUI();
    }

    public void AddMango(int amount = 1)
    {
        mangoScore += amount;
        UpdateAllUI();
    }

    // called by Obstacle instead of GameOver() directly so we can handle lives first
    public void TakeDamage(string reason = "Hit an obstacle!")
    {
        if (_isGameOver)              return;
        if (_isTemporarilyInvincible) return;
        if (_player != null && _player.IsInvincible()) return;

        currentLives--;
        UpdateAllUI();

        if (currentLives <= 0)
            GameOver(reason);
        else
            StartCoroutine(TemporaryInvincibilityRoutine()); // give the player a moment to recover
    }

    private IEnumerator TemporaryInvincibilityRoutine()
    {
        _isTemporarilyInvincible = true;
        if (_player != null) _player.SetInvincible(true);

        yield return new WaitForSeconds(invincibilityAfterHitDuration);

        _isTemporarilyInvincible = false;
        if (_player != null) _player.SetInvincible(false);
    }

    public void GameOver(string reason = "Caught by the Farmer!")
    {
        if (_isGameOver) return;

        _isGameOver    = true;
        Time.timeScale = 0f;

        Debug.Log("Game Over: " + reason);

        // total score = mangoes + 1 point per 10 metres
        int totalScore = mangoScore + Mathf.FloorToInt(distanceTravelled / 10f);
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }

        if (gameOverPanel         != null) gameOverPanel.SetActive(true);
        if (gameOverTitleText     != null) gameOverTitleText.text = reason;
        if (gameOverScoreText     != null) gameOverScoreText.text = "Score: " + totalScore;
        if (gameOverHighScoreText != null) gameOverHighScoreText.text = "Best: " + highScore;

        UpdateAllUI();
        }

    // toggles pause on/off — safe to bind to a UI button
    public void TogglePause()
    {
        if (_isGameOver) return;
        _isPaused      = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(_isPaused);
    }

    public void ResumeGame()
    {
        if (!_isPaused) return;
        _isPaused      = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void RestartGame()
    {
        ResetState(); // reset flags and timeScale before the scene reloads
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleMazeTimer()
    {
        currentMazeTime -= Time.deltaTime;

        if (currentMazeTime <= 0)
        {
            currentMazeTime = 0;
            GameOver("Time's Up! You didn't escape the maze.");
        }

        UpdateFatigueEffect();
    }

    private void UpdateFatigueEffect()
    {
        if (fatigueOverlay == null) return;
        float fatigueRatio = 1f - (currentMazeTime / mazeTimeLimit);
        Color c = fatigueOverlay.color;
        // quadratic easing so the effect gets dramatic near the end
        c.a = Mathf.Lerp(0, maxFatigueAlpha, fatigueRatio * fatigueRatio);
        fatigueOverlay.color = c;
    }

    private void UpdateAllUI()
    {
        if (scoreText    != null) scoreText.text    = "Mangoes: " + mangoScore;
        if (distanceText != null) distanceText.text = Mathf.FloorToInt(distanceTravelled) + "m";
        if (highScoreText != null) highScoreText.text = "Best: " + highScore;
        if (livesText    != null) livesText.text    = "❤️ " + currentLives;

        if (timerText != null && _currentMode == GameMode.Maze)
            timerText.text = "Time: " + Mathf.Ceil(currentMazeTime) + "s";
    }
}
