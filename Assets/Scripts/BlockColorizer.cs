using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BlockColorizer
{
    public enum BlockColorPattern
    {
        HorizontalStripes,
        VerticalStripes,
        Checkerboard,
        ConcentricSquares,
        ConcentricCircles,
        Diagonal45,
        Diagonal135,
        RadialSectors,
        PerlinIslands,
        LifeBands,
        SymmetryAware,   // colore por setores/quadrantes para realçar a simetria do level
        EdgeFalloff,     // gradiente a partir das bordas (ou do centro) para dar "volume"
        RowWave,         // ondas horizontais (seno) com fase/frequência sementeadas
        SeededShuffle   // “granulado” estável por célula, embaralhado pela seed/célula
    }

    /// <summary>
    /// Opção 1 (recomendada p/ performance): retorna um "resolvedor" de cor on-demand.
    /// Você chama resolver(row, col) na hora de instanciar cada tijolo, evitando alocar uma matriz grande.
    /// </summary>
    public static Func<int, int, Color32> MakeColorResolver(int paletteIndex, int[][] level, bool zerosAsClear = true, int symmetryHint = -1, int seedOverride = 0)
    {
        if (level == null || level.Length == 0)
            throw new ArgumentException("level inválido");

        // Dimensões (altura = número de linhas; largura = maior linha)
        int rows = level.Length;
        int cols = level.Max(r => r?.Length ?? 0);

        // Paleta
        var palette = AssetsManager.Instance.colorPalettes[paletteIndex];
        var allColors = palette.colors; // supondo Color[] internamente
        if (allColors == null || allColors.Length < 2)
            throw new ArgumentException("A paleta precisa ter pelo menos 2 cores (1+ indestrutível)");

        int destructibleCount = allColors.Length - 1;
        Color32 indestructible = allColors[allColors.Length - 1];
        // converte para Color32 para reduzir GC/memória
        Color32[] destructible = new Color32[destructibleCount];
        for (int i = 0; i < destructibleCount; i++)
            destructible[i] = allColors[i];

        // Fonte de random sementeada do seu LevelGenerator
        var rng = LevelGenerator.Instance.random;
        if (rng == null) rng = new System.Random();

        // Sorteia padrão
        var patterns = (BlockColorPattern[])Enum.GetValues(typeof(BlockColorPattern));
        var chosenPattern = patterns[rng.Next(patterns.Length)];

        // Alguns parâmetros aleatórios por padrão (espessura de faixa, offsets, escalas)
        int stripeWidth = Mathf.Max(1, rng.Next(1, 4));            // p/ listras/diagonais
        int sectorCount = Mathf.Max(3, rng.Next(4, 9));            // p/ radial
        float perlinScale = UnityEngine.Random.Range(0.08f, 0.18f); // usa UnityEngine.Random só p/ Perlin
        float perlinThreshold = UnityEngine.Random.Range(0.35f, 0.65f);

        // Centro geométrico
        float cx = (cols - 1) * 0.5f;
        float cy = (rows - 1) * 0.5f;

        // Helper: mapeia índice de "faixa" para cor
        Color32 ColorFromBand(int band)
        {
            if (destructibleCount == 0) return indestructible; // fallback
            int idx = band % destructibleCount;
            if (idx < 0) idx += destructibleCount;
            return destructible[idx];
        }

        // Resolve cor com base no padrão escolhido
        return (r, c) =>
        {
            // Trata posições fora do "cols" da linha jagged
            if (c < 0 || r < 0 || r >= rows) return Color.clear;
            int[] rowArr = level[r];
            if (rowArr == null || c >= rowArr.Length) return Color.clear;

            int cell = rowArr[c];
            if (cell == 0) return zerosAsClear ? new Color(0, 0, 0, 0) : new Color32(0, 0, 0, 255);
            if (cell < 0) return indestructible; // -1 indestrutível

            // célula > 0: destructible
            int band = 0;

            // ===== Sorteio ponderado do padrão conforme a simetria do nível =====
            var all = (BlockColorPattern[])System.Enum.GetValues(typeof(BlockColorPattern));

            // pesos base (1.0 cada)
            var weights = new Dictionary<BlockColorPattern, float>(all.Length);
            foreach (var p in all) weights[p] = 1f;

            // aplique viés conforme simetria (0=None, 1=H, 2=V, 3=Quad)
            int hint = (symmetryHint >= 0) ? symmetryHint : 0;
            switch (hint)
            {
                case 3: // Quad
                        // destaca padrões que ecoam a simetria/centro
                    if (weights.ContainsKey(BlockColorPattern.SymmetryAware)) weights[BlockColorPattern.SymmetryAware] *= 3.0f;
                    if (weights.ContainsKey(BlockColorPattern.EdgeFalloff)) weights[BlockColorPattern.EdgeFalloff] *= 1.6f;
                    break;
                case 1: // Horizontal
                case 2: // Vertical
                        // ondas e checker funcionam bem, SymmetryAware moderado
                    if (weights.ContainsKey(BlockColorPattern.RowWave)) weights[BlockColorPattern.RowWave] *= 2.2f;
                    if (weights.ContainsKey(BlockColorPattern.SymmetryAware)) weights[BlockColorPattern.SymmetryAware] *= 1.4f;
                    break;
                case 0: // None
                default:
                    // layouts menos regulares: granulado fica melhor
                    if (weights.ContainsKey(BlockColorPattern.SeededShuffle)) weights[BlockColorPattern.SeededShuffle] *= 2.4f;
                    break;
            }

            // sorteio ponderado determinístico
            float sum = 0f; foreach (var kv in weights) sum += kv.Value;
            float pick = (float)rng.NextDouble() * sum;
            BlockColorPattern chosenPattern = all[0];
            foreach (var kv in weights)
            {
                pick -= kv.Value;
                if (pick <= 0f) { chosenPattern = kv.Key; break; }
            }

            switch (chosenPattern)
            {
                case BlockColorPattern.HorizontalStripes:
                    band = (r / stripeWidth);
                    break;

                case BlockColorPattern.VerticalStripes:
                    band = (c / stripeWidth);
                    break;

                case BlockColorPattern.Checkerboard:
                    band = (r + c);
                    break;

                case BlockColorPattern.ConcentricSquares:
                    {
                        int dx = Mathf.Abs(c - Mathf.RoundToInt(cx));
                        int dy = Mathf.Abs(r - Mathf.RoundToInt(cy));
                        band = Mathf.Max(dx, dy);
                        break;
                    }

                case BlockColorPattern.ConcentricCircles:
                    {
                        float dx = c - cx;
                        float dy = r - cy;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        // Normaliza espessura de "anéis" em função do maior lado
                        float ringWidth = Mathf.Max(rows, cols) / 10f; // 10 anéis aprox
                        band = Mathf.FloorToInt(dist / Mathf.Max(1f, ringWidth));
                        break;
                    }

                case BlockColorPattern.Diagonal45:
                    band = ((r + c) / stripeWidth);
                    break;

                case BlockColorPattern.Diagonal135:
                    band = ((r - c) / stripeWidth);
                    break;

                case BlockColorPattern.RadialSectors:
                    {
                        float dx = c - cx;
                        float dy = r - cy;
                        float angle = Mathf.Atan2(dy, dx); // -pi..pi
                        float norm = (angle + Mathf.PI) / (2f * Mathf.PI); // 0..1
                        band = Mathf.FloorToInt(norm * sectorCount);
                        break;
                    }

                case BlockColorPattern.PerlinIslands:
                    {
                        // Perlin em coordenadas normalizadas
                        float u = (float)c / Mathf.Max(1, cols - 1);
                        float v = (float)r / Mathf.Max(1, rows - 1);
                        float n = Mathf.PerlinNoise(u / perlinScale, v / perlinScale);
                        // Discretiza em bandas, mas cria áreas sólidas
                        band = n > perlinThreshold ? 1 : 0;
                        // opcional: aumenta variedade usando col/linhas
                        band += ((r / stripeWidth) + (c / stripeWidth)) % 2;
                        break;
                    }

                case BlockColorPattern.LifeBands:
                default:
                    // Mapeia diretamente pela vida (1..N)
                    band = (cell - 1);
                    break;
                case BlockColorPattern.SymmetryAware:
                    {
                        int midR = rows / 2;
                        int midC = cols / 2;
                        int quad = (r < midR ? 0 : 2) + (c < midC ? 0 : 1); // 0..3
                        int lifeBand = Mathf.Max(0, cell - 1);             // 1->0, 2->1, 3->2...
                        band = quad + lifeBand;
                        break;
                    }

                case BlockColorPattern.EdgeFalloff:
                    {
                        int thisRowCols = rowArr.Length;
                        int top = r, left = c, bottom = rows - 1 - r, right = thisRowCols - 1 - c;
                        int distEdge = Mathf.Min(Mathf.Min(top, bottom), Mathf.Min(left, right)); // 0 = borda
                        int maxEdge = Mathf.Max(1, Mathf.Min(rows, thisRowCols) / 2);
                        float t = Mathf.Clamp01(1f - (distEdge / (float)maxEdge)); // 0..1 (centro = 1)

                        // meia chance de inverter (bordas claras x centro claro)
                        bool invert = rng.NextDouble() < 0.5;
                        if (invert) t = 1f - t;

                        int bandsAvail = Mathf.Max(1, destructibleCount - 1); // evitar tudo na mesma cor
                        int wave = Mathf.FloorToInt(t * bandsAvail);
                        int lifeBand = Mathf.Max(0, cell - 1);
                        band = lifeBand + wave;
                        break;
                    }

                case BlockColorPattern.RowWave:
                    {
                        // Ondas horizontais seedadas
                        int seed = (LevelGenerator.Instance != null) ? LevelGenerator.Instance.effectiveSeed : 0;
                        float baseFreq = 2f * Mathf.PI / Mathf.Max(4, rows);             // ~2 ondas no grid
                        float jitter = (Hash32(0, 0, seed) % 100) / 400f;                 // 0..0.25
                        float freq = baseFreq * (1f + jitter);
                        float phase = (Hash32(1, 2, seed) % 628) / 100f;                  // 0..6.28

                        float s = Mathf.Sin(r * freq + phase); // -1..1
                        float t = (s + 1f) * 0.5f;             // 0..1
                        int bandsAvail = Mathf.Max(1, destructibleCount - 1);
                        int wave = Mathf.FloorToInt(t * bandsAvail);
                        int lifeBand = Mathf.Max(0, cell - 1);
                        band = lifeBand + wave;
                        break;
                    }

                case BlockColorPattern.SeededShuffle:
                    {
                        // “granulado” estável por célula + vida
                        int seed = (LevelGenerator.Instance != null) ? LevelGenerator.Instance.effectiveSeed : 0;
                        int bandsAvail = Mathf.Max(1, destructibleCount);
                        int baseBand = Mathf.Max(0, cell - 1) % bandsAvail;
                        int h = Hash32(r, c, seed) % bandsAvail;
                        band = baseBand + h;
                        break;
                    }

            }

            return ColorFromBand(band);
        };
    }

    /// <summary>
    /// Resolver de cor para patterns marcadas com SpecialColor:
    /// - Cada valor distinto recebe uma cor fixa da paleta.
    /// - 1 e 2 compartilham a mesma cor (realça o desenho da pattern).
    /// - -1 usa sempre a última cor da paleta (indestrutível).
    /// </summary>
    public static Func<int, int, Color32> MakeSpecialPatternResolver(int paletteIndex, int[][] level, bool zerosAsClear = true)
    {
        if (level == null || level.Length == 0)
            throw new ArgumentException("level inválido");

        int rows = level.Length;
        int cols = level.Max(r => r?.Length ?? 0);

        var palette = AssetsManager.Instance.colorPalettes[paletteIndex];
        var allColors = palette.colors;
        if (allColors == null || allColors.Length < 2)
            throw new ArgumentException("A paleta precisa ter pelo menos 2 cores (1+ indestrutível)");

        // Última cor = indestrutível; demais = destrutíveis
        Color32 indestructible = allColors[allColors.Length - 1];
        Color32[] destructible = new Color32[Mathf.Max(1, allColors.Length - 1)];
        for (int i = 0; i < destructible.Length; i++) destructible[i] = allColors[i];

        // 1) Levantamos os valores distintos (>0) presentes no level
        //    e colapsamos 1/2 em uma mesma "classe".
        var classes = new SortedSet<int>();
        bool hasOneOrTwo = false;

        for (int r = 0; r < rows; r++)
        {
            var rowArr = level[r];
            if (rowArr == null) continue;
            for (int c = 0; c < rowArr.Length; c++)
            {
                int v = rowArr[c];
                if (v <= 0) continue; // ignora 0 e -1 aqui
                if (v == 1 || v == 2) { hasOneOrTwo = true; continue; }
                classes.Add(v);
            }
        }

        // 2) Monta uma tabela determinística valor -> índice de cor
        //    Primeiro a classe (1/2), depois os demais em ordem crescente.
        var valueToBand = new Dictionary<int, int>(32);
        int band = 0;

        if (hasOneOrTwo)
        {
            valueToBand[1] = band;
            valueToBand[2] = band;
            band++;
        }

        foreach (var v in classes) { valueToBand[v] = band; band++; }

        // 3) Função para buscar cor por "banda"
        Color32 ColorFromBand(int b)
        {
            if (destructible.Length == 0) return indestructible;
            int idx = b % destructible.Length;
            if (idx < 0) idx += destructible.Length;
            return destructible[idx];
        }

        // 4) Resolver
        return (r, c) =>
        {
            if (r < 0 || r >= rows || c < 0) return Color.clear;
            var rowArr = level[r];
            if (rowArr == null || c >= rowArr.Length) return new Color32(0, 0, 0, 0);

            int cell = rowArr[c];
            if (cell == 0) return zerosAsClear ? new Color32(0, 0, 0, 0) : new Color32(0, 0, 0, 255);
            if (cell < 0) return indestructible; // -1

            // 1/2 mesma cor; demais: cor fixa por valor
            if (cell == 1 || cell == 2) return ColorFromBand(valueToBand.ContainsKey(1) ? valueToBand[1] : 0);
            if (valueToBand.TryGetValue(cell, out int b)) return ColorFromBand(b);

            // Se aparecer um valor inesperado, cai para "LifeBands"
            int fallbackBand = cell - 1;
            return ColorFromBand(fallbackBand);
        };
    }

    // Mapeia vida -> "banda" base (para padrões que usam bandas por vida)
    private static int BaseBandFromValue(int v)
    {
        if (v <= 0) return -1;     // 0 ou -1 não contam (tratados à parte)
        if (v == 1) return 0;      // banda 0
        if (v == 2) return 1;      // banda 1
        return Math.Max(2, v - 1); // 3+ => 2,3,4...
    }

    // hash simples estável por célula + seed
    private static int Hash32(int r, int c, int seed)
    {
        unchecked
        {
            uint h = 0x811C9DC5u;
            h = (h ^ (uint)(r * 73856093)) * 16777619;
            h = (h ^ (uint)(c * 19349663)) * 16777619;
            h = (h ^ (uint)seed) * 16777619;
            return (int)(h & 0x7fffffff);
        }
    }

    // clamp seguro
    private static float Clamp01(float x) => x < 0 ? 0 : (x > 1 ? 1 : x);


    /// <summary>
    /// Opção 2: pré-calcula toda a grade de cores e retorna Color32[,].
    /// Útil se você realmente precisa consultar cores várias vezes depois.
    /// </summary>
    public static Color32[,] GenerateColorGrid(int paletteIndex, int[][] level, bool zerosAsClear = true)
    {
        var resolver = MakeColorResolver(paletteIndex, level, zerosAsClear);

        int rows = level.Length;
        int cols = level.Max(r => r?.Length ?? 0);
        var grid = new Color32[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            int thisRowCols = level[r]?.Length ?? 0;
            for (int c = 0; c < cols; c++)
            {
                grid[r, c] = (c < thisRowCols) ? resolver(r, c) : new Color(0, 0, 0, 0);
            }
        }
        return grid;
    }
}
