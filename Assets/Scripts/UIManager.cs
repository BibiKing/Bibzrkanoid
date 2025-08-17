using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

[DefaultExecutionOrder(-900)]
public class UIManager : MonoBehaviour
{
    private TMP_Text TargetText;
    private TMP_Text ScoreText;
    private TMP_Text LevelText;

    private List<Transform> paddleLives;
    private List<Transform> remainingBalls;

    private float paddleLifePrefabStartingX = -2.3f;
    private float paddleLifePrefabStartingY = -4.5f;
    private float paddleLifePrefabStartingOffset = 0.4f;
    private float remainingBallsPrefabStartingX = -2.4f;
    private float remainingBallsPrefabStartingY = -4.68f;
    private float remainingBallsPrefabStartingOffset = 0.15f;

    private Transform paddleLifePrefab;
    private Transform ballRemainingPrefab;

    private EffectSpawner smallExplosion;

    private CameraShake cameraShake;

    public int Score {  get; set; }

    #region Singleton

    private static UIManager _instance;

    public static UIManager Instance => _instance;


    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }

        LoadFromAssetManager();

        if (!TargetText || !ScoreText || !LevelText)
            Debug.LogError("[UIManager] TMP_Text não atribuídos (via AssetsManager). Arraste no Inspector do AssetsManager!");


        Brick.OnBrickDestruction += OnBrickDestruction;
        BricksManager.OnLevelLoaded += OnLevelLoaded;
        GameManager.OnLiveLost += OnLiveLost;
    }
    #endregion


    private void Start()
    {
        // Fallback: se no Awake ainda faltava algo, tente resolver aqui de novo
        if ((!TargetText || !ScoreText || !LevelText))
        {
            LoadFromAssetManager();
        }

        // Se mesmo assim faltar, evita NRE e alerta
        if (!TargetText || !ScoreText || !LevelText)
        {
            Debug.LogError("[UIManager] TMP_Text ainda não atribuídos. Verifique os campos no AssetsManager da cena.");
            return;
        }

        StartRemainingLives();

        OnLiveLost(GameManager.Instance.TotalAvailableBalls);
        UpdateLevel(BricksManager.Instance.currentLevel, BricksManager.Instance.maxLevels);
        UpdateRemainingBricksText();
        UpdateScoreText(0);
    }

    private void LoadFromAssetManager()
    {
        var am = AssetsManager.Instance ?? FindFirstObjectByType<AssetsManager>();
        if (am != null)
        {
            if (!ScoreText) ScoreText = am.scoreTextTMP;
            if (!TargetText) TargetText = am.targetTextTMP;
            if (!LevelText) LevelText = am.levelTextTMP;
            paddleLifePrefab = am.paddleLifePrefab;
            ballRemainingPrefab = am.remainingBallsPrefab;
            smallExplosion = am.smallExplosion;
            cameraShake = am.cameraShake;
        }
    }

    private void StartRemainingLives()
    {
        int maxPaddles = GameManager.Instance.TotalAvailablePaddles;
        int maxLifes = GameManager.Instance.TotalAvailableBalls;

        for(int i = 0; i < maxPaddles; i++)
        {
            AddRemainingPaddle();
        }

        for (int i = 0; i < maxLifes; i++)
        {
            AddRemainingBall();
        }

    }

    private void AddRemainingBall()
    {
        if(remainingBalls == null || remainingBalls.Count == 0)
        {
            remainingBalls = new List<Transform>();
        }
        float positionX = remainingBallsPrefabStartingX + (remainingBallsPrefabStartingOffset * remainingBalls.Count);
        Vector3 position = new Vector3(positionX, remainingBallsPrefabStartingY, 0);
        Transform ballToAdd = Instantiate(ballRemainingPrefab, position, Quaternion.identity);
        remainingBalls.Add(ballToAdd);
    }

    private void RemoveRemainingBall()
    {
        int index = remainingBalls.Count - 1;

        if (GameManager.Instance.IsGameStarted)
        {
            Vector3 removedBallPosition = remainingBalls[index].transform.position;
            smallExplosion.SpawnAllEffects(removedBallPosition);
            cameraShake.Shake();
        }

        Transform ballToRemove = remainingBalls[index];
        Destroy(ballToRemove.gameObject);
        remainingBalls.Remove(ballToRemove);

    }

    private void AddRemainingPaddle()
    {
        if (paddleLives == null || paddleLives.Count == 0)
        {
            paddleLives = new List<Transform>();
        }
        float positionX = paddleLifePrefabStartingX + (paddleLifePrefabStartingOffset * paddleLives.Count);
        Vector3 position = new Vector3(positionX, paddleLifePrefabStartingY, 0);
        Transform paddleToAdd = Instantiate(paddleLifePrefab, position, Quaternion.identity);
        paddleLives.Add(paddleToAdd);
    }

    private void RemoveRemainingPaddle()
    {
        int index = paddleLives.Count - 1;
        Vector3 removedPaddlePosition = paddleLives[index].transform.position; //will use for effect
        Transform paddleToRemove = paddleLives[index];
        Destroy(paddleToRemove.gameObject);
        paddleLives.Remove(paddleToRemove);
    }

    private void OnLiveLost(int remainingLives)
    {
        RemoveRemainingBall();
    }

    private void OnLevelLoaded()
    {
        UpdateRemainingBricksText();
        UpdateScoreText(0);
    }

    private void UpdateScoreText(int increment)
    {
        this.Score += increment;
        string scoreString = this.Score.ToString().PadLeft(5, '0');
        ScoreText.text = $"SCORE:{Environment.NewLine}{scoreString}";
    }

    private void OnBrickDestruction(Brick obj)
    {
        UpdateRemainingBricksText();
        UpdateScoreText(obj.points);
    }

    private void UpdateRemainingBricksText()
    {
        // Guards para NRE e para quando a geração ainda não terminou
        if (!TargetText) { Debug.LogWarning("[UIManager] TargetText nulo."); return; }
        if (BricksManager.Instance == null || BricksManager.Instance.remainingBricks == null)
        {
            // pode acontecer na primeira carga, antes do GenerateBricks terminar
            return;
        }

        TargetText.text = $"Target:{Environment.NewLine}{BricksManager.Instance.remainingBricks.Count} / {BricksManager.Instance.initialBricksCount}";
    }

    public void UpdateLevel(int current, int max)
    {
        LevelText.text = $"LEVEL: {current+1}/{max}";
    }

    private void OnDisable()
    {
        Brick.OnBrickDestruction -= OnBrickDestruction;
        BricksManager.OnLevelLoaded -= OnLevelLoaded;
        GameManager.OnLiveLost -= OnLiveLost;
    }

}
