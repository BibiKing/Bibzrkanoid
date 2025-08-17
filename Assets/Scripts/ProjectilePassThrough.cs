using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProjectilePassThrough : MonoBehaviour
{
    [SerializeField] private LayerMask brickMask; // selecione a Layer Brick
    [SerializeField] private bool destroyOnFirstHit = true; // false = perfura vários
    [SerializeField] private float lifetime = 3f;

    private Rigidbody2D rb;
    private Vector2 lastPos;
    private RaycastHit2D[] hits = new RaycastHit2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        lastPos = rb.position;
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        Vector2 cur = rb.position;
        Vector2 delta = cur - lastPos;
        float dist = delta.magnitude;
        if (dist > 0f)
        {
            int n = Physics2D.RaycastNonAlloc(lastPos, delta.normalized, hits, dist, brickMask);
            for (int i = 0; i < n; i++)
            {
                var brick = hits[i].collider ? hits[i].collider.GetComponent<Brick>() : null;
                if (brick != null)
                {
                    // Reaproveita a mesma lógica do tijolo
                    brick.ApplyCollisionLogic(null);

                    if (destroyOnFirstHit)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }
        }

        lastPos = cur;
    }
}
