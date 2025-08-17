using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class WeightedBuffEntry
{
    public Buff prefab;       // arraste o prefab do Buff/Debuff aqui
    public int weight = 1;    // peso relativo (>=1). 0 = desativado
    public bool isDebuff;     // meramente informativo, útil pra filtros/telemetria
}

public class BuffsManager : MonoBehaviour
{
    #region Singleton
    private static BuffsManager _instance;
    public static BuffsManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); }
        else { _instance = this; }
    }
    #endregion

    [Header("Drop global")]
    [Range(0, 100)] public float dropChance = 20f; // % única: se passa, escolhe ponderado

    [Header("Catálogo ponderado (Buffs e Debuffs)")]
    public List<WeightedBuffEntry> catalog = new List<WeightedBuffEntry>();

    //[Header("Sprites")]
    private List<Sprite> availableSprites; // pegue de AssetsManager se preferir

    // cache interno
    private System.Random rng => LevelGenerator.Instance.random; // usa o mesmo RNG do jogo
    private List<WeightedBuffEntry> _active;   // filtrado (weight>0 e prefab!=null)

    private void Start()
    {
        // Inicializa lista ativa e embaralha sprites por instância
        _active = catalog.Where(e => e != null && e.prefab != null && e.weight > 0).ToList();

        availableSprites = AssetsManager.Instance.buffsSprites.OrderBy(_ => rng.Next()).ToList();

        RandomizePrefabsSprites();
    }

    /// <summary>Associa sprites aleatórios aos prefabs, para não decorarem o ícone.</summary>
    public void RandomizePrefabsSprites()
    {
        if (_active == null || _active.Count == 0 || availableSprites == null || availableSprites.Count == 0) return;

        // Embaralha sprites e atribui em sequência (ciclo)
        var shuffled = availableSprites.OrderBy(_ => rng.Next()).ToList();
        int i = 0;

        foreach (var entry in _active)
        {
            var sr = entry.prefab.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = shuffled[i % shuffled.Count];
                i++;
            }
        }
    }

    /// <summary>Brick chama aqui: faz o roll global e, se passar, spawna um Buff ponderado no ponto.</summary>
    public void TrySpawnWeightedBuff(Vector3 position)
    {
        // 1) roll global (0..100)
        float roll = rng.Next(0, 10000) / 100f;
        if (roll > dropChance) return;

        // 2) escolhe ponderado
        var chosen = ChooseWeighted();
        if (chosen == null || chosen.prefab == null) return;

        Instantiate(chosen.prefab, position, Quaternion.identity);
    }

    /// <summary>Escolha ponderada por pesos (sem alocar listas novas).</summary>
    private WeightedBuffEntry ChooseWeighted()
    {
        if (_active == null || _active.Count == 0) return null;

        // soma dos pesos
        int total = 0;
        for (int i = 0; i < _active.Count; i++)
            total += _active[i].weight;

        if (total <= 0) return null;

        // rola 1..total
        int r = rng.Next(1, total + 1);
        int acc = 0;

        for (int i = 0; i < _active.Count; i++)
        {
            acc += _active[i].weight;
            if (r <= acc) return _active[i];
        }
        return _active[_active.Count - 1]; // fallback
    }
}