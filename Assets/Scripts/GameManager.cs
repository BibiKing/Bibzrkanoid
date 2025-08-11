using System;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region Singleton

    private static GameManager _instance;

    public static GameManager Instance => _instance;

    private void Awake()
    {
        if(_instance != null)
        {
            Destroy(gameObject);
        } else
        {
            _instance = this;
        }
    }

    #endregion

    public GameObject gameOverScreen;
    public GameObject victoryScreen;

    public int AvailableLives = 3;
    public bool IsGameStarted { get; set; }
    public int Lives { get; set; }

    public static event Action<int> OnLiveLost;

    private void Start()
    {
        this.Lives = this.AvailableLives;
        Screen.SetResolution(1080, 1920, false);
        Ball.OnBallDeath += OnBallDeath;
        Brick.OnBrickDestruction += OnBrickDestruction;
    }

    private void OnBrickDestruction(Brick obj)
    {
        if (BricksManager.Instance.remainingBricks.Count <= 0)
        {
            BallsManager.Instance.ResetBalls();
            GameManager.Instance.IsGameStarted = false;
            BricksManager.Instance.LoadNextLevel();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnBallDeath(Ball ball)
    {
        if (BallsManager.Instance.balls.Count <= 0)
        {
            this.Lives--;

            if (this.Lives < 1)
            {
                gameOverScreen.SetActive(true);
                return;
            } else
            {
                OnLiveLost?.Invoke(this.Lives);
                IsGameStarted = false;
                BallsManager.Instance.ResetBalls();                
                BricksManager.Instance.LoadLevel(BricksManager.Instance.currentLevel);
            }
        }

    }

    private void OnDisable()
    {
        Ball.OnBallDeath -= OnBallDeath;
    }

    internal void ShowVictoryScreen()
    {
        victoryScreen.SetActive(true);
        
    }
}
