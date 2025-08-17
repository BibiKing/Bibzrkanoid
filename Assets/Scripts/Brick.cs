using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Brick : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    [SerializeField] private string brickLayerName = "Bricks";
    [SerializeField] private string indestructableBrickLayerName = "Indestructable";
    private int _brickLayer;
    private int _indestructableBrickLayer;

    public int hitPoints = 1;
    public ParticleSystem destroyEffect;
    public ParticleSystem downgradeEffect;
    public ParticleSystem unbreakableEffect;

    private int points {get; set;}
    private bool isUnbreakable = false;

    private Sprite regularBrickSprite;
    private Sprite hardBrickSprite;
    private Sprite indestructibleBrickSprite;

    public static event Action<Brick> OnBrickDestruction;

    private void Awake()
    {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
        this.boxCollider = GetComponent<BoxCollider2D>();

        regularBrickSprite = AssetsManager.Instance.regularBrickSprite;
        hardBrickSprite = AssetsManager.Instance.hardBrickSprite;
        indestructibleBrickSprite = AssetsManager.Instance.indestructibleBrickSprite;

        _brickLayer = LayerMask.NameToLayer(brickLayerName);
        _indestructableBrickLayer = LayerMask.NameToLayer(indestructableBrickLayerName);
        if (_brickLayer < 0) _brickLayer = gameObject.layer;  // fallback
        if (_indestructableBrickLayer < 0) _indestructableBrickLayer = _brickLayer;   // fallback

    }
    private void Start()
    {
        if(hitPoints >= 3)
            points += points * hitPoints;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Ball ball = collision.gameObject.GetComponent<Ball>();
        ApplyCollisionLogic(ball);
    }

    public void ApplyCollisionLogic(Ball ball)
    {
        if (isUnbreakable)
        {
            SpawnEffect(unbreakableEffect);
            return;
        }

        this.hitPoints--;

        if(this.hitPoints <= 0)
        {
            ScoreTextPopup.Create(this.transform.position, points);
            BricksManager.Instance.remainingBricks.Remove(this);
            OnBrickDestruction?.Invoke(this);
            OnBrickDestroy();
            SpawnEffect(destroyEffect, true);
            Destroy(this.gameObject);
        } else
        {
            SpawnEffect(downgradeEffect, true);
        }

        
    }

    private void OnBrickDestroy()
    {
        // Delega tudo ao BuffsManager (única chance + peso)
        BuffsManager.Instance.TrySpawnWeightedBuff(this.transform.position);

    }

    private void SpawnEffect(ParticleSystem effectPrefab, bool ajustColor = false)
    {
        if (effectPrefab == null) return;

        Vector3 p = transform.position + new Vector3(0f, 0f, -0.2f);
        // instancia o ROOT do efeito
        var instance = Instantiate(effectPrefab.gameObject, p, Quaternion.identity);
        Destroy(instance, 12f);

        // pega TODOS os ParticleSystems (inclui filhos/sub-emitters)
        var systems = instance.GetComponentsInChildren<ParticleSystem>(true);

        var brickSR = spriteRenderer; // SpriteRenderer do tijolo

        foreach (var ps in systems)
        {
            if (ps == null) continue;

            if (ajustColor)
            {
                // 1) Cor: usa a cor do tijolo (força alfa=1 para garantir visibilidade)
                var c = brickSR != null ? brickSR.color : Color.white;
                c.a = 1f;
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(c);
            }

            // 2) Sorting igual ao do tijolo (evita “sumir atrás”)
            var rnd = ps.GetComponent<ParticleSystemRenderer>();
            if (rnd != null && brickSR != null)
            {
                rnd.sortingLayerID = brickSR.sortingLayerID;
                rnd.sortingOrder = brickSR.sortingOrder + 1; // ou -1, conforme seu gosto
            }

            // 3) Garante tocar mesmo se Play On Awake estiver off
            ps.Clear(true);
            ps.Play(true);
        }

        // 4) Destrói quando TODOS terminarem (inclui sub-emitters, bursts e curvas)
        StartCoroutine(DestroyEffectWhenDone(instance, systems));
    }

    private IEnumerator DestroyEffectWhenDone(GameObject root, ParticleSystem[] systems)
    {
        if (root == null) yield break;

        const float softTimeout = 8f;  // espera "normal"
        const float hardTimeout = 2f;  // após StopEmitting, tempo extra para morrer

        float t = 0f;
        // 1) Espera normal até todo mundo morrer
        while (t < softTimeout)
        {
            bool anyAlive = false;
            foreach (var ps in systems)
            {
                if (ps != null && ps.IsAlive(true)) { anyAlive = true; break; }
            }
            if (!anyAlive) break;

            t += Time.deltaTime;
            yield return null;
        }

        // 2) Se ainda tem alguém vivo, força parar emissão (pega loopers/sub-emitters)
        if (systems != null)
        {
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                // Para de EMITIR mas deixa as partículas existentes sumirem naturalmente
                ps.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // 3) Espera curto para as remanescentes sumirem
        float t2 = 0f;
        while (t2 < hardTimeout)
        {
            bool anyAlive = false;
            foreach (var ps in systems)
            {
                if (ps != null && ps.IsAlive(true)) { anyAlive = true; break; }
            }
            if (!anyAlive) break;

            t2 += Time.deltaTime;
            yield return null;
        }

        // 4) Destrói o GO raiz (não apenas o componente)
        Destroy(root);
    }

    public void Init(Transform containerTransform, Color color, int hitPoints, bool isIndest = false, int points = 50)
    {
        this.transform.SetParent(containerTransform);
        Sprite sprite = null;
        if (hitPoints < 0)
        {
            sprite = indestructibleBrickSprite;
            gameObject.layer = _indestructableBrickLayer;
        } else if (hitPoints <=2)
        {
            sprite = regularBrickSprite;
            gameObject.layer = _brickLayer;
        } else if(hitPoints > 2) 
        { 
            sprite = hardBrickSprite;
            gameObject.layer = _brickLayer;
        }

        this.spriteRenderer.sprite = sprite;
        this.spriteRenderer.color = color;
        this.hitPoints = hitPoints;
        this.isUnbreakable = isIndest;
        this.points = points;
    }

}
