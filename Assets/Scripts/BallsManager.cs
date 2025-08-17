using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallsManager : MonoBehaviour
{
    #region Singleton

    private static BallsManager _instance;

    public static BallsManager Instance => _instance;

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

    private Ball ballPrefab;
    private Ball initialBall;
    private Rigidbody2D initialBallRb;

    //public float initialBallSpeed = 250;
    
    public List<Ball> balls {  get; private set; }
    public int maxBallsSpawned;

    public enum LaunchMethod { Velocity, AddForceImpulse }

    [Header("Aim Settings")]
    public bool showAimIndicator = true;
    [Range(10f, 85f)] public float maxAimAngleDeg = 70f; // limites de lançamento em torno do vertical
    public float aimOscillationPeriod = 2f;            // segundos para ir e voltar
    public float aimLength = 1f;                       // tamanho da seta/linha
    public bool invertYOnLaunch = false;                 // marque true se seu “cima” for -Y
    public float lineWidth = 0.06f;                     // mais grosso para URP 2D

    [Header("Launch")]
    public LaunchMethod launchMethod = LaunchMethod.Velocity;
    public float initialBallSpeed = 6.0f;        // alvo de velocidade inicial
    public float extraSpeedClamp = 8.0f;         // teto (pós-lançamento)
    public bool enforceMinAngleAfterLaunch = true;
    public float minAngleFromHorizontalDeg = 12f;

    [Header("Ball Speed Limits")]
    public float minBallSpeed = 3.5f;
    public float maxBallSpeed = 8.0f; // pode casar com seu extraSpeedClamp

    [Header("2D Physics Safety")]
    public bool forceContinuousCollision = true;  // evita atravessar coisas
    public bool forceInterpolate = true;          // movimento visual mais suave

    [Header("Launch – Separation & Safety")]
    public float launchSeparation = 0.25f;     // empurra a bola para frente na direção do disparo
    public float ignorePaddleCollisionSeconds = 0.10f; // tempo ignorando colisão com o paddle

    [Header("Multiball")]
    [Range(0f, 45f)] public float multiBallSpreadDeg = 15f;   // espalhamento em torno da direção base
    public bool multiBallMatchReferenceSpeed = true;          // usa a velocidade da bola atual
    public float multiBallMinSpeed = 6f;                      // fallback se a ref estiver lenta


    // privados
    private LineRenderer aimLine;

    private void Start()
    {
        ballPrefab = AssetsManager.Instance.ballPrefab;
        InitBall();

        CreateAimLine();
        UpdateAimLine();
        EnsureRBSettings(initialBallRb);
    }

    private void OnEnable()
    {
        Ball.OnBallDeath += HandleBallDeath; // <<< NOVO
    }

    private void OnDisable()
    {
        Ball.OnBallDeath -= HandleBallDeath; // <<< NOVO
    }

    private void HandleBallDeath(Ball b)
    {
        // remove referência (Destroy atrasado em 1s, mas já podemos tirar da lista)
        if (balls != null) balls.Remove(b);

        // se zerou bolas ativas, é fim de rodada: pare o shooting
        if (balls == null || balls.Count == 0)
        {
            if (Paddle.Instance != null) Paddle.Instance.StopShooting();
            // aqui você também pode sinalizar fim de round se quiser
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.IsGameStarted)
        {
            initialBall.transform.position = StartingBallPosition();

            if (aimLine == null) CreateAimLine();
            UpdateAimLine();
        }
        else
        {
            HideAimLine();
        }
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        // garante 1 disparo por clique (Input System)
        if (!context.performed) return;

        if (!GameManager.Instance.IsGameStarted)
        {
            initialBallRb.bodyType = RigidbodyType2D.Dynamic;                     

            Vector2 dir = CurrentAimDirection();

            // 1) posiciona a bola um pouco ADIANTE do paddle na direção de disparo
            Vector3 safePos = StartingBallPosition() + (Vector3)(dir.normalized * launchSeparation);
            initialBall.transform.position = safePos;

            // 2) libera a física e ignora colisão com o paddle por alguns ms
            initialBallRb.bodyType = RigidbodyType2D.Dynamic;
            StartCoroutine(IgnorePaddleCollisionTemporarily(ignorePaddleCollisionSeconds));

            // 3) aplica o lançamento (Velocity ou AddForce, como você já configurou)
            LaunchBall(initialBallRb, dir);

            initialBall.BeginTrailEmission();

            GameManager.Instance.IsGameStarted = true;
            HideAimLine();
        }

    }

    private void LaunchBall(Rigidbody2D rb, Vector2 dir)
    {
        if (rb == null) return;
        EnsureRBSettings(rb);

        switch (launchMethod)
        {
            case LaunchMethod.Velocity:
                rb.linearVelocity = dir * initialBallSpeed;
                break;

            case LaunchMethod.AddForceImpulse:
                {
                    // Impulso instantâneo: Δv = Impulse / mass  =>  Impulse = mass * targetSpeed
                    float mass = Mathf.Max(0.0001f, rb.mass);
                    Vector2 impulse = dir * (initialBallSpeed * mass);
                    rb.AddForce(impulse, ForceMode2D.Impulse);
                    break;
                }
        }

        // Clamp mínimo de ângulo e teto de velocidade logo após lançar
        if (enforceMinAngleAfterLaunch)
            Ball.ClampVelocityAngle(rb, minAngleFromHorizontalDeg);

        float speed = rb.linearVelocity.magnitude;
        if (speed > extraSpeedClamp)
            rb.linearVelocity = rb.linearVelocity.normalized * extraSpeedClamp;
    }

    private void LaunchBall(Rigidbody2D rb, Vector2 dir, float targetSpeed)
    {
        if (rb == null) return;
        EnsureRBSettings(rb);

        switch (launchMethod)
        {
            case LaunchMethod.Velocity:
                rb.linearVelocity = dir * targetSpeed;
                break;

            case LaunchMethod.AddForceImpulse:
                {
                    float mass = Mathf.Max(0.0001f, rb.mass);
                    Vector2 impulse = dir * (targetSpeed * mass);
                    rb.AddForce(impulse, ForceMode2D.Impulse);
                    break;
                }
        }

        if (enforceMinAngleAfterLaunch)
            Ball.ClampVelocityAngle(rb, minAngleFromHorizontalDeg);

        // respeita seu teto logo após spawn
        float speed = rb.linearVelocity.magnitude;
        if (speed > extraSpeedClamp)
            rb.linearVelocity = rb.linearVelocity.normalized * extraSpeedClamp;
    }

    private void InitBall()
    {
        initialBall = Instantiate(ballPrefab, StartingBallPosition(), Quaternion.identity);
        initialBallRb = initialBall.GetComponent<Rigidbody2D>();

        // Garante que a bola fique “presa” ao paddle até o clique
        initialBallRb.bodyType = RigidbodyType2D.Kinematic;
        initialBallRb.linearVelocity = Vector2.zero;
        initialBallRb.angularVelocity = 0f;
        initialBallRb.gravityScale = 0f; // se usar gravidade no projeto

        initialBall.ConfigureSpeedBounds(minBallSpeed, maxBallSpeed);
        // >>>trail desligado/limpo enquanto não foi lançada <<<
        initialBall.PauseTrailEmission(clearNow: true);

        this.balls = this.balls ?? new List<Ball>();
        this.balls.Add(initialBall);
    }

    private Vector3 StartingBallPosition()
    {
        float ballRadius = ballPrefab.GetComponentInChildren<SpriteRenderer>().bounds.size.y;
        float paddleHeight = Paddle.Instance.gameObject.GetComponent<SpriteRenderer>().bounds.size.y;
        float offset = (ballRadius / 2) + (paddleHeight / 2);

        Vector3 paddlePosition = Paddle.Instance.gameObject.transform.position;
        return new Vector3(paddlePosition.x, paddlePosition.y + offset, 0);
    }

    public void ResetBalls()
    {
        foreach (var ball in this.balls.ToList())
        {
            if (ball != null) Destroy(ball.gameObject);
            
        }

        // Limpa a lista e cria uma única bola inicial
        this.balls.Clear();

        if (Paddle.Instance != null) Paddle.Instance.ClearAllEffectsAndResetState();

        InitBall();

        EnsureRBSettings(initialBallRb);
        if (aimLine == null) CreateAimLine();

        UpdateAimLine();
    }

    public void SpawnBalls(Vector3 position, int count, bool isLightningBall)
    {
        // bola de referência: pega uma bola ativa, de preferência a mais rápida
        Rigidbody2D refRb = null;
        float refSpeed = 0f;

        foreach (var b in this.balls)
        {
            if (b == null) continue;
            var rb = b.GetComponent<Rigidbody2D>();
            if (rb == null) continue;
            float s = rb.linearVelocity.magnitude;
            if (s > refSpeed) { refSpeed = s; refRb = rb; }
        }

        // direção base: a direção da bola de ref; se não houver, usa sua base de lançamento
        Vector2 baseDir;
        if (refRb != null && refSpeed > 0.05f)
            baseDir = refRb.linearVelocity.normalized;
        else
            baseDir = invertYOnLaunch ? Vector2.down : Vector2.up;

        // velocidade alvo: igual à bola de referência, ou a velocidade inicial
        float targetSpeed = multiBallMatchReferenceSpeed
            ? Mathf.Max(multiBallMinSpeed, refSpeed, initialBallSpeed)
            : initialBallSpeed;

        for (int i = 0; i < count; i++)
        {
            if (this.balls.Count >= maxBallsSpawned) return;

            Ball spawnedBall = Instantiate(ballPrefab, position, Quaternion.identity) as Ball;
            spawnedBall.ConfigureSpeedBounds(minBallSpeed, maxBallSpeed);
            if (isLightningBall) spawnedBall.StartLightningBall();

            var rb = spawnedBall.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;

            // espalha ângulos de -spread..+spread uniformemente
            float t = (count <= 1) ? 0f : (i / (float)(count - 1));   // 0..1
            float offsetDeg = Mathf.Lerp(-multiBallSpreadDeg, multiBallSpreadDeg, t);
            Vector2 dir = (Quaternion.Euler(0, 0, offsetDeg) * baseDir).normalized;

            // lança com a MESMA lógica do lançamento principal, mas com "targetSpeed"
            LaunchBall(rb, dir, targetSpeed);

            // >>>iniciar rastro imediatamente após o lançamento de cada bola do multiball <<<
            spawnedBall.BeginTrailEmission();

            this.balls.Add(spawnedBall);
        }
    }


    //AIM--------------------------------------
    private void CreateAimLine()
    {
        if (!showAimIndicator || aimLine != null) return;

        var go = new GameObject("AimLine");
        aimLine = go.AddComponent<LineRenderer>();
        aimLine.useWorldSpace = true;
        aimLine.positionCount = 2;

        // Largura consistente
        aimLine.startWidth = lineWidth;
        aimLine.endWidth = lineWidth;

        // Material “Sprites/Default” (funciona bem em URP 2D)
        var mat = new Material(Shader.Find("Sprites/Default"));
        aimLine.material = mat;

        // Branco semi-opaco
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.9f, 1f) }
        );
        aimLine.colorGradient = grad;

        // Ordem alta para ficar acima dos bricks
        aimLine.sortingOrder = 1000;
    }

    private Vector2 CurrentAimDirection()
    {
        // Oscila 0..1..0 dentro do período
        float period = Mathf.Max(0.01f, aimOscillationPeriod);
        float phase = (Time.time % period) / period;
        float k = Mathf.Sin(phase * Mathf.PI * 2f) * 0.5f + 0.5f; // 0..1

        float angle = Mathf.Lerp(-maxAimAngleDeg, maxAimAngleDeg, k);
        Vector2 baseDir = invertYOnLaunch ? Vector2.down : Vector2.up;
        return (Quaternion.Euler(0, 0, angle) * baseDir).normalized;
    }

    private void UpdateAimLine()
    {
        if (!showAimIndicator || aimLine == null || initialBall == null) return;

        Vector3 start = initialBall.transform.position;
        Vector3 end = start + (Vector3)(CurrentAimDirection() * aimLength);
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
        aimLine.enabled = true;
    }

    private void HideAimLine()
    {
        if (aimLine != null) aimLine.enabled = false;
    }
    //--------------------------------------------------------
    private void EnsureRBSettings(Rigidbody2D rb)
    {
        if (rb == null) return;
        if (forceContinuousCollision) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (forceInterpolate) rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // Se quiser limitar velocidade absoluta: rb.sharedMaterial e drag ajudam;
        // clamparemos logo após o lançamento e pós-colisão (na Ball).
    }

    private IEnumerator IgnorePaddleCollisionTemporarily(float seconds)
    {
        if (initialBall == null || Paddle.Instance == null) yield break;

        var ballCol = initialBall.GetComponent<Collider2D>();
        var paddleCol = Paddle.Instance.GetComponent<Collider2D>();
        if (ballCol == null || paddleCol == null) yield break;

        Physics2D.IgnoreCollision(ballCol, paddleCol, true);
        // espere alguns FixedUpdates para garantir que saiu da área do paddle
        float t = 0f;
        while (t < seconds)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }
        if (ballCol != null && paddleCol != null)
            Physics2D.IgnoreCollision(ballCol, paddleCol, false);
    }


}
