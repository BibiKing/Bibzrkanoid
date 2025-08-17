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

    public int TotalAvailableBalls = 3;
    public int TotalAvailablePaddles = 1;
    public bool IsGameStarted { get; set; }
    public int RemainingBalls { get; set; }
    public int RemainingPaddles { get; set; }

    public static event Action<int> OnLiveLost;
    public static event Action OnRoundReset;

    private void Start()
    {
        this.RemainingBalls = this.TotalAvailableBalls;
        Screen.SetResolution(1080, 1920, false);
        Ball.OnBallDeath += OnBallDeath;
        Brick.OnBrickDestruction += OnBrickDestruction;
    }

    private void OnBrickDestruction(Brick obj)
    {
        if (BricksManager.Instance.remainingBricks.Count <= 0)
        {
            CleanupTransient(clearActiveBuffs: true);

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
            this.RemainingBalls--;

            if (this.RemainingBalls < 1)
            {
                CleanupTransient(clearActiveBuffs: true);
                gameOverScreen.SetActive(true);
                return;
            } else
            {
                OnLiveLost?.Invoke(this.RemainingBalls);
                //IsGameStarted = false;
                //BallsManager.Instance.ResetBalls();                
                //BricksManager.Instance.LoadLevel(BricksManager.Instance.currentLevel);
                ResetRoundAfterLifeLost();
            }
        }

    }

    private void CleanupTransient(bool clearActiveBuffs)
    {
        /*
        // 1) Destruir power-ups que ainda estão caindo
        // Opção por Tag (recomendado): marque seus prefabs de drop com a Tag "PowerUp"
        var fallingByTag = GameObject.FindGameObjectsWithTag("Buff");
        for (int i = 0; i < fallingByTag.Length; i++)
            Destroy(fallingByTag[i]);
        */

        // Opcional: se você tem uma classe base p/ power-ups, descomente:
        foreach (var pu in FindObjectsByType<Buff>(FindObjectsSortMode.InstanceID)) Destroy(pu.gameObject);

        // 2) Limpar efeitos ativos (paddle/bolas)
        if (clearActiveBuffs)
        {
            // Estes métodos precisam existir nessas classes (implemente neles)
            Paddle.Instance.ClearAllEffectsAndResetState();
            BallsManager.Instance.ResetBalls();
        }

        // 3) Sinal opcional p/ quem quiser ouvir e se limpar também
        OnRoundReset?.Invoke();
    }

    private void ResetRoundAfterLifeLost()
    {
        CleanupTransient(clearActiveBuffs: true);

        // Para a rodada atual e reposiciona tudo
        IsGameStarted = false;
        BallsManager.Instance.ResetBalls(); // volta para a bola “presa” no paddle, por exemplo

        // Se seu Paddle não reseta no ResetBalls, garanta reset explícito:
        Paddle.Instance.ResetToDefaultPoseAndParams(); // implemente se necessário
    }

    private void OnDisable()
    {
        Ball.OnBallDeath -= OnBallDeath;
        Brick.OnBrickDestruction -= OnBrickDestruction;
    }

    internal void ShowVictoryScreen()
    {
        victoryScreen.SetActive(true);
        
    }
}
