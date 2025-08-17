using System.Collections;
using UnityEngine;

public class BallSquashStretch : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;         // arraste o RB da bola
    [SerializeField] private Transform deformRoot;   // arraste o DeformRoot
    [SerializeField] private Transform graphics;     // arraste o Graphics (opcional p/ keep upright)

    [Header("Speed-based stretch")]
    [Tooltip("Velocidade MIN da bola para 0% de efeito")]
    private float minSpeed = 3.5f;
    [Tooltip("Velocidade MAX da bola para 100% de efeito")]
    private float maxSpeed = 8.0f;
    [Tooltip("Fator máximo de alongamento (y) ao longo do movimento. X = 1/y p/ conservar área.")]
    [SerializeField] private float maxStretch = 1.25f;  // 1.0 = sem efeito; 1.25 = 25% stretch
    [Tooltip("Quão rápido o visual acompanha mudanças (maior = mais rápido)")]
    [SerializeField] private float followLerp = 12f;

    [Header("Impact squash")]
    [Tooltip("Fator de squash no impacto (y ao longo da NORMAL vira 1/impact, x vira impact)")]
    [SerializeField] private float impactFactor = 1.4f;
    [SerializeField] private float impactHold = 0.06f;
    [SerializeField] private float impactRecover = 0.14f;
    [SerializeField] private AnimationCurve impactEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Visual")]
    [SerializeField] private bool keepSpriteUpright = true; // mantém o sprite sem girar
    [SerializeField] private int sortingOrderDelta = 0;     // não é usado aqui, só lembrando que o FX usa

    // runtime
    private Quaternion targetRot = Quaternion.identity;
    private Vector3 targetScale = Vector3.one;
    private Coroutine impactCo;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (transform.childCount > 0)
        {
            deformRoot = transform.GetChild(0);
            if (deformRoot.childCount > 0) graphics = deformRoot.GetChild(0);
        }
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (deformRoot == null) Debug.LogWarning("[BallSquashStretch] DeformRoot não atribuído.");

        minSpeed = BallsManager.Instance.minBallSpeed;
        maxSpeed = BallsManager.Instance.maxBallSpeed;
    }

    private void LateUpdate()
    {
        if (deformRoot == null || rb == null) return;

        // Direção de movimento
        Vector2 v = rb.linearVelocity;
        float speed = v.magnitude;

        // Direção alvo: se quase parado, mantenha a atual
        if (speed > 0.001f)
        {
            // Alinhamos o eixo local +Y do DeformRoot com a direção do movimento
            var desired = Quaternion.AngleAxis(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f, Vector3.forward);
            targetRot = desired;
        }

        // Fator de stretch por velocidade (0..1 entre min e max)
        float t = 0f;
        if (maxSpeed > minSpeed) t = Mathf.Clamp01((speed - minSpeed) / (maxSpeed - minSpeed));
        // scaleY = Lerp(1, maxStretch, t), scaleX = 1/scaleY  (conserva área)
        float sY = Mathf.Lerp(1f, Mathf.Max(1.0001f, maxStretch), t);
        float sX = 1f / sY;
        targetScale = new Vector3(sX, sY, 1f);

        // Interpola suave até o alvo
        deformRoot.rotation = Quaternion.Slerp(deformRoot.rotation, targetRot, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
        deformRoot.localScale = Vector3.Lerp(deformRoot.localScale, targetScale, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

        // Mantém o sprite “em pé” se quiser (contra-rotação)
        if (keepSpriteUpright && graphics != null)
            graphics.rotation = Quaternion.identity;
    }

    /// <summary>Chame no impacto, passando a normal do contato.</summary>
    public void OnImpact(Vector2 collisionNormal)
    {
        if (deformRoot == null) return;
        if (impactCo != null) StopCoroutine(impactCo);
        impactCo = StartCoroutine(ImpactRoutine(collisionNormal));
    }

    private IEnumerator ImpactRoutine(Vector2 n)
    {
        n = n.sqrMagnitude > 0.0001f ? n.normalized : Vector2.up;

        // Fase 1: entra no squash alinhado à NORMAL (local +Y = normal)
        Quaternion squashRot = Quaternion.AngleAxis(Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg - 90f, Vector3.forward);
        Vector3 squashScale = new Vector3(impactFactor, 1f / impactFactor, 1f); // amassa no Y(local) (normal), estica no X

        float t = 0f;
        while (t < impactHold)
        {
            float k = impactEase.Evaluate(t / Mathf.Max(0.0001f, impactHold));
            deformRoot.rotation = Quaternion.Slerp(deformRoot.rotation, squashRot, k);
            deformRoot.localScale = Vector3.Lerp(deformRoot.localScale, squashScale, k);
            if (keepSpriteUpright && graphics != null) graphics.rotation = Quaternion.identity;
            t += Time.deltaTime;
            yield return null;
        }

        // Fase 2: volta pro alvo de velocidade (LateUpdate continuará ajustando depois)
        Quaternion startRot = deformRoot.rotation;
        Vector3 startScale = deformRoot.localScale;

        t = 0f;
        while (t < impactRecover)
        {
            float k = impactEase.Evaluate(t / Mathf.Max(0.0001f, impactRecover));
            deformRoot.rotation = Quaternion.Slerp(startRot, targetRot, k);
            deformRoot.localScale = Vector3.Lerp(startScale, targetScale, k);
            if (keepSpriteUpright && graphics != null) graphics.rotation = Quaternion.identity;
            t += Time.deltaTime;
            yield return null;
        }
        impactCo = null;
    }

    // API para Ball configurar min/max dinamicamente, se quiser ler do Ball
    public void ConfigureSpeedBounds(float min, float max)
    {
        minSpeed = min;
        maxSpeed = Mathf.Max(min + 0.0001f, max);
    }
}
