using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TMP_Text TargetText;
    public TMP_Text ScoreText;
    public TMP_Text LivesText;

    public int Score {  get; set; }

    private void Awake()
    {
        Brick.OnBrickDestruction += OnBrickDestruction;
        BricksManager.OnLevelLoaded += OnLevelLoaded;
        GameManager.OnLiveLost += OnLiveLost;
    }

    private void Start()
    {
        OnLiveLost(GameManager.Instance.AvailableLives);
    }

    private void OnLiveLost(int remainingLives)
    {
        LivesText.text = $"LIVES: {remainingLives}";
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
        UpdateScoreText(10);
    }

    private void UpdateRemainingBricksText()
    {
        TargetText.text = $"Target:{Environment.NewLine}{BricksManager.Instance.remainingBricks.Count} / {BricksManager.Instance.initialBricksCount}";
    }

    private void OnDisable()
    {
        Brick.OnBrickDestruction -= OnBrickDestruction;
        BricksManager.OnLevelLoaded -= OnLevelLoaded;
    }
}
