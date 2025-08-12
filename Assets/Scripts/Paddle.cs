using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Paddle : MonoBehaviour
{
    #region Singleton

    private static Paddle _instance;

    public static Paddle Instance => _instance;

    public bool PaddleIsTransforming { get; set; }

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

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    [Header("Shooting Mechanic")]
    [SerializeField]
    public bool PaddleIsShooting { get; set; }
    public float fireCooldown = 0.5f;
    public float shootingDuration = 10f;
    public GameObject rightMuzzle;
    public GameObject leftMuzzle;
    public Projectile bulletPrefab; 

    [Header("Paddle Movement")]
    [Tooltip("Maximum movement speed of paddle")]
    [SerializeField]    
    private float paddleSpeed = 5.0f;
    private float movement;

    [Header("Sizing Mechanic")]
    [SerializeField]
    public float extendShrinkDuration = 10f;
    public float defaultPaddleWidth = 0.5f;
    public float defaultPaddleHeight = 0.1f;

    private void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();        
    }
    void Update()
    {
        PaddleMovement();
        if (PaddleIsShooting)
        {
            UpdateMuzzlePosition();
        }
    }

    private void UpdateMuzzlePosition()
    {
        // metade da largura REAL do sprite, com escala aplicada (em espaço de mundo)
        float halfWidth = spriteRenderer.bounds.extents.x;
        float size = spriteRenderer.size.x;

        // desconta a borda
        float rightEdge = Mathf.Max(0f, halfWidth - size - Mathf.Max(0f, 0.05f));
        float leftEdge = Mathf.Max(0f, halfWidth - size - Mathf.Max(0f, 0.1f));

        // Espaço local: simples e imune a escalas dos pais
        leftMuzzle.transform.localPosition = new Vector3(-leftEdge, 0f, leftMuzzle.transform.localPosition.z);
        rightMuzzle.transform.localPosition = new Vector3(+rightEdge, 0f, rightMuzzle.transform.localPosition.z);

    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movement = context.ReadValue<float>();
    }

    private void PaddleMovement()
    {
        float frameWidth = 0.15f;
        float leftClamp = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0)).x + frameWidth;
        float rightClamp = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x - frameWidth;
        float paddleWidth = spriteRenderer.bounds.size.x;

        transform.Translate(movement * paddleSpeed * Time.deltaTime, 0, 0);

        Vector3 position = transform.position;

        position.x = Mathf.Clamp(position.x, leftClamp + paddleWidth/2, rightClamp - paddleWidth / 2);

        transform.position = position;

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Ball")
        {
            Rigidbody2D ballRb = collision.gameObject.GetComponent<Rigidbody2D>();
            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 paddleCenter = new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y);

            ballRb.linearVelocity = Vector2.zero;

            float difference = paddleCenter.x - hitPoint.x;

            if (hitPoint.x < paddleCenter.x)
            {
                ballRb.AddForce(new Vector2(-(Mathf.Abs(difference * 200)),BallsManager.Instance.initialBallSpeed));
            } else
            {
                ballRb.AddForce(new Vector2(Mathf.Abs(difference * 200), BallsManager.Instance.initialBallSpeed));
            }
        }
    }

    public void StartWidthAnimation(float width, bool isReset = false)
    {
        if (isReset = false && width != defaultPaddleWidth)
        {
            return;
        }
        StartCoroutine(AnimatePaddleWidth(width, isReset));
    }

    public IEnumerator AnimatePaddleWidth(float width, bool isReset)
    {
        this.PaddleIsTransforming = true;
        if (!isReset)
        {
            this.StartCoroutine(ResetPaddleWidthAfterTime(this.extendShrinkDuration));
        }

        float currentWidth = this.spriteRenderer.size.x;

        if (width > currentWidth)
        {
            
            while(currentWidth < width)
            {
                currentWidth += Time.deltaTime * 1.0f;
                if(currentWidth > width)
                {
                    currentWidth = width;
                }
                this.spriteRenderer.size = new Vector2(currentWidth, defaultPaddleHeight);
                this.boxCollider.size = new Vector2(currentWidth, defaultPaddleHeight);
                yield return null;
            }
        } else
        {
            while (currentWidth > width)
            {
                currentWidth -= Time.deltaTime * 1f;
                if (currentWidth < width)
                {
                    currentWidth = width;
                }
                this.spriteRenderer.size = new Vector2(currentWidth, defaultPaddleHeight);
                this.boxCollider.size = new Vector2(currentWidth, defaultPaddleHeight);
                yield return null;
            }
        }

        this.PaddleIsTransforming = false;
    }

    private IEnumerator ResetPaddleWidthAfterTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);  
        this.StartWidthAnimation(this.defaultPaddleWidth, true);
    }

    internal void StartShooting()
    {
        if (!this.PaddleIsShooting)
        {
            this.PaddleIsShooting = true;
            StartCoroutine(StartShootingRoutine());
        }
    }

    public IEnumerator StartShootingRoutine()
    {
        float fireCooldownLeft = 0;
        float shootingDurationLeft = this.shootingDuration;

        while(shootingDurationLeft >= 0)
        {
            fireCooldownLeft -= Time.deltaTime;
            shootingDurationLeft -= Time.deltaTime;

            if(fireCooldownLeft <= 0)
            {
                UpdateMuzzlePosition();
                this.Shoot();
                fireCooldownLeft = this.fireCooldown;
            }

            yield return null;
        } 

        this.PaddleIsShooting = false;
        leftMuzzle.SetActive(false);
        rightMuzzle.SetActive(false);
    }

    private void Shoot()
    {
        leftMuzzle.SetActive(false);
        rightMuzzle.SetActive(false);

        leftMuzzle.SetActive(true);
        rightMuzzle.SetActive(true);

        this.SpawnBullet(leftMuzzle);
        this.SpawnBullet(rightMuzzle);

    }

    private void SpawnBullet(GameObject muzzle)
    {
        Vector3 spawnPosition = new Vector3(muzzle.transform.position.x, muzzle.transform.position.y + 0.5f, muzzle.transform.position.z);
        Projectile bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        Rigidbody2D bulletRigidBody = bullet.GetComponent<Rigidbody2D>();
        bulletRigidBody.AddForce(new Vector2(0, 450f));
    }
}
