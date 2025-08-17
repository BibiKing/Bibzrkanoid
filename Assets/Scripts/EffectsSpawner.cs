using System.Collections;
using System.Linq;
using UnityEngine;

public class EffectSpawner : MonoBehaviour
{
    [Header("Opções")]
    [SerializeField] private float zOffset = 0.2f;
    [Tooltip("Se algum ParticleSystem estiver em loop, o efeito será forçado a encerrar após esse tempo (segundos).")]
    [SerializeField] private float loopedSystemsTimeout = 8f;

    [Tooltip("Se true e este objeto estiver na cena (não como prefab), ele toca e se autodestrói sozinho ao iniciar.")]
    [SerializeField] private bool playOnAwakeInScene = false;

    private void Awake()
    {
        if (playOnAwakeInScene && Application.isPlaying)
        {
            StartAllAndScheduleDestroy();
        }
    }

    /// <summary>
    /// Instancia este prefab (EffectSpawner) na posição indicada.
    /// </summary>
    public EffectSpawner SpawnAllEffects(Vector3 position)
    {
        var spawnPos = new Vector3(position.x, position.y, position.z + zOffset);
        var instance = Instantiate(this, spawnPos, Quaternion.identity);
        instance.StartAllAndScheduleDestroy();
        return instance;
    }

    /// <summary>
    /// Instancia este prefab (EffectSpawner) na posição indicada, como filho de um transform (segue o parent).
    /// </summary>
    public EffectSpawner SpawnAllEffects(Vector3 position, Transform parent)
    {
        var spawnPos = new Vector3(position.x, position.y, position.z + zOffset);
        var instance = Instantiate(this, spawnPos, Quaternion.identity, parent);
        instance.StartAllAndScheduleDestroy();
        return instance;
    }

    // --- Internals ---

    private void StartAllAndScheduleDestroy()
    {
        var systems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in systems)
        {
            if (ps == null) continue;
            ps.Clear(true);
            ps.Play(true);
        }

        StartCoroutine(DestroyWhenDone(gameObject, systems, loopedSystemsTimeout));
    }

    private IEnumerator DestroyWhenDone(GameObject root, ParticleSystem[] systems, float timeoutIfLooping)
    {
        bool anyLooping = systems.Any(ps => ps != null && ps.main.loop);
        float elapsed = 0f;

        while (true)
        {
            bool anyAlive = false;
            foreach (var ps in systems)
            {
                if (ps != null && ps.IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive) break;

            if (anyLooping)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= timeoutIfLooping)
                {
                    foreach (var ps in systems)
                    {
                        if (ps != null && ps.main.loop)
                            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }

                    yield return new WaitWhile(() => systems.Any(ps => ps != null && ps.IsAlive(true)));
                    break;
                }
            }

            yield return null;
        }

        if (root != null) Destroy(root);
    }
}
