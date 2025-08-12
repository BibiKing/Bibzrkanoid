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

        this.random = seed != 0 ? new System.Random(seed) : new System.Random();
        this.levels = new List<int[,]>();
        this.specialLevels = new List<string>();

        rowCount = BricksManager.Instance.maxRows;
        colCount = BricksManager.Instance.maxCols;
        maxLives = BricksManager.Instance.sprites.Count();
        totalLevels = BricksManager.Instance.maxLevels;

        GenerateSpecialLevels();
        nextSpecialLevel = 0;
        GenerateLevels();
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
    public System.Random random;

    private void Start()
    {
        
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
        }},
        { "escada_alternada", new[] { "100000", "110000", "101000", "101100", "101010", "101011" } },
        { "espiral", new[] { "1111111", "0000001", "1111101", "1000101", "1011101", "1010001", "1111111" } },
        { "zigzag", new[] { "1000001", "0100010", "0010100", "0001000", "0010100", "0100010", "1000001" } },
        { "piramide_oca", new[] { "0001000", "0011100", "0100010", "1000001" } },
        { "circulo", new[] { "0011100", "0100010", "1000001", "1000001", "0100010", "0011100" } },
        { "seta", new[] { "0001000", "0001000", "1111111", "0001000", "0001000" } },
        { "x_nucleo", new[] { "1000001", "0100010", "0010100", "0001000", "0010100", "0100010", "1000001" } },
        { "labirinto", new[] { "1111111", "1000001", "1011101", "1010101", "1011101", "1000001", "1111111" } },
        { "ondas", new[] { "0000000", "0011000", "0100100", "1000010", "0100100", "0011000" } },
        { "hexagono", new[] { "0011100", "0100010", "1000001", "0100010", "0011100" } },
        { "letra_t", new[] { "1111111", "0001000", "0001000", "0001000" } },
        { "circulo_pontilhado", new[] { "0010100", "0100010", "1000001", "0100010", "0010100" } },
        { "escudo", new[] { "0011100", "0111110", "1111111", "1111111", "0111110", "0011100" } },
        { "ponte", new[] { "1100011", "1100011", "0000000", "1100011", "1100011" } },
        { "nuvem", new[] { "0111110", "1111111", "1111111", "0111110" } },
        { "diamante_risco", new[] { "0001000", "0010100", "0101010", "0010100", "0001000" } },
        { "flecha_dupla", new[] { "00100", "01110", "11111", "01110", "00100" } },
        { "cogumelo", new[] { "0011100", "0111110", "1111111", "0001000", "0001000" } },
        { "roda", new[] { "01110", "11011", "11011", "01110" } },
        { "puzzle", new[] { "110011", "110011", "000000", "001100", "001100" } }
    };

    private Dictionary<string, string[]> specialShapes = new Dictionary<string, string[]>
{
    // Padrão de diamante escalonado (como no seu exemplo)
    { "diamante_escalonado", new[]
    {
        "010000000000000",
        "001000000000000",
        "010100000000000",
        "001010000000000",
        "010101000000000",
        "002020200000000",
        "020202020000000",
        "002020202000000",
        "030303030300000",
        "033333333330000",
        "000000000000000",
        "000000000000000",
        "000000000000000",
        "000000000000000",
        "000000000000000"
    }},

    // Lua crescente
    { "lua_crescente", new[]
    {
        "000001111000000",
        "000111111100000",
        "001111111110000",
        "011111111000000",
        "111111110000000",
        "111111100000000",
        "111111000000000",
        "111110000000000",
        "111111000000000",
        "011111100000000",
        "011111110000000",
        "001111111000000",
        "000111111100000",
        "000011111110000",
        "000001111100000"
    }},

    // Foice e Martelo (estilizado)
    { "foice_martelo", new[]
    {
        "000000111000000",
        "000000011100000",
        "000000001110000",
        "000000000111000",
        "111100000011100",
        "011110000001110",
        "001111000000000",
        "000111100000000",
        "000011110000000",
        "000001111000000",
        "000000111100000",
        "000000011110000",
        "000000001111000",
        "000000000111100",
        "000000000011110"
    }},

    // Smiley feliz
    { "smiley", new[]
    {
        "000011111110000",
        "000100000001000",
        "001000000000100",
        "010000000000010",
        "100010000100001",
        "100000000000001",
        "100000000000001",
        "100100000001001",
        "100011111110001",
        "010000000000010",
        "001000000000100",
        "000100000001000",
        "000011111110000",
        "000000000000000",
        "000000000000000"
    }},

    // Nave espacial (estilo pixel art)
    { "nave_espacial", new[]
    {
        "000000001000000",
        "000000011100000",
        "000000111110000",
        "000001111111000",
        "000011111111100",
        "000111111111110",
        "001111111111111",
        "011111111111111",
        "111111111111111",
        "011111111111110",
        "001111111111100",
        "000111111111000",
        "000011111110000",
        "000001111100000",
        "000000111000000"
    }},

    // Árvore de Natal
    { "arvore_natal", new[]
    {
        "000000100000000",
        "000000100000000",
        "000001110000000",
        "000001110000000",
        "000011111000000",
        "000011111000000",
        "000111111100000",
        "000111111100000",
        "001111111110000",
        "001111111110000",
        "011111111111000",
        "011111111111000",
        "111111111111100",
        "000001110000000",
        "000001110000000"
    }},

    // Caveira (estilo pixel art)
    { "caveira", new[]
    {
        "000111111111000",
        "001100000001100",
        "011000000000110",
        "011000000000110",
        "110011111100111",
        "110110011011011",
        "110000000000011",
        "110000000000011",
        "110011111100011",
        "011001111001110",
        "011000000001110",
        "001100000011100",
        "000111111111000",
        "000000000000000",
        "000000000000000"
    }},

    // Notas musicais
    { "notas_musicais", new[]
    {
        "000000000000000",
        "000000000000000",
        "000000000000000",
        "000000000000000",
        "000011000110000",
        "000011001111000",
        "000011011000000",
        "000011110000000",
        "000011100000000",
        "000011000000000",
        "000011000000000",
        "000011000000000",
        "000000000000000",
        "000000000000000",
        "000000000000000"
    }},

    // Coroa
    { "coroa", new[]
    {
        "001111111111100",
        "010000000000010",
        "100000000000001",
        "100101111010001",
        "100101111010001",
        "100101111010001",
        "100101111010001",
        "100101111010001",
        "100101111010001",
        "100000000000001",
        "100000000000001",
        "100000000000001",
        "011111111111110",
        "000000000000000",
        "000000000000000"
    }},

    // Dragão (silhueta)
    { "dragao", new[]
    {
        "000000000111100",
        "000000011000010",
        "000001100000010",
        "000110000000010",
        "001000000000100",
        "010000000001000",
        "100000000110000",
        "100000111000000",
        "110001000000000",
        "011010000000000",
        "001100000000000",
        "000111111000000",
        "000000001100000",
        "000000000110000",
        "000000000011000"
    }},
    { "coracao", new[] 
    {
        "01100110",
        "11111111",
        "11111111",
        "01111110",
        "00111100",
        "00011000"
    }},
    { "space_invader", new[] {
        "00111100",
        "01111110",
        "11011011",
        "11111111",
        "10111101",
        "00100100",
        "01000010"
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
        if(levels == null)
        {
            GenerateLevels();
        }
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
    
    private void GenerateSpecialLevels()
    {

        specialLevelsIndexes = new List<int>();
        int numberOfSpecialLevels;
        numberOfSpecialLevels = random.Next(Math.Min(1, totalLevels-1),totalLevels/3);


        for (int i = 0; i < numberOfSpecialLevels; i++)
        {
            int index;

            do
            {
                index = random.Next(0, Math.Max(2, totalLevels - 2));
            } while(specialLevelsIndexes.Contains(index));
            specialLevelsIndexes.Add(index);
            

            do
            {
                index = random.Next(0, specialShapes.Count());
            } while(specialLevels.Contains(specialShapes.Keys.ElementAt(index)));

            specialLevels.Add(specialShapes.Keys.ElementAt(index));
        }

    }

    private int[,] GenerateNextSpecialLevel(int vidaMin, int vidaMax)
    {
        int[,] specialLevel = CreateEmptyLevel();
        string shape = specialLevels[nextSpecialLevel];
        int offsetX;
        int width = specialShapes[shape][0].Length;
        int offsetY;
        int height = specialShapes[shape].Length;
        int vida;

        offsetX = (rowCount - width)/2;
        offsetY = (colCount - height)/2;
        vida = random.Next(vidaMin, vidaMax);


        InsertShape(specialLevel, specialShapes[shape], offsetX, offsetY, vida);
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
            //Garantir que no últmo nível a vida máxima seja o máximo
            if ( i == totalLevels - 1)
            {
                vidaMax = maxLives;
            }



            int targetBlocks = 20 + (int)((i / (float)(totalLevels - 3)) * 150);

            int[,] level;
            do
            {
                if (specialLevelsIndexes.Contains(i) && nextSpecialLevel < specialLevelsIndexes.Count)
                {
                    level = GenerateNextSpecialLevel(vidaMin, vidaMax);
                }
                else
                {
                    level = GenerateSymmetricLevel(vidaMin, vidaMax, targetBlocks);
                }
            }
            while (LevelExists(level));

            ApplyLifePattern(level, vidaMin, vidaMax);

            levels.Add(level);
        }
    }

    /// <summary>
    /// Aplica um padrão de substituição aos blocos não vazios de um nível.
    /// </summary>
    private void ApplyLifePattern(int[,] level, int vidaMin, int vidaMax) 
    {
        if (level == null || level.Length == 0 || vidaMin >= vidaMax)
            return;

        int rows = level.GetLength(0);
        int cols = level.GetLength(1);

        // Garante valores válidos
        vidaMin = Mathf.Max(1, vidaMin);
        vidaMax = Mathf.Max(1, vidaMax);

        // Pré-calcula todos os valores necessários
        int pattern = random.Next(9);
        int[] patternValues = new int[Mathf.Max(rows, cols)];
        int[] chessValues = { random.Next(vidaMin, vidaMax + 1),
                         random.Next(vidaMin, vidaMax + 1) };

        // Evita divisão por zero
        int centerX = cols / 2;
        int centerY = rows / 2;
        float maxDist = Mathf.Max(1, centerX + centerY);
        float maxRadius = Mathf.Max(1, Mathf.Sqrt(centerX * centerX + centerY * centerY));

        // Limite de tentativas para o padrão xadrez
        const int maxAttempts = 100;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (level[y, x] > 0)
                {
                    int newValue = level[y, x];

                    switch (pattern)
                    {
                        case 0: // random
                            newValue = random.Next(vidaMin, vidaMax + 1);
                            break;

                        case 1: // diagonal
                            if (x == y || x + y == cols - 1)
                                newValue = random.Next(vidaMin, vidaMax + 1);
                            break;

                        case 2: // faixas_horizontais
                            if (x == 0) patternValues[y] = random.Next(vidaMin, vidaMax + 1);
                            newValue = patternValues[y];
                            break;

                        case 3: // faixas_verticais
                            if (y == 0) patternValues[x] = random.Next(vidaMin, vidaMax + 1);
                            newValue = patternValues[x];
                            break;

                        case 4: // xadrez_faixas
                            int attempts = 0;
                            do
                            {
                                newValue = random.Next(vidaMin, vidaMax + 1);
                                attempts++;
                            } while (attempts < maxAttempts &&
                                    ((x > 0 && level[y, x - 1] == newValue) ||
                                     (y > 0 && level[y - 1, x] == newValue)));

                            if (attempts >= maxAttempts)
                                newValue = (x + y) % 2 == 0 ? vidaMin : vidaMax;
                            break;

                        case 5: // xadrez
                            newValue = chessValues[(x + y) % 2];
                            break;

                        case 6: // gradiente
                            float progressG = (x + y) / maxDist;
                            newValue = Mathf.Clamp((int)Mathf.Round(vidaMin + (vidaMax - vidaMin) * progressG), vidaMin, vidaMax);
                            break;

                        case 7: // espiral
                            float dist = Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY);
                            newValue = Mathf.Clamp((int)Mathf.Round(vidaMin + (vidaMax - vidaMin) * (dist / maxDist)), vidaMin, vidaMax);
                            break;

                        case 8: // circular
                            float dx = x - centerX;
                            float dy = y - centerY;
                            float radius = Mathf.Sqrt(dx * dx + dy * dy);
                            newValue = Mathf.Clamp((int)Mathf.Round(vidaMin + (vidaMax - vidaMin) * (radius / maxRadius)), vidaMin, vidaMax);
                            break;
                    }

                    level[y, x] = newValue;
                }
            }
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
