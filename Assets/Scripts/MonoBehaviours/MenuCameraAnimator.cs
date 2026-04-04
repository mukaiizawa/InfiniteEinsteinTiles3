using System.Collections;
using UnityEngine;

/// <summary>
/// Menu scene background camera animation.
/// Traces a lemniscate of Bernoulli (∞) path with Kepler-like speed variation:
/// faster near the center, slower at the outer edges.
/// Attach to the same root GameObject as MenuSceneManager.
/// </summary>
public class MenuCameraAnimator : MonoBehaviour
{
    [Tooltip("Scale of the ∞ path (world units)")]
    public float Scale = 100f;
    [Tooltip("Reference period in seconds (actual loop time varies with GravityStrength)")]
    public float Period = 70f;
    [Tooltip("Speed ratio between center and outer edge (1 = uniform, higher = stronger Kepler effect)")]
    public float GravityStrength = 5f;

    Camera _cam;
    Vector3 _origin;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Start()
    {
        _origin = _cam.transform.position;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        float param = 0f;
        float baseSpeed = 2f * Mathf.PI / Period;
        while (true)
        {
            // Lemniscate of Bernoulli: x = a·cos(t)/(1+sin²(t)), y = a·sin(t)·cos(t)/(1+sin²(t))
            float sinP = Mathf.Sin(param);
            float denom = 1f + sinP * sinP;
            float x = Scale * Mathf.Cos(param) / denom;
            float y = Scale * sinP * Mathf.Cos(param) / denom;
            _cam.transform.position = new Vector3(_origin.x + x, _origin.y + y, _origin.z);
            // Kepler-like: speed inversely proportional to distance from center
            float rNorm = Mathf.Max(Mathf.Sqrt(x * x + y * y) / Scale, 1f / GravityStrength);
            param += baseSpeed / rNorm * Time.deltaTime;
            yield return null;
        }
    }
}
