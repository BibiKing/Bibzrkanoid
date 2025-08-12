using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Brick : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    public int hitPoints = 1;
    public ParticleSystem destroyEffect;
    public ParticleSystem downgradeEffect;

    public static event Action<Brick> OnBrickDestruction;

    private void Awake()
    {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
        this.boxCollider = GetComponent<BoxCollider2D>();
        Ball.OnLightningBallEnable += OnLightningBallEnable;
        Ball.OnLightningBallDisable += OnLightningBallDisable;
    }

    private void OnLightningBallDisable(Ball obj)
    {
        if (this != null)
        {
            this.boxCollider.isTrigger = false;
        }
    }

    private void OnLightningBallEnable(Ball obj)
    {
        if(this != null)
        {
            this.boxCollider.isTrigger = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Ball ball = collision.gameObject.GetComponent<Ball>();
        ApplyCollisionLogic(ball);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ball") || collision.CompareTag("Projectile"))
        {
            Ball ball = collision.gameObject.GetComponent<Ball>();
            ApplyCollisionLogic(ball);
        }
    }

    private void ApplyCollisionLogic(Ball ball)
    {
        this.hitPoints--;

        if(this.hitPoints <= 0)
        {
            BricksManager.Instance.remainingBricks.Remove(this);
            OnBrickDestruction?.Invoke(this);
            OnBrickDestroy();
            SpawnEffect(destroyEffect);
            Destroy(this.gameObject);
        } else
        {
            SpawnEffect(downgradeEffect);
            this.spriteRenderer.sprite = BricksManager.Instance.sprites[this.hitPoints - 1];
        }
    }

    private void OnBrickDestroy()
    {
        float buffSpawnChance = LevelGenerator.Instance.random.Next(0, 10000)/100;
        float debuffSpawnChance = LevelGenerator.Instance.random.Next(0, 10000)/100;
        bool alreadySpawned = false;

        if(buffSpawnChance <= BuffsManager.Instance.buffChance)
        {
            alreadySpawned = true;
            Buff newBuff = this.spawnBuff(true);
        }

        if (debuffSpawnChance <= BuffsManager.Instance.debuffChance && !alreadySpawned)
        {
            alreadySpawned = true;
            Buff newDebuff = this.spawnBuff(false);
        }

    }

    private Buff spawnBuff(bool isBuff)
    {
        List<Buff> collection;

        if(isBuff)
        {
            collection = BuffsManager.Instance.availableBuffs;
        } else
        {
            collection = BuffsManager.Instance.availableDebuffs;
        }

        int buffIndex = LevelGenerator.Instance.random.Next(0, collection.Count);
        Buff prefab = collection[buffIndex];
        Buff newBuff = Instantiate(prefab, this.transform.position, Quaternion.identity) as Buff;

        return newBuff;
        
    }

    private void SpawnEffect(ParticleSystem particleEffect)
    {
        Vector3 brickPosition = gameObject.transform.position;
        Vector3 spawnPosition = new Vector3(brickPosition.x, brickPosition.y, brickPosition.z + 0.2f);
        GameObject effect = Instantiate(particleEffect.gameObject, spawnPosition, Quaternion.identity);

        MainModule mm = effect.GetComponent<ParticleSystem>().main;
        mm.startColor = this.spriteRenderer.color;
        Destroy(effect, particleEffect.main.startLifetime.constant);

    }

    public void Init(Transform containerTransform, Sprite sprite, Color color, int hitPoints)
    {
        this.transform.SetParent(containerTransform);
        this.spriteRenderer.sprite = sprite;
        this.spriteRenderer.color = color;
        this.hitPoints = hitPoints;
    }

    private void OnDisable()
    {
        Ball.OnLightningBallDisable -= OnLightningBallDisable;
        Ball.OnLightningBallEnable -= OnLightningBallEnable;
    }
}
