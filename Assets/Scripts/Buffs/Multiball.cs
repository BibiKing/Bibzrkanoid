using System.Linq;

public class Multiball : Buff
{
    protected override void ApplyEffect()
    {
        foreach (Ball ball in BallsManager.Instance.balls.ToList())
        {
            BallsManager.Instance.SpawnBalls(ball.gameObject.transform.position, 2, ball.isLightningBall);
        }
    }
}
