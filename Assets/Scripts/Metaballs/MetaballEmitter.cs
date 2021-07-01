using UnityEngine;

public class MetaballEmitter : MonoBehaviour
{
    public Channel channel = Channel.Red;
    public float radius = 1;

    public enum Channel
    {
        Red,
        Green,
        Blue,
        Alpha,
    }

    public Color GetColor()
    {
        switch (channel)
        {
            case Channel.Red:
                return new Color(1, 0, 0, 0);
            case Channel.Green:
                return new Color(0, 1, 0, 0);
            case Channel.Blue:
                return new Color(0, 0, 1, 0);
            case Channel.Alpha:
                return new Color(0, 0, 0, 1);
        }
        return Color.clear;
    }

    private void OnEnable()
    {
        GetComponentInParent<MetaballsBlitter>()?.emitters.Add(this);
    }

    private void OnDisable()
    {
        GetComponentInParent<MetaballsBlitter>()?.emitters.Remove(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Color color = GetColor();
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
