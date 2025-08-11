using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    public int maxRows = 15;
    public int maxCols = 15;
    public int maxLevels = 15;

    private GameObject bricksContainer;
    private float initialBrickStartPositionX = -2.35f;
    private float initialBrickStartPositionY = 3.8f;
    private float blockShiftAmount = 0.335f;

    public Brick brickPrefab;

    public Sprite[] sprites;
    //public string levelsFile = "levels_15_hp6";

    public Color[] brickColors;
    public List<Brick> remainingBricks { get; set; }

    public List<int[,]> levelsData { get; set; }

    public int initialBricksCount { get; set; }

    public int currentLevel;
    

    private void Start()
    {
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

        for (int row = 0; row < this.maxRows; row++)
        {
            for (int col = 0; col < this.maxCols; col++)
            {
                int brickType = currentLevelData[row, col];

                if(brickType > 0)
                {
                    Brick newBrick = Instantiate(brickPrefab, new Vector3(currentSpawnX, currentSpawnY, 0.0f - zShift), Quaternion.identity) as Brick;
                    newBrick.Init(bricksContainer.transform, this.sprites[brickType-1], this.brickColors[brickType], brickType);

                    this.remainingBricks.Add(newBrick);
                    zShift += 0.0001f;
                }

                currentSpawnX += blockShiftAmount;

                if(col +1 ==  this.maxCols)
                {
                    currentSpawnX = initialBrickStartPositionX;
                }
            }

            currentSpawnY -= blockShiftAmount;
        }

        this.initialBricksCount = this.remainingBricks.Count;
        OnLevelLoaded?.Invoke();
    }

    /*
    private List<int[,]> LoadLevelsData()
    {
        TextAsset levelsText = Resources.Load(levelsFile) as TextAsset;

        string[] rows = levelsText.text.Split(new String[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        List<int[,]> levelsData = new List<int[,]>();
        int[,] currentLevel = new int[maxRows, maxCols];

        int currentRow = 0;

        for (int row = 0; row < rows.Length; row++)
        {
            string line = rows[row];

            if (line.IndexOf("--") == -1)
            {
                string[] bricks = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int col = 0; col < bricks.Length; col++)
                {
                    currentLevel[currentRow, col] = int.Parse(bricks[col]);
                }

                currentRow++;
            }
            else
            {
                currentRow = 0;
                levelsData.Add(currentLevel);
                currentLevel = new int[maxRows, maxCols];
            }
        }
        return levelsData;
    }*/

    private List<int[,]> LoadLevelsData()
    {
        return LevelGenerator.Instance.GetAllLevels();
    }

    public void LoadLevel(int level)
    {
        this.currentLevel = level;
        this.ClearRemainingBricks();
        this.GenerateBricks();
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
}
