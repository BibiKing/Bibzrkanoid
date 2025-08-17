using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal; // ok manter mesmo sem URP (campo pode ficar null)

public class BallTrailController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SpriteRenderer graphicsSprite;
    [SerializeField] private Light2D graphicsLight2D;                 // opcional
    [SerializeField] private ParticleSystem lightningParticleSystem;  // opcional
    [SerializeField] private TrailRenderer trail;                     // único trail
    [SerializeField] private Rigidbody2D rb;                          // arraste o RB da bola

    [Header("Parâmetros do Trail")]
    [SerializeField] private float emitDelay = 0.02f;
    [SerializeField] private float trailTime = 0.45f;
    [SerializeField] private float minVertexDistance = 0.02f;
    [SerializeField] private float startWidth = 0.10f;
    [SerializeField] private float endWidth = 0.03f;
    [SerializeField] private int sortingOrderOffset = -1;

    [Header("Diagnóstico")]
    [SerializeField] private bool debugLogs = true;

    private Coroutine emitCo;

    private void Awake()
    {
        if (!trail) { if (debugLogs) Debug.LogWarning("[BallTrail] TrailRenderer não atribuído."); return; }

        // Defaults seguros
        trail.time = trailTime;
        trail.minVertexDistance = minVertexDistance;
        trail.alignment = LineAlignment.View;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, Mathf.Max(0.001f, startWidth)),
            new Keyframe(1f, Mathf.Max(0.001f, endWidth))
        );

        SyncSorting();
        trail.emitting = false;
        trail.Clear();

        ApplyNormalGradientFromLightOrSprite(); // já aplica cor normal (com alfa = 1)
    }

    private void OnEnable()
    {
        SyncSorting();
        if (trail != null) { trail.emitting = false; trail.Clear(); }
    }

    private void OnDisable()
    {
        if (emitCo != null) { StopCoroutine(emitCo); emitCo = null; }
    }

    // Mantém sorting sincronizado mesmo se outro script mexer no SpriteRenderer em runtime
    private void LateUpdate()
    {
        SyncSorting();
    }

    private void SyncSorting()
    {
        if (trail == null || graphicsSprite == null) return;
        trail.sortingLayerID = graphicsSprite.sortingLayerID;
        trail.sortingOrder = graphicsSprite.sortingOrder + sortingOrderOffset;
    }

    // ---------- Gradientes (FORÇANDO ALFA = 1) ----------
    private void ApplyNormalGradientFromLightOrSprite()
    {
        if (!trail) return;

        Color baseColor;
        if (graphicsLight2D != null) baseColor = graphicsLight2D.color;
        else if (graphicsSprite != null) baseColor = graphicsSprite.color;
        else baseColor = Color.white;

        baseColor.a = 1f; // <<< força visibilidade
        var white = Color.white; white.a = 1f;
        trail.colorGradient = BuildTwoColorGradient(baseColor, white);
    }

    private void ApplyLightningGradientFromParticle()
    {
        if (!trail) return;

        Color baseColor = ExtractPSStartColor(lightningParticleSystem, new Color(0f, 1f, 1f, 1f));
        baseColor.a = 1f; // <<< força visibilidade
        var white = Color.white; white.a = 1f;
        trail.colorGradient = BuildTwoColorGradient(baseColor, white);
    }

    private static Gradient BuildTwoColorGradient(Color start, Color end)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) } // <<< alfa 1 em toda curva
        );
        return g;
    }

    private static Color ExtractPSStartColor(ParticleSystem ps, Color fallback)
    {
        if (ps == null) return fallback;
        var main = ps.main;
        var sc = main.startColor;
        switch (sc.mode)
        {
            case ParticleSystemGradientMode.Color: return sc.color;
            case ParticleSystemGradientMode.TwoColors: return Color.Lerp(sc.colorMin, sc.colorMax, 0.5f);
            case ParticleSystemGradientMode.Gradient: return sc.gradient.Evaluate(0.5f);
            case ParticleSystemGradientMode.TwoGradients: return Color.Lerp(sc.gradientMin.Evaluate(0.5f), sc.gradientMax.Evaluate(0.5f), 0.5f);
            case ParticleSystemGradientMode.RandomColor: return sc.gradient.Evaluate(0.5f);
            default: return fallback;
        }
    }

    // ---------- API chamada pela Ball ----------
    public void OnLaunch()
    {
        if (debugLogs) Debug.Log("[BallTrail] OnLaunch");
        ApplyNormalGradientFromLightOrSprite();
        AutoClampMinVertexDistance();
        StartEmitWithDelay();
    }

    public void OnLightningEnable()
    {
        if (debugLogs) Debug.Log("[BallTrail] OnLightningEnable");
        ApplyLightningGradientFromParticle();
        AutoClampMinVertexDistance();
        if (trail != null && !trail.emitting) StartEmitWithDelay(0f);
    }

    public void OnLightningDisable()
    {
        if (debugLogs) Debug.Log("[BallTrail] OnLightningDisable");
        ApplyNormalGradientFromLightOrSprite();
        AutoClampMinVertexDistance();
    }

    public void OnDie()
    {
        if (debugLogs) Debug.Log("[BallTrail] OnDie");
        if (emitCo != null) { StopCoroutine(emitCo); emitCo = null; }
        if (trail != null) trail.emitting = false;
    }

    public void OnRespawnReset()
    {
        if (debugLogs) Debug.Log("[BallTrail] OnRespawnReset");
        if (emitCo != null) { StopCoroutine(emitCo); emitCo = null; }
        if (trail != null) { trail.emitting = false; trail.Clear(); }
        ApplyNormalGradientFromLightOrSprite();
    }

    private void StartEmitWithDelay(float? delayOverride = null)
    {
        if (trail == null) return;
        float d = delayOverride ?? emitDelay;
        if (emitCo != null) { StopCoroutine(emitCo); emitCo = null; }
        emitCo = StartCoroutine(EmitDelayed(d));
    }

    private IEnumerator EmitDelayed(float d)
    {
        if (d > 0f) yield return new WaitForSeconds(d);
        if (trail != null) trail.emitting = true;
        emitCo = null;
    }

    // Ajusta MVD se a distância por frame for menor (evita “zero vértices”)
    private void AutoClampMinVertexDistance()
    {
        if (!trail || !rb) return;

        float dxPerFrame = rb.linearVelocity.magnitude * Time.deltaTime;
        float suggested = Mathf.Clamp(dxPerFrame * 0.5f, 0.005f, minVertexDistance);
        if (suggested < trail.minVertexDistance)
        {
            if (debugLogs)
                Debug.Log($"[BallTrail] Ajuste MinVertexDistance {trail.minVertexDistance:F3} → {suggested:F3}");
            trail.minVertexDistance = suggested;
        }
    }
}
