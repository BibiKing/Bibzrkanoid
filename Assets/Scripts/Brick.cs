using System;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class Brick : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public int hitPoints = 1;
    public ParticleSystem destroyEffect;
    public ParticleSystem downgradeEffect;

    public static event Action<Brick> OnBrickDestruction;

    private void Awake()
    {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Ball ball = collision.gameObject.GetComponent<Ball>();
        ApplyCollisionLogic(ball);
    }

    private void ApplyCollisionLogic(Ball ball)
    {
        this.hitPoints--;

        if(this.hitPoints <= 0)
        {
            BricksManager.Instance.remainingBricks.Remove(this);
            OnBrickDestruction?.Invoke(this);
            SpawnEffect(destroyEffect);
            Destroy(this.gameObject);
        } else
        {
            SpawnEffect(downgradeEffect);
            this.spriteRenderer.sprite = BricksManager.Instance.sprites[this.hitPoints - 1];
        }
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
}
