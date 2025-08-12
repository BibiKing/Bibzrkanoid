using Unity.VisualScripting;
using UnityEngine;

public abstract class Buff : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Paddle"))
        {
            this.ApplyEffect();
        }

        if (collision.CompareTag("Paddle") || collision.CompareTag("DeathWall"))
        {
            Destroy(this.gameObject);
        }
    }


    protected abstract void ApplyEffect();
}
