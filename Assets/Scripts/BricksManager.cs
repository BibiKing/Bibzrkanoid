using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-800)]
public class BricksManager : MonoBehaviour
{
    #region Singleton

    private static BricksManager _instance;

    public static BricksManager Instance => _instance;

    public static event Action OnLevelLoaded;

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

        
    }

    #endregion

    public int maxRows = 18;
    public int maxCols = 12;
    public int maxLevels = 15;
    public int maxLives = 10;

    private GameObject bricksContainer;
    private float initialBrickStartPositionX = -2.3f;
    private float initialBrickStartPositionY = 2.6f;
    private float blockShiftAmountX = 0.42f;
    private float blockShiftAmountY = 0.23f;

    private Brick brickPrefab;

    public int colorPaletteIndex = 1;

    public List<Brick> remainingBricks { get; set; }

    public List<int[,]> levelsData { get; set; }

    public int initialBricksCount { get; set; }

    public int currentLevel;
    

    private void Start()
    {
        brickPrefab = AssetsManager.Instance.brickPrefab;
        if (brickPrefab == null)
        {
            Debug.LogError("[BricksManager] brickPrefab não atribuído no AssetsManager.");
            return; // evita Instantiate(null)
        }

        var palettes = AssetsManager.Instance.colorPalettes;
        int count = palettes?.Length ?? 0;
        if (count == 0)
        {
            Debug.LogWarning("[BricksManager] Nenhuma colorPalette atribuída. Usando índice 0 com fallback.");
            colorPaletteIndex = 0;
        }
        else
        {
            colorPaletteIndex = LevelGenerator.Instance.random.Next(0, count);
        }
        this.bricksContainer = new GameObject("BricksContainer");        
        this.levelsData = this.LoadLevelsData();
        this.GenerateBricks();        
    }

    private void GenerateBricks()
    {
        this.remainingBricks = new List<Brick>();
        int[,] currentLevelData = this.levelsData[this.currentLevel];
        float currentSpawnX = initialBrickStartPositionX;
        float currentSpawnY = initialBrickStartPositionY;
        float zShift = 0;

        // NOVO: resolver de cores baseado na paleta e no layout do nível
        var levelJagged = ToJagged(currentLevelData);
        Func<int, int, Color32> colorResolver;
        bool useSpecial = LevelGenerator.Instance.IsLevelSpecialColor(this.currentLevel);
        if (useSpecial)
        {
            // Modo vitrine para destacar o desenho da pattern: 1/2 = mesma cor; demais por valor
            colorResolver = BlockColorizer.MakeSpecialPatternResolver(
                colorPaletteIndex,
                levelJagged,
                zerosAsClear: true
            );
        }
        else
        {
            // Padrões “normais” (listras, quadriculado, perlin, etc.)
            int symHint = 0;
            if (LevelGenerator.Instance != null)
                symHint = LevelGenerator.Instance.GetSymmetryHintForLevel(this.currentLevel);

            colorResolver = BlockColorizer.MakeColorResolver(
                colorPaletteIndex,
                levelJagged,
                zerosAsClear: true,
                symmetryHint: symHint,
                seedOverride: LevelGenerator.Instance != null ? LevelGenerator.Instance.effectiveSeed : 0
            );
        }

        for (int row = 0; row < this.maxRows; row++)
        {
            for (int col = 0; col < this.maxCols; col++)
            {
                int brickType = currentLevelData[row, col];
                Color32 color = colorResolver(row,col);

                if(brickType == -1)
                {
                    Brick newBrick = Instantiate(brickPrefab, new Vector3(currentSpawnX, currentSpawnY, 0.0f - zShift), Quaternion.identity) as Brick;
                    newBrick.Init(bricksContainer.transform, color, -1, true);
                    var l2d = newBrick.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
                    if (l2d != null) l2d.color = color; // só se existir

                    zShift += 0.0001f;
                }
                else if(brickType > 0)
                {
                    Brick newBrick = Instantiate(brickPrefab, new Vector3(currentSpawnX, currentSpawnY, 0.0f - zShift), Quaternion.identity) as Brick;
                    newBrick.Init(bricksContainer.transform, color, brickType);
                    var l2d = newBrick.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
                    if (l2d != null) l2d.color = color; // só se existir

                    this.remainingBricks.Add(newBrick);
                    zShift += 0.0001f;
                }

                currentSpawnX += blockShiftAmountX;

                if(col +1 ==  this.maxCols)
                {
                    currentSpawnX = initialBrickStartPositionX;
                }
            }

            currentSpawnY -= blockShiftAmountY;
        }

        this.initialBricksCount = this.remainingBricks.Count;
        OnLevelLoaded?.Invoke();
    }

    private List<int[,]> LoadLevelsData()
    {
        return LevelGenerator.Instance.GetAllLevels();
    }

    public void LoadLevel(int level)
    {
        this.currentLevel = level;
        this.ClearRemainingBricks();
        this.GenerateBricks();

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateLevel(currentLevel, maxLevels);
    }

    private void ClearRemainingBricks()
    {
        foreach(var brick in this.remainingBricks.ToList())
        {
            Destroy(brick.gameObject);
        }
    }

    public void LoadNextLevel()
    {
        this.currentLevel++;

        if(this.currentLevel >= this.levelsData.Count())
        {
            GameManager.Instance.ShowVictoryScreen();
        } else
        {
            this.LoadLevel(this.currentLevel);
        }
    }

    private static int[][] ToJagged(int[,] rect)
    {
        int rows = rect.GetLength(0);
        int cols = rect.GetLength(1);
        var jag = new int[rows][];
        for (int r = 0; r < rows; r++)
        {
            jag[r] = new int[cols];
            for (int c = 0; c < cols; c++)
                jag[r][c] = rect[r, c];
        }
        return jag;
    }
}
