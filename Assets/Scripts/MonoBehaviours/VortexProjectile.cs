using System.Collections;

using UnityEngine;

public class VortexProjectile : MonoBehaviour
{
    public float RotationSpeed = 360f;

    Vector2 _center;
    float _duration;
    float _delay;

    public void Initialize(Vector2 center, float duration)
    {
        _center = center;
        _duration = duration;

        var collider = GetComponent<Collider2D>();
        if (collider != null) Destroy(collider);

        float distance = ((Vector2)transform.position - center).magnitude;
        _delay = Mathf.Max(0f, 0.3f - distance * 0.02f);

        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        if (_delay > 0f)
            yield return new WaitForSeconds(_delay);

        Vector2 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        float startDistance = (startPos - _center).magnitude;
        float startAngle = Mathf.Atan2(startPos.y - _center.y, startPos.x - _center.x);

        float spinSign = (startPos.x + startPos.y) > (_center.x + _center.y) ? 1f : -1f;
        float totalAngle = (2f + startDistance * 0.3f) * 360f * spinSign * Mathf.Deg2Rad;

        float rotDir = Random.value > 0.5f ? 1f : -1f;
        float duration = _duration - _delay;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / duration);
            float eased = ratio * ratio;

            float currentDistance = Mathf.Lerp(startDistance, 0f, eased);
            float angle = startAngle + totalAngle * eased;
            Vector2 newPos = _center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentDistance;
            transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

            transform.localScale = startScale * Mathf.Lerp(1f, 0f, eased);

            float rotSpeed = RotationSpeed * (1f + eased * 4f) * rotDir;
            transform.Rotate(Vector3.forward * rotSpeed * Time.deltaTime);

            yield return null;
        }

        gameObject.SetActive(false);
    }

}
