using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

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

        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rigidBody = GetComponent<Rigidbody2D>();
    }

    #endregion

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rigidBody;

    [Header("Shooting Mechanic")]
    [SerializeField]
    public bool PaddleIsShooting { get; set; }
    public float fireCooldown = 0.5f;
    public float shootingDuration = 10f;
    public GameObject rightMuzzle;
    public GameObject leftMuzzle;
    public Projectile bulletPrefab;
    private Coroutine shootingCo;

    [Header("Paddle Movement")]
    [SerializeField]  private float maxBounceAngle = 75f;
    private float movement;
    

    [Header("Sizing Mechanic")]
    [SerializeField]
    public float extendShrinkDuration = 10f;
    public float defaultPaddleWidth = 0.5f;
    public float defaultPaddleHeight = 0.1f;
    public float sizingSpeed = 1.0f;

    [Header("Accel Movement")]
    [Tooltip("Velocidade para micro-ajustes (início do movimento)")]
    [SerializeField] private float fineSpeed = 4f;

    [Tooltip("Velocidade quando totalmente acelerado (segurando por um tempo)")]
    [SerializeField] private float fastSpeed = 11f;

    [Tooltip("Segundos segurando a tecla/analógico para atingir fastSpeed")]
    [SerializeField] private float timeToMax = 0.6f;

    [Tooltip("Tempo para perder a aceleração quando solta/inverte direção")]
    [SerializeField] private float releaseFalloff = 0.25f;

    [Tooltip("Curva da aceleração (0=sem aceleração; 1=aceleração máxima).")]
    [SerializeField] private AnimationCurve accelCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // --- estado interno ---
    private float holdTimer = 0f;   // quanto tempo está segurando nesta direção
    private int lastDir = 0;        // -1, 0, +1

    private void Start()
    {

    }
    void Update()
    {
        PaddleMovement();
        if (PaddleIsShooting)
        {
            UpdateMuzzlePosition();
        }
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

        // Entrada do Input System: teclado = -1/0/1 ; analógico = contínuo [-1..1]
        float input = movement;
        int dir = Mathf.Approximately(input, 0f) ? 0 : (input > 0f ? +1 : -1);

        // cronômetro de aceleração por direção
        if (dir == 0)
        {
            // Soltou: decai o "boost"
            if (releaseFalloff > 0f)
                holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime / releaseFalloff);
            else
                holdTimer = 0f;
        }
        else
        {
            // Inverteu direção? zera o acúmulo para não "derrapar" acelerado
            if (dir != lastDir) holdTimer = 0f;

            // Cresce mais rápido com analógico mais forte; teclado = 1
            float grow = Mathf.Abs(input);
            if (timeToMax > 0f) holdTimer = Mathf.Min(timeToMax, holdTimer + Time.deltaTime * grow);
            else holdTimer = 1f;
        }
        lastDir = dir;

        // 0..1 conforme o tempo segurando, com curva para sensibilidade
        float accel01 = (timeToMax > 0f) ? Mathf.Clamp01(holdTimer / timeToMax) : 1f;
        accel01 = accelCurve != null ? accelCurve.Evaluate(accel01) : accel01;

        // Velocidade alvo: começa em fineSpeed e sobe até fastSpeed
        float currentSpeed = Mathf.Lerp(fineSpeed, fastSpeed, accel01);

        // Aplicar movimento (escala pela força do input: analógico controla fino naturalmente)
        transform.Translate(input * currentSpeed * Time.deltaTime, 0f, 0f);

        // Clamp de bordas (mantém o seu)
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, leftClamp + paddleWidth * 0.5f, rightClamp - paddleWidth * 0.5f);
        transform.position = pos;

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Ball"))
        {

            Rigidbody2D rb = collision.rigidbody;
            float speed = rb.linearVelocity.magnitude;

            // ponto de contato e centro do paddle
            Vector2 hit = collision.GetContact(0).point;
            float half = spriteRenderer.bounds.size.x * 0.5f;

            // posição relativa [-1..1] (esquerda..direita)
            float t = Mathf.Clamp((hit.x - transform.position.x) / half, -1f, 1f);

            // mapeia t para ângulo em torno do eixo Y (para cima):
            // t>0 (lado direito) vai inclinar para a direita
            float angle = -t * maxBounceAngle;              // ex.: maxBounceAngle = 65~75
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;

            // garante que a bola SEMPRE vá pra cima após bater no paddle
            if (dir.y <= 0f) dir.y = Mathf.Abs(dir.y);

            // aplica a nova direção com a MESMA velocidade
            rb.linearVelocity = dir.normalized * speed;

            // clamp extra: evita sair quase horizontal
            Ball.ClampVelocityAngle(rb, 12f); // 12° de mínimo em relação ao eixo horizontal

        }
    }

    private void UpdateMuzzlePosition()
    {
        // metade da largura REAL do sprite, com escala aplicada (em espaço de mundo)
        float halfWidthWorld = spriteRenderer.bounds.extents.x;
        float offsetWorld = 0.05f; //pra não pegar na borda

        float halfWidthLocal = halfWidthWorld / transform.lossyScale.x;
        float offsetLocal = offsetWorld / transform.lossyScale.x;

        float zL = leftMuzzle.transform.localPosition.z;
        float zR = rightMuzzle.transform.localPosition.z;

        leftMuzzle.transform.localPosition = new Vector3(-halfWidthLocal + offsetLocal, 0f, zL);
        rightMuzzle.transform.localPosition = new Vector3(+halfWidthLocal - offsetLocal, 0f, zR);


    }

    public void StartWidthAnimation(float width, bool isReset = false)
    {
        StartCoroutine(AnimatePaddleWidth(width, isReset));
    }

    public IEnumerator AnimatePaddleWidth(float width, bool isReset)
    {
        this.PaddleIsTransforming = true;
        if (!isReset)
            StartCoroutine(ResetPaddleWidthAfterTime(this.extendShrinkDuration));

        //largura atual em mundo
        float currentWidthWorld = spriteRenderer.bounds.size.x;

        // base local (do sprite, sem escala)
        float baseLocalWidth = spriteRenderer.sprite.bounds.size.x;

        while (!Mathf.Approximately(currentWidthWorld, width))
        {
            currentWidthWorld = Mathf.MoveTowards(currentWidthWorld, width, sizingSpeed * Time.deltaTime);

            // escala X necessária para atingir a largura em mundo desejada
            float scaleX = currentWidthWorld / baseLocalWidth;
            this.spriteRenderer.size = new Vector2(scaleX, defaultPaddleHeight);
            this.boxCollider.size = new Vector2(scaleX, defaultPaddleHeight);

            yield return null;
        }

        PaddleIsTransforming = false;
    }

    private IEnumerator ResetPaddleWidthAfterTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);  
        this.StartWidthAnimation(this.defaultPaddleWidth, true);
    }

    private void ResetPaddleWidthInstant()
    {
        this.spriteRenderer.size = new Vector2(defaultPaddleWidth, defaultPaddleHeight);
        this.boxCollider.size = new Vector2(defaultPaddleWidth, defaultPaddleHeight);
    }

    internal void StartShooting()
    {
        if (!this.PaddleIsShooting)
        {
            this.PaddleIsShooting = true;
            shootingCo = StartCoroutine(StartShootingRoutine());
        }
    }

    internal void StopShooting()
    {
        if(shootingCo != null)
            StopCoroutine(shootingCo);
        this.PaddleIsShooting = false;
        leftMuzzle.SetActive(false);
        rightMuzzle.SetActive(false);
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

        StopShooting();
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

    internal void ClearAllEffectsAndResetState()
    {
        ResetPaddleWidthInstant();
        StopShooting();
    }

    internal void ResetToDefaultPoseAndParams()
    {
        transform.position = new Vector3(0, transform.position.y);
    }
}
