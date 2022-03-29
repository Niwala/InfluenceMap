using UnityEngine;

public class MoreMetaballEmitter : MonoBehaviour
{
    public int channel = 0;
    public float radius = 1;

#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    private Color gizmoColor;
#endif

    private void OnEnable()
    {
        GetComponentInParent<MoreMetaballsBlitter>()?.emitters.Add(this);
    }

    private void OnDisable()
    {
        GetComponentInParent<MoreMetaballsBlitter>()?.emitters.Remove(this);
    }

    private void OnValidate()
    {
        MoreMetaballsBlitter blitter = GetComponentInParent<MoreMetaballsBlitter>();
        if (blitter != null && channel >= 0 && blitter.colors.Length > channel)
            gizmoColor = blitter.colors[channel];
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Color color = gizmoColor;
        color.a = 1.0f;
        Gizmos.color = color;

        Vector3 p0 = new Vector3(1.0f, 0.0f, 0.0f) * radius;
        for (float t = 0; t < 1.0f; t += 0.02f)
        {
            float a = t * Mathf.PI * 2.0f;
            Vector3 p1 = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(p0, p1);
            p0 = p1;
        }
    }

}
