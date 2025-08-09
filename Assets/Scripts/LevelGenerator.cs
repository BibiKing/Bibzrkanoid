using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    #region Singleton

    private static LevelGenerator _instance;

    public static LevelGenerator Instance => _instance;

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

    private int totalLevels;
    private int maxLives;
    public int seed;

    private int rowCount;
    private int colCount;

    private List<int[,]> levels;
    private List<string> specialLevels;
    private List<int> specialLevelsIndexes;
    private int nextSpecialLevel;
    private System.Random random;

    private void Start()
    {
        this.random = seed != 0 ? new System.Random(seed) : new System.Random();
        this.levels = new List<int[,]>();
        this.specialLevels = new List<string>();

        rowCount = BricksManager.Instance.maxRows;
        colCount = BricksManager.Instance.maxCols;
        maxLives = BricksManager.Instance.sprites.Count();
        totalLevels = BricksManager.Instance.maxLevels;

        specialLevelsIndexes = GenerateSpecialLevels();
        nextSpecialLevel = 0;
        GenerateLevels();
    }

    // Formas pré-definidas
    private Dictionary<string, string[]> shapes = new Dictionary<string, string[]>
    {
        { "quadrado", new[] { "111", "111", "111" } },
        { "retangulo", new[] { "11111", "11111", "11111" } },
        { "bloco_largo", new[] { "1111111", "1111111" } },
        { "bloco_alto", new[] { "11", "11", "11", "11", "11" } },
        { "cruz", new[] { "00100", "11111", "00100" } },
        { "losango", new[] { "00100", "01110", "11111", "01110", "00100" } },
        { "triangulo", new[] { "00100", "01110", "11111" } },
        { "triangulo_ret_esq", new[] { "1", "11", "111" } },
        { "triangulo_ret_dir", new[] { "001", "011", "111" } },
        { "estrela", new[] { "00100", "11111", "01110", "11111", "00100" } },
        { "coracao", new[] {
            "01100110", "11111111", "11111111",
            "01111110", "00111100", "00011000"
        }},
        { "space_invader", new[] {
            "00111100","01111110","11011011",
            "11111111","10111101","00100100","01000010"
        }}
    };

    /// <summary>
    /// Retorna a matriz de um level pelo índice.
    /// </summary>
    public int[,] GetLevel(int index)
    {
        if (index < 0 || index >= levels.Count)
            throw new ArgumentOutOfRangeException("Index inválido.");
        return levels[index];
    }

    public List<int[,]> GetAllLevels()
    {
        return levels;
    }

    /// <summary>
    /// Exporta todos os níveis para um arquivo TXT.
    /// </summary>
    public void ExportToTxt(string filePath)
    {
        using (StreamWriter sw = new StreamWriter(filePath))
        {
            foreach (var lvl in levels)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    string[] row = new string[colCount];
                    for (int x = 0; x < colCount; x++)
                        row[x] = lvl[y, x].ToString();
                    sw.WriteLine(string.Join(",", row));
                }
                sw.WriteLine("--");
            }
        }
    }

    /// <summary>
    /// Gera todos os níveis com progressão de dificuldade e densidade.
    /// </summary>
    
    private List<int> GenerateSpecialLevels()
    {
        specialLevels.Add("coracao");
        specialLevels.Add("space_invader");

        List<int> specialLevelsIndexes = new List<int>();

        for (int i = 0; i < specialLevels.Count; i++)
        {
            specialLevelsIndexes.Add(random.Next(0, totalLevels/2));
        }

        return specialLevelsIndexes;
               
    }

    private int[,] GenerateNextSpecialLevel(int vidaMin, int vidaMax)
    {
        int[,] specialLevel = CreateEmptyLevel();
        string shape = specialLevels[nextSpecialLevel];
        int offsetX;
        int width = shapes[shape][0].Length;
        int offsetY;
        int height = shapes[shape].Length;
        int vida;

        offsetX = (rowCount - width)/2;
        offsetY = (colCount - height)/2;
        vida = random.Next(vidaMin, vidaMax);


        InsertShape(specialLevel, shapes[shape], offsetX, offsetY, vida);
        nextSpecialLevel++;

        return specialLevel;
    }

    private void GenerateLevels()
    { 
        // níveis progressivos
        for (int i = 0; i < totalLevels; i++)
        {

            // Cálculo para vida mínima: linear de 1 no nível 1 até 50% do máximo no último nível
            double vidaMinimaDouble = 1 + (i - 1) * ((maxLives * 0.5) - 1) / (totalLevels - 1);
            int vidaMin = (int)Math.Round(vidaMinimaDouble);

            // Cálculo para vida máxima: linear de 1 no nível 1 até quantidadeMaximaDeVidas no último nível
            double vidaMaximaDouble = 1 + (i - 1) * (maxLives - 1) / (totalLevels - 1);
            int vidaMax = (int)Math.Round(vidaMaximaDouble);

            // Garantir que os valores não sejam menores que 1
            vidaMin = Math.Max(1, vidaMin);
            vidaMax = Math.Max(1, vidaMax);

            // Garantir que a vida mínima não ultrapasse a vida máxima
            vidaMin = Math.Min(vidaMin, vidaMax);


            int targetBlocks = 20 + (int)((i / (float)(totalLevels - 3)) * 150);

            int[,] level;
            do
            {
                if (specialLevelsIndexes.Contains(i) && nextSpecialLevel < specialLevels.Count)
                {
                    level = GenerateNextSpecialLevel(vidaMin, vidaMax);
                }
                else
                {
                    level = GenerateSymmetricLevel(vidaMin, vidaMax, targetBlocks);
                }

            }
            while (LevelExists(level));

            levels.Add(level);
        }
    }

    /// <summary>
    /// Gera um nível simétrico horizontalmente com densidade e vida especificadas.
    /// </summary>
    private int[,] GenerateSymmetricLevel(int vidaMin, int vidaMax, int targetBlocks)
    {
        int[,] lvl = CreateEmptyLevel();
        int totalBlocks = 0;

        while (totalBlocks < targetBlocks)
        {
            string[] shape = GetRandomShape();
            int shapeWidth = shape[0].Length;
            int shapeHeight = shape.Length;

            int offsetX = random.Next(0, 8 - shapeWidth / 2);
            int offsetY = random.Next(0, 15 - shapeHeight);

            int vida = GetRandomVida(vidaMin, vidaMax);
            InsertShape(lvl, shape, offsetX, offsetY, vida);

            int espelho = 14 - offsetX - shapeWidth + 1;
            InsertShape(lvl, shape, espelho, offsetY, vida);

            totalBlocks = CountBlocks(lvl);
        }

        return lvl;
    }

    private string[] GetRandomShape()
    {
        List<string[]> values = new List<string[]>(shapes.Values);
        return values[random.Next(values.Count)];
    }

    private int[,] CreateEmptyLevel()
    {
        return new int[rowCount, colCount];
    }

    private void InsertShape(int[,] level, string[] shape, int offsetX, int offsetY, int vida)
    {
        for (int y = 0; y < shape.Length; y++)
        {
            for (int x = 0; x < shape[y].Length; x++)
            {
                if (shape[y][x] == '1')
                {
                    int lx = offsetX + x;
                    int ly = offsetY + y;
                    if (lx >= 0 && lx < colCount && ly >= 0 && ly < rowCount)
                    {
                        level[ly, lx] = vida;
                    }
                }
            }
        }
    }

    private int CountBlocks(int[,] level)
    {
        int count = 0;
        for (int y = 0; y < rowCount; y++)
            for (int x = 0; x < colCount; x++)
                if (level[y, x] > 0) count++;
        return count;
    }

    private int GetRandomVida(int min, int max)
    {
        List<int> pool = new List<int>();
        for (int v = min; v <= max; v++)
        {
            int weight = (v < max) ? 3 : 1;
            for (int k = 0; k < weight; k++)
                pool.Add(v);
        }
        return pool[random.Next(pool.Count)];
    }

    private bool LevelExists(int[,] candidate)
    {
        foreach (var lvl in levels)
        {
            bool same = true;
            for (int y = 0; y < rowCount && same; y++)
                for (int x = 0; x < colCount && same; x++)
                    if (lvl[y, x] != candidate[y, x])
                        same = false;
            if (same) return true;
        }
        return false;
    }
}
