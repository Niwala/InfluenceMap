//Metaballs © 2022 by Sam's Backpack is licensed under CC BY-SA 4.0 (http://creativecommons.org/licenses/by-sa/4.0/)
//Source page of the project : https://niwala.itch.io/metaballs


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

namespace SamsBackpack.Metaballs
{
    public class MetaballsRenderer : MonoBehaviour
    {
        [Header("Settings")]

        //Resolutions
        [SerializeField, Tooltip("The size of the buffers in which all computations will be performed.")]
        protected texSize m_computeResolution = texSize._512;
        protected int m_computeTexSize { get => (int)Mathf.Pow(2, (int)m_computeResolution); }
        [SerializeField, Tooltip("The size of the final rendering texture. Should be the same size or larger than the Compute Resolution.")]
        protected texSize m_renderResolution = texSize._512;
        protected int m_renderTexSize { get => (int)Mathf.Pow(2, (int)m_renderResolution); }
        protected bool m_useUpscaledVersion { get => m_computeResolution != m_renderResolution; }



        [Tooltip("Max. number of emitters If there are more emitters, they will be ignored.")]
        public int maxEmitterCount = 64;

        [Tooltip("Range of the area. Emitters outside this area will not be drawn.")]
        public float range = 10.0f;

        [Tooltip("Smoothness of the metaballs.")]
        public float smooth = 25.0f;

        [Header("Borders")]
        public BorderMode borderMode = BorderMode.SplitAreas;
        public AnimationCurve borderOpacity;
        [Tooltip("Thickness of the edges around blobs."), Range(0.1f, 5f)]
        public float borderRange = 0.3f;


        [ColorUsage(true, true)]
        public Color[] colors = new Color[4];

        //Hidden
        protected RenderTexture result;
        protected RenderTexture upscaledResult;
        protected ComputeBuffer emitterInfosBuffer;
        protected EmitterInfo[] emitterInfosArray;
        protected ComputeBuffer distancePerArea;
        protected ComputeBuffer areasA;
        protected ComputeBuffer areasB;
        protected ComputeBuffer colorBuffer;
        [HideInInspector]
        public HashSet<MetaballEmitter> emitters = new HashSet<MetaballEmitter>();
        protected Texture2D borderGradient;

        protected virtual void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 200, 80), 
                "R8 : " + SystemInfo.SupportsTextureFormat(TextureFormat.R8) +
                "\nARGBFloat : " + SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) +
                "\nCompute buffers : "+ SystemInfo.supportsComputeShaders);
        }

        protected virtual void OnEnable()
        {
            AllocateBuffers();
            BuildBorderGradient();
        }

        protected virtual void OnDisable()
        {
            ReleaseBuffers();
        }

        protected virtual void OnValidate()
        {
            if (m_renderResolution < m_computeResolution)
                m_renderResolution = m_computeResolution;

            if (Application.isPlaying)
            {
                AllocateBuffers();
                BuildBorderGradient();
            }
        }

        public virtual void BuildBorderGradient()
        {
            const int gradientResolution = 512;
            borderGradient = new Texture2D(gradientResolution, 1, GraphicsFormat.R8_UNorm, TextureCreationFlags.None);
            for (int i = 0; i < gradientResolution; i++)
            {
                float t = i / (gradientResolution - 1.0f);
                borderGradient.SetPixel(i, 0, Color.white * borderOpacity.Evaluate(t));
            }
            borderGradient.Apply();
        }

        protected virtual void AllocateBuffers()
        {
            bool rebuildBuffer = ((result != null && result.width != m_computeTexSize) || (colorBuffer != null && colorBuffer.count != colors.Length));
            if (rebuildBuffer)
            {
                ReleaseBuffers();
            }
            else
            {
                if (result != null)
                    return;
            }

            result = new RenderTexture(m_computeTexSize, m_computeTexSize, 0, RenderTextureFormat.ARGBFloat);
            result.enableRandomWrite = true;
            result.filterMode = FilterMode.Bilinear;

            emitterInfosBuffer = new ComputeBuffer(maxEmitterCount, EmitterInfo.stride);
            emitterInfosArray = new EmitterInfo[maxEmitterCount];

            areasA = new ComputeBuffer(m_computeTexSize * m_computeTexSize, AreaInfo.stride);
            areasB = new ComputeBuffer(m_computeTexSize * m_computeTexSize, AreaInfo.stride);

            distancePerArea = new ComputeBuffer(m_computeTexSize * m_computeTexSize * colors.Length, sizeof(float));
            colorBuffer = new ComputeBuffer(colors.Length, sizeof(float) * 4);
            colorBuffer.SetData(colors);
        }

        protected virtual void ReleaseBuffers()
        {
            if (result == null)
                return;

            result.Release();
            if (upscaledResult != null)
                upscaledResult.Release();

            emitterInfosBuffer.Release();
            distancePerArea.Release();
            areasA.Release();
            areasB.Release();
            colorBuffer.Release();
        }

        protected struct EmitterInfo
        {
            public Vector3 position;
            public float range;
            public int channel;

            public EmitterInfo(Vector3 position, float range, int channel)
            {
                this.position = position;
                this.range = range;
                this.channel = channel;
            }

            public static int stride
            {
                get { return sizeof(float) * 4 + sizeof(int); }
            }
        }

        protected struct AreaInfo
        {
            public float distance;
            public Vector2 coords;
            public int id;

            public static int stride
            {
                get { return sizeof(float) * 3 + sizeof(int); }
            }
        }

        protected virtual void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 size = Vector3.one * range;
            size.y = 0.01f;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        public enum texSize
        {
            _32 = 5,
            _64 = 6,
            _128 = 7,
            _256 = 8,
            _512 = 9,
            _1024 = 10,
            _2048 = 11,
            _4096 = 12
        }

        public enum BorderMode
        {
            NoBorders,
            GroupAllAreas,
            SplitAreas
        }
    }
}
