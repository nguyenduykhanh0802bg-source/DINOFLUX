using UnityEngine;

/// <summary>Rung camera đơn giản (dùng unscaled time nên vẫn rung được khi slow-motion/hit-stop).</summary>
public class CameraShake : MonoBehaviour
{
    Vector3 origin;
    float duration, remaining, magnitude;

    void Awake()
    {
        origin = transform.position;
    }

    public void Shake(float seconds, float strength)
    {
        duration = Mathf.Max(0.01f, seconds);
        remaining = duration;
        magnitude = strength;
    }

    void LateUpdate()
    {
        if (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            float falloff = Mathf.Clamp01(remaining / duration);
            Vector2 offset = Random.insideUnitCircle * (magnitude * falloff);
            transform.position = origin + new Vector3(offset.x, offset.y, 0f);
        }
        else
        {
            transform.position = origin;
        }
    }
}
