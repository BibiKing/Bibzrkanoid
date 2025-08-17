using System;
using System.Collections;
using UnityEngine;
using UnityEngine.LightTransport;

public class Ball : MonoBehaviour
{
    public bool isLightningBall;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    [SerializeField] private string defaultLayerName = "Ball"; 
    [SerializeField] private string passthroughLayerName = "GhostBall"; 
    [SerializeField] private LayerMask brickLayer; 
    private int _defaultLayer;
    private int _passthroughLayer;
    private bool _passthroughActive;
    // buffer reusável pra evitar GC
    private RaycastHit2D[] _rayBuffer = new RaycastHit2D[8];

    private Vector2 _lastPos;

    public ParticleSystem lightningBallEffect;
    public float lightningBallDuration = 10;

    [SerializeField] private GameObject bounceEffectPrefab;
    // Ajustes do spawn
    [SerializeField] private float bounceFxOffset = 0.05f;     // empurra o spawn um pouquinho na direção da normal
    [SerializeField] private float bounceFxSnapDegrees = 22.5f; // desvio máximo para "grudar" nas 4 direções
    [SerializeField] private int bounceFxSortingOrderDelta = +1; // deixar o FX na frente/atrás do sprite
    // acima desta velocidade, não aplicamos snap (ângulo fica “livre”)
    [SerializeField] private float bounceFxNoSnapSpeed = 9f; // ajuste ao gosto

    [SerializeField] private float minAngleFromHorizontalDeg = 12f;
    [SerializeField] private float minAngleFromVerticalDeg = 5f;

    //[Header("Speed Bounds")]
    private float minSpeed = 3.5f;
    private float maxSpeed = 8.0f;

    [SerializeField] private BallTrailController trailController;

    public static event Action<Ball> OnBallDeath;
    public static event Action<Ball> OnLightningBallEnable;
    public static event Action<Ball> OnLightningBallDisable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        this.spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (trailController == null)
            trailController = GetComponent<BallTrailController>();

        _defaultLayer = LayerMask.NameToLayer(defaultLayerName);
        _passthroughLayer = LayerMask.NameToLayer(passthroughLayerName);
        if (_defaultLayer < 0) _defaultLayer = gameObject.layer;  // fallback
        if (_passthroughLayer < 0) _passthroughLayer = _defaultLayer;   // fallback

