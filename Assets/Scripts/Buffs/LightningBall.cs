using UnityEngine;

public class LightningBall : Buff
{
    protected override void ApplyEffect()
    {
        foreach (Ball ball in BallsManager.Instance.balls)
        {
            ball.StartLightningBall();
        }
    }
}
