public class ExtendOrShrinkPaddle : Buff
{
    public float widthExpandRate = 2f;
    protected override void ApplyEffect()
    {
        if (Paddle.Instance != null && !Paddle.Instance.PaddleIsTransforming)
        {
            Paddle.Instance.StartWidthAnimation(Paddle.Instance.defaultPaddleWidth * widthExpandRate);
        }
    }
}
