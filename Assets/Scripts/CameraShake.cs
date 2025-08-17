using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public AnimationCurve curve;
    public float defaultShakeDuration = 0.5f;

    public void Shake(float shakeDuration = 0f)
    {
        if (shakeDuration == 0f)
        {
            shakeDuration = defaultShakeDuration;
        }
        StartCoroutine(Shaking(shakeDuration));
    }

    private IEnumerator Shaking(float shakeDuration)
    {
        Vector3 startingPosition = transform.position;
        float elapsedTime = 0f;

        while ( elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float strength = curve.Evaluate(elapsedTime/ shakeDuration);
            transform.position = startingPosition + Random.insideUnitSphere * strength;
            yield return null;
        }

        transform.position = startingPosition;
    }
}
