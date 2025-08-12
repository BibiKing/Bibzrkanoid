using System;
using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public bool isLightningBall;
    private SpriteRenderer spriteRenderer;

    public ParticleSystem lightningBallEffect;
    public float lightningBallDuration = 10;

    public static event Action<Ball> OnBallDeath;
    public static event Action<Ball> OnLightningBallEnable;
    public static event Action<Ball> OnLightningBallDisable;

    private void Awake()
    {
        this.spriteRenderer = GetComponentInChildren<SpriteRenderer>();

    }
    public void Die()
    {
        OnBallDeath?.Invoke(this);
        Destroy(gameObject, 1);
    }

    internal void StartLightningBall()
    {
        if (!this.isLightningBall) 
        { 
            this.isLightningBall = true;
            this.spriteRenderer.enabled = false;
            this.lightningBallEffect.gameObject.SetActive(true);
            StartCoroutine(StopLightningBallEffectAfterTime(this.lightningBallDuration));

            OnLightningBallEnable?.Invoke(this);
        }
    }

    private IEnumerator StopLightningBallEffectAfterTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        StopLightningBall();
    }

    private void StopLightningBall()
    {
        if (this.isLightningBall)
        {
            this.isLightningBall = false;
            this.spriteRenderer.enabled = true;
            this.lightningBallEffect.gameObject.SetActive(false);

            OnLightningBallDisable?.Invoke(this);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Ball"))
        {
            Physics2D.IgnoreCollision(collision.collider, this.GetComponent<CircleCollider2D>());
            Physics2D.IgnoreCollision(collision.collider, this.GetComponent<Collider2D>());
        }
    }
}
