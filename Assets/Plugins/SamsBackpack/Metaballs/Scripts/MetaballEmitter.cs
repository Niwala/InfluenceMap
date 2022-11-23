//Metaballs © 2022 by Sam's Backpack is licensed under CC BY-SA 4.0 (http://creativecommons.org/licenses/by-sa/4.0/)
//Source page of the project : https://niwala.itch.io/metaballs

using UnityEngine;
using UnityEngine.Serialization;

namespace SamsBackpack.Metaballs
{
    public class MetaballEmitter : MonoBehaviour
    {
        public int area = 0;
        public float radius = 1;

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private Color gizmoColor;
#endif

        private void OnEnable()
        {
            GetComponentInParent<MetaballsBlitter>()?.emitters.Add(this);
        }

        private void OnDisable()
        {
            GetComponentInParent<MetaballsBlitter>()?.emitters.Remove(this);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            MetaballsBlitter blitter = GetComponentInParent<MetaballsBlitter>();
            if (blitter != null && area >= 0 && blitter.colors.Length > area)
                gizmoColor = blitter.colors[area];
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Color color = Color.white;
            color.a = 0.7f;
            Gizmos.color = color;
            float radius = this.radius - 0.1f;

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
}