        minSpeed = BallsManager.Instance.minBallSpeed;
        maxSpeed = BallsManager.Instance.maxBallSpeed;
    }

    // (opcional mas útil)
    private void OnEnable()
    {
        _lastPos = rb ? rb.position : (Vector2)transform.position;
    }

    private void FixedUpdate()
    {
        // guarde posição anterior
        Vector2 currentPos = rb.position;

        if (_passthroughActive)
        {
            // traça do ponto anterior até o atual (pega todos os bricks atravessados no frame)
            Vector2 delta = currentPos - _lastPos;
            float dist = delta.magnitude;

            if (dist > 0f)
            {
                int hits = Physics2D.RaycastNonAlloc(_lastPos, delta.normalized, _rayBuffer, dist, brickLayer);
                for (int i = 0; i < hits; i++)
                {
                    var h = _rayBuffer[i];
                    var brick = h.collider ? h.collider.GetComponent<Brick>() : null;
                    if (brick != null)
                    {
                        // chame o método público do Brick que você já usa pra aplicar dano
                        // se não tiver, crie um: brick.ApplyHit(fromLightning:true);
                        brick.ApplyCollisionLogic(this);
                    }
                }
            }
        }

        _lastPos = currentPos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Ignore “colisão dupla” entre bolas/projéteis (mantém seu comportamento original)
        if (collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Projectile"))
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<CircleCollider2D>());
            return;
        }

        // --- EXCEÇÃO: no paddle, permite 90° (sem clamp vertical) ---
        bool hitPaddle = collision.collider.CompareTag("Paddle");
        if (hitPaddle)
            ClampVelocityAxes(rb, minAngleFromHorizontalDeg, 0f);               // só horizontal
        else
            ClampVelocityAxes(rb, minAngleFromHorizontalDeg, minAngleFromVerticalDeg); // horizontal + vertical

        EnforceSpeedBounds();

        // Spawna FX de colisão (se houver contato e prefab)
        if (bounceEffectPrefab != null && collision.contactCount > 0)
        {
            var contact = collision.GetContact(0);
            SpawnBounceEffect(contact.point, contact.normal);
        }

        // SQUASH de impacto
        if (collision.contactCount > 0)
        {
            var c = collision.GetContact(0);
            var squasher = GetComponent<BallSquashStretch>();
            if (squasher) squasher.OnImpact(c.normal);
        }
    }

    private void SpawnBounceEffect(Vector2 contactPoint, Vector2 normal)
    {
        if (bounceEffectPrefab == null) return;

        // 1) Posição levemente afastada do contato
        Vector3 spawnPos = (Vector3)contactPoint + (Vector3)(normal.normalized * bounceFxOffset);

        // 2) Ângulo base da normal: nosso prefab "atira para cima" ( +Y ), então -90°
        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
        float rotZ = angle - 90f;

        // 3) Snap condicional: só aplica se a bola NÃO estiver muito rápida
        float speed = rb ? rb.linearVelocity.magnitude : 0f;
        //float snapDeg = (speed >= bounceFxNoSnapSpeed) ? 0f : bounceFxSnapDegrees;
        // reduz snap de forma suave entre (noSnap*0.6 .. noSnap)
        float t = Mathf.InverseLerp(bounceFxNoSnapSpeed * 0.6f, bounceFxNoSnapSpeed, speed);
        float snapDeg = Mathf.Lerp(bounceFxSnapDegrees, 0f, Mathf.Clamp01(t));

        if (snapDeg > 0f)
        {
            // Snap para ↑ ↓ ← → se estiver próximo
            float[] cardinals = { 0f, 90f, 180f, -90f };
            float best = rotZ;
            float minDiff = 999f;
            foreach (var c in cardinals)
            {
                float d = Mathf.DeltaAngle(rotZ, c);
                float ad = Mathf.Abs(d);
                if (ad < minDiff) { minDiff = ad; best = c; }
            }
            if (minDiff <= snapDeg) rotZ = best;
        }

        // 4) Instanciar e tocar TODOS os PS (inclui filhos), independente de PlayOnAwake
        Quaternion rot = Quaternion.Euler(0f, 0f, rotZ);
        var go = Instantiate(bounceEffectPrefab.gameObject, spawnPos, rot);

        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            if (ps == null) continue;
            ps.Clear(true);
            ps.Play(true);

            var rnd = ps.GetComponent<ParticleSystemRenderer>();
            if (rnd != null && spriteRenderer != null)
            {
                rnd.sortingLayerID = spriteRenderer.sortingLayerID;
                rnd.sortingOrder = spriteRenderer.sortingOrder + bounceFxSortingOrderDelta;
            }
        }

        // 5) Limpeza robusta
        StartCoroutine(DestroyFxWhenDone(go, systems));
    }

    private IEnumerator DestroyFxWhenDone(GameObject root, ParticleSystem[] systems)
    {
        if (root == null) yield break;

        const float softTimeout = 6f; // espera "normal"
        const float hardTimeout = 1.5f;

        float t = 0f;
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

        // força parar emissão se ainda estiver vivo (pega loopers/sub-emitters)
        foreach (var ps in systems)
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // curto extra para morrer completamente
        float t2 = 0f;
        while (t2 < hardTimeout)
        {
            bool anyAlive = false;
            foreach (var ps in systems)
                if (ps != null && ps.IsAlive(true)) { anyAlive = true; break; }
            if (!anyAlive) break;

            t2 += Time.deltaTime;
            yield return null;
        }

        if (root != null) Destroy(root);
    }


    public void ConfigureSpeedBounds(float min, float max)
    {
        minSpeed = Mathf.Max(0.01f, min);
        maxSpeed = Mathf.Max(minSpeed, max);
    }

    private void EnforceSpeedBounds()
    {
        if (rb == null) return;

        var v = rb.linearVelocity;
        float speed = v.magnitude;

        // Anti-stuck: quase parado (apertada entre paddle/parede)
        if (speed < 0.05f)
        {
            // escolhe direção para cima (mantendo sinal original do X se existir)
            float signX = Mathf.Sign(v.x); if (signX == 0) signX = 1f;
            Vector2 dir = new Vector2(signX * Mathf.Cos(minAngleFromHorizontalDeg * Mathf.Deg2Rad),
                                      Mathf.Sin(minAngleFromHorizontalDeg * Mathf.Deg2Rad));
            rb.linearVelocity = dir.normalized * minSpeed;
            return;
        }

        // Clamp normal
        if (speed < minSpeed)
            rb.linearVelocity = v.normalized * minSpeed;
        else if (speed > maxSpeed)
            rb.linearVelocity = v.normalized * maxSpeed;
    }

    public void BeginTrailEmission() => trailController?.OnLaunch();
    public void PauseTrailEmission(bool clearNow = false)
    {
        if (clearNow) trailController?.OnRespawnReset();
        else trailController?.OnDie();
    }

    public void Die()
    {
        if (isLightningBall) OnLightningBallDisable?.Invoke(this);
        ApplyLightning(false);

        trailController?.OnDie(); // garante parar o rastro

        OnBallDeath?.Invoke(this);
        Destroy(gameObject, 1);
    }

    private void ApplyLightning(bool on)
    {
        isLightningBall = on;
        _passthroughActive = on;
        gameObject.layer = on ? _passthroughLayer : _defaultLayer;

         if (spriteRenderer) spriteRenderer.enabled = !on;
        if (lightningBallEffect) lightningBallEffect.gameObject.SetActive(on);
    }

    internal void StartLightningBall()
    {
        if (!this.isLightningBall) 
        {
            ApplyLightning(true);
            StartCoroutine(StopLightningBallEffectAfterTime(this.lightningBallDuration));

            trailController?.OnLightningEnable();   // <<< troca gradiente p/ lightning

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
            ApplyLightning(false);

            trailController?.OnLightningDisable();  // <<< volta gradiente normal

            OnLightningBallDisable?.Invoke(this);
        }
    }


    // Chame isto sempre que quiser garantir que não está "quase horizontal".
    public static void ClampVelocityAngle(Rigidbody2D body, float minAngleFromHorizontalDeg)
    {
        ClampVelocityAxes(body, minAngleFromHorizontalDeg, 0f);
    }
    /// <summary>
    /// Garante que a velocidade não esteja "quase horizontal" nem "quase vertical".
    /// - minHoriz: afastamento mínimo do eixo horizontal (0°/180°)
    /// - minVert : afastamento mínimo do eixo vertical   (90°/-90°)
    /// </summary>
    public static void ClampVelocityAxes(Rigidbody2D body, float minHorizDeg, float minVertDeg)
    {
        if (body == null) return;

        Vector2 v = body.linearVelocity;
        float speed = v.magnitude;
        if (speed < 0.001f) return;

        // Ângulo relativo ao eixo X, em graus (-180..180)
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        // Distância dos eixos, em [0..90]
        // Horizontal: distância até 0°/±180°
        float absFromHoriz = Mathf.Abs(angle);
        if (absFromHoriz > 90f) absFromHoriz = 180f - absFromHoriz;

        // Vertical: distância até ±90°
        float absFromVert = Mathf.Abs(90f - Mathf.Abs(angle));

        // Sinais originais (preservam quadrante)
        float signX = Mathf.Sign(v.x); if (signX == 0) signX = 1f;
        float signY = Mathf.Sign(v.y); if (signY == 0) signY = 1f;

        // Se está muito perto do horizontal → empurra para minHoriz
        if (absFromHoriz < minHorizDeg)
        {
            float a = minHorizDeg * Mathf.Deg2Rad; // ângulo a partir do horizontal
            Vector2 dir = new Vector2(signX * Mathf.Cos(a), signY * Mathf.Sin(a));
            body.linearVelocity = dir.normalized * speed;
            return;
        }

        // Se está muito perto do vertical → empurra para (90 - minVert) a partir do horizontal
        if (absFromVert < minVertDeg)
        {
            float a = (90f - minVertDeg) * Mathf.Deg2Rad; // afastamento do horizontal
            Vector2 dir = new Vector2(signX * Mathf.Cos(a), signY * Mathf.Sin(a));
            body.linearVelocity = dir.normalized * speed;
        }
    }

    private void OnDisable()
    {
        // blindagem para pooling/disable
        ApplyLightning(false);
    }

}
