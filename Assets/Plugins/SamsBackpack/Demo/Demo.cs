using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SamsBackpack.Metaballs
{
    public class Demo : MonoBehaviour
    {
        public MetaballsBlitter blitter;
        public AnimationCurve selectCurve;
        public AnimationCurve deselectCurve;
        public float animSpeed;
        public float animAmplitude;
        public float areaSensibility;
        public float sizeSensibility;

        private MetaballEmitter[] emitters;
        private MetaballEmitter currentEmitter;
        private MetaballEmitter previousEmitter;
        private float currentEmitterRadius;
        private float previousEmitterRadius;
        private float currentEmitterTime;
        private float previousEmitterTime;

        private int drag = -1;
        private Vector2 dragPosition;
        private float startRadius;
        private int startArea;

        void Start()
        {
            emitters = blitter.GetComponentsInChildren<MetaballEmitter>();
        }

        void Update()
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);

                if (drag == -1)      //Search nearest point to drag
                {

                    float minDist = float.MaxValue;
                    int minId = -1;

                    for (int i = 0; i < emitters.Length; i++)
                    {
                        float dist = Vector3.Distance(emitters[i].transform.position, point);
                        if (dist < minDist && dist < emitters[i].radius * 2)
                        {
                            minDist = dist;
                            minId = i;
                        }
                    }

                    MetaballEmitter hoverEmitter = (minId == -1 ? null : emitters[minId]);
                    if (currentEmitter != hoverEmitter)
                        OnEmitterChange(hoverEmitter);
                }

                else if (drag == 0)        //Drag current point
                {
                    currentEmitter.transform.position = point;
                }

                else if (drag == 1)        //Drag color & size
                {
                    Vector2 delta = (Vector2)Input.mousePosition - dragPosition;
                    currentEmitter.transform.localScale = Vector3.one * Mathf.Clamp(startRadius + delta.x * sizeSensibility, 0.5f, 3.0f);
                    int newArea = Mathf.RoundToInt(startArea + delta.y * areaSensibility) % blitter.colors.Length;
                    if (newArea < 0)
                        newArea += blitter.colors.Length;
                    currentEmitter.area = newArea;
                }
            }

            //Anim emitters radius
            if (currentEmitter != null)
            {
                float t = Mathf.Clamp01((Time.time - currentEmitterTime) * animSpeed);
                currentEmitter.radius = currentEmitterRadius + selectCurve.Evaluate(t) * animAmplitude;
            }

            if (previousEmitter != null)
            {
                float t = Mathf.Clamp01((Time.time - previousEmitterTime) * animSpeed);
                previousEmitter.radius = previousEmitterRadius + deselectCurve.Evaluate(t) * animAmplitude;
            }

            if (Input.GetMouseButtonDown(0) && currentEmitter != null && drag == -1)
            {
                drag = 0;
                dragPosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonDown(1) && currentEmitter != null && drag == -1)
            {
                drag = 1;
                dragPosition = Input.mousePosition;
                startRadius = currentEmitter.transform.localScale.x;
                startArea = currentEmitter.area;
            }

            if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) && (drag != -1))
            {
                drag = -1;
            }
        }

        private void OnEmitterChange(MetaballEmitter newEmitter)
        {
            if (newEmitter != null && newEmitter == previousEmitter)
            {
                if (currentEmitter != null)
                    currentEmitter.radius = currentEmitterRadius;
                currentEmitter = previousEmitter;
                currentEmitterRadius = previousEmitterRadius;
                currentEmitterTime = Time.time;
                previousEmitter = null;
                return;
            }

            if (currentEmitter != null)
            {
                if (previousEmitter != null)
                {
                    previousEmitter.radius = previousEmitterRadius;
                }

                previousEmitter = currentEmitter;
                previousEmitterRadius = currentEmitterRadius;
                previousEmitterTime = Time.time;
            }

            currentEmitter = newEmitter;
            if (newEmitter != null)
            {
                currentEmitterRadius = (newEmitter == previousEmitter ? previousEmitterRadius : newEmitter.radius);
                currentEmitterTime = Time.time;
            }
        }


    }
}