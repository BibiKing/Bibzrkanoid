using UnityEngine;

public class DeathWall : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ball"))
        {
            Ball ball = collision.GetComponent<Ball>();
            if (!ball) return;
            BallsManager.Instance.balls.Remove(ball);
            ball.Die();

        }
    }
}
