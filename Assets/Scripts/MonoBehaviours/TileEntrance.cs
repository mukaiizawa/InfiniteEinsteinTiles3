using System.Collections;

using UnityEngine;

public class TileEntrance : MonoBehaviour
{
    public float RotationSpeed = 360f;

    Vector2 _targetPos;
    Vector3 _targetScale;
    Quaternion _targetRotation;
    float _duration;
    float _delay;

    public void Initialize(Vector2 center, float duration, float delay)
    {
        _targetPos = transform.position;
        _targetScale = transform.localScale;
        _targetRotation = transform.rotation;
        _duration = duration;
        _delay = delay;
        var cam = Camera.main;
        float screenRadius = cam != null
            ? cam.orthographicSize * Mathf.Sqrt(cam.aspect * cam.aspect + 1f) * 2f
            : 20f;
        Vector2 dir = (Vector2)_targetPos - center;
        Vector2 startPos = dir.magnitude > 0.01f
            ? center + dir.normalized * screenRadius
            : center + Random.insideUnitCircle.normalized * screenRadius;
        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
        transform.localScale = Vector3.zero;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        if (_delay > 0f)
            yield return new WaitForSeconds(_delay);
        Vector2 startPos = transform.position;
        float rotDir = Random.value > 0.5f ? 1f : -1f;
        float t = 0f;
        while (t < _duration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / _duration);
            float eased = 1f - (1f - ratio) * (1f - ratio); // ease out quad
            Vector2 pos = Vector2.Lerp(startPos, _targetPos, eased);
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            transform.localScale = _targetScale * ratio;
            float rotSpeed = RotationSpeed * (1f - ratio) * rotDir;
            transform.Rotate(Vector3.forward * rotSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = new Vector3(_targetPos.x, _targetPos.y, transform.position.z);
        transform.localScale = _targetScale;
        transform.rotation = _targetRotation;
        Destroy(this);
    }
}
