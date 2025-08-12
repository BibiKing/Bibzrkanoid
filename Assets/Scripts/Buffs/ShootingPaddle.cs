public class ShootingPaddle : Buff
{
    protected override void ApplyEffect()
    {
        Paddle.Instance.StartShooting();
    }
}
