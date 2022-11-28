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
    public class MetaballsBlitter : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private MeshRenderer rend;
        [SerializeField] private ComputeShader shader;
        [SerializeField] private Shader m_upscaleShader;
        private Material m_upscaleMaterial;

        [Header("Settings")]

        //Resolutions
        [SerializeField, Tooltip("The size of the buffers in which all computations will be performed.")]
        private texSize m_computeResolution = texSize._512;
        private int m_computeTexSize { get => (int)Mathf.Pow(2, (int)m_computeResolution); }
        [SerializeField, Tooltip("The size of the final rendering texture. Should be the same size or larger than the Compute Resolution.")]
        private texSize m_renderResolution = texSize._512;
        private int m_renderTexSize { get => (int)Mathf.Pow(2, (int)m_renderResolution); }
        private bool m_useUpscaledVersion { get => m_computeResolution != m_renderResolution; }



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
        private RenderTexture result;
        private RenderTexture upscaledResult;
        private ComputeBuffer emitterInfosBuffer;
        private EmitterInfo[] emitterInfosArray;
        private ComputeBuffer distancePerArea;
        private ComputeBuffer areasA;
        private ComputeBuffer areasB;
        private ComputeBuffer colorBuffer;
        [HideInInspector]
        public HashSet<MetaballEmitter> emitters = new HashSet<MetaballEmitter>();
        private Texture2D borderGradient;

        private void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 200, 80), 
                "R8 : " + SystemInfo.SupportsTextureFormat(TextureFormat.R8) +
                "\nARGBFloat : " + SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) +
                "\nCompute buffers : "+ SystemInfo.supportsComputeShaders);
        }

        private void OnEnable()
        {
            AllocateBuffers();
            UpdateUpscaleBuffer();
            BuildBorderGradient();
        }

        private void OnDisable()
        {
            ReleaseBuffers();
        }

        private void OnValidate()
        {
            if (m_renderResolution < m_computeResolution)
                m_renderResolution = m_computeResolution;

            AllocateBuffers();
            UpdateUpscaleBuffer();
            BuildBorderGradient();
        }

        public void BuildBorderGradient()
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

        private void AllocateBuffers()
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

        private void ReleaseBuffers()
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

        private void UpdateUpscaleBuffer()
        {
            if (m_useUpscaledVersion)
            {
                upscaledResult = new RenderTexture(m_renderTexSize, m_renderTexSize, 0, RenderTextureFormat.ARGBFloat);
                upscaledResult.enableRandomWrite = true;
                upscaledResult.filterMode = FilterMode.Bilinear;
                m_upscaleMaterial = new Material(m_upscaleShader);
            }
            else
            {
                if (upscaledResult != null)
                    upscaledResult.Release();

                if (m_upscaleMaterial != null)
                    DestroyImmediate(m_upscaleMaterial);
            }
        }

        struct EmitterInfo
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

        struct AreaInfo
        {
            public float distance;
            public Vector2 coords;
            public int id;

            public static int stride
            {
                get { return sizeof(float) * 3 + sizeof(int); }
            }
        }

        private void Update()
        {
            //Pack emitter in a compute buffer
            Matrix4x4 w2l = transform.worldToLocalMatrix;
            int i = 0;
            foreach (var emitter in emitters)
            {
                Vector3 p = -w2l.MultiplyPoint(emitter.transform.position) / (range * 0.5f);
                float r = emitter.radius / (range * 0.5f) * emitter.transform.localScale.x;
                emitterInfosArray[i] = new EmitterInfo(p, r, (int)emitter.area);
                i++;
            }
            emitterInfosBuffer.SetData(emitterInfosArray);


            bool areasBufferSwap = false;
            int threadSize = Mathf.CeilToInt(m_computeTexSize / 8.0f);
            int channelThreadSize = Mathf.CeilToInt(colors.Length / 8.0f);

            //Global
            shader.SetInt(ShaderProperties.borderMode, (int)borderMode);
            shader.SetInt(ShaderProperties.mapSize, m_computeTexSize);
            shader.SetVector(ShaderProperties.invMapSize, new Vector2(1.0f / m_computeTexSize, 1.0f / m_computeTexSize));
            shader.SetInt(ShaderProperties.areaCount, colors.Length);
            shader.SetInt(ShaderProperties.emitterCount, i);
            shader.SetFloat(ShaderProperties.smoothing, smooth);
            shader.SetFloat(ShaderProperties.borderPower, borderRange);

            //Clear data
            int clearKernel = shader.FindKernel("Clear");
            shader.SetBuffer(clearKernel, ShaderProperties.distanceFields, distancePerArea);
            shader.Dispatch(clearKernel, threadSize, threadSize, channelThreadSize);

            //Asign areas
            int asignAreaKernel = shader.FindKernel("AsignAreas");
            shader.SetBuffer(asignAreaKernel, ShaderProperties.emitters, emitterInfosBuffer);
            shader.SetBuffer(asignAreaKernel, ShaderProperties.distanceFields, distancePerArea);
            shader.Dispatch(asignAreaKernel, threadSize, threadSize, 1);

            //Flatten
            int FlattenKernel = shader.FindKernel("Flatten");
            shader.SetBuffer(FlattenKernel, ShaderProperties.distanceFields, distancePerArea);
            shader.SetBuffer(FlattenKernel, ShaderProperties.areaRead, areasBufferSwap ? areasB : areasA);
            shader.SetBuffer(FlattenKernel, ShaderProperties.areaWrite, areasBufferSwap ? areasA : areasB);
            shader.Dispatch(FlattenKernel, threadSize, threadSize, 1);
            areasBufferSwap = !areasBufferSwap;

            //Jump flooding
            if (borderMode != BorderMode.NoBorders)
            {
                int JumpFloodingrH = shader.FindKernel("JumpFlooding");

                int itterationCount = (int)m_computeResolution;
                for (int j = 0; j < itterationCount; j++)
                {
                    shader.SetBuffer(JumpFloodingrH, ShaderProperties.areaRead, areasBufferSwap ? areasB : areasA);
                    shader.SetBuffer(JumpFloodingrH, ShaderProperties.areaWrite, areasBufferSwap ? areasA : areasB);
                    shader.SetInt(ShaderProperties.jumpFloodingStepSize, (int)Mathf.Pow(2, ((int)m_computeResolution) - (j + 1)));
                    shader.Dispatch(JumpFloodingrH, threadSize, threadSize, 1);
                    areasBufferSwap = !areasBufferSwap;
                }
            }


            //Upscale render
            if (m_useUpscaledVersion)
            {
                //Render data pass
                int renderKernel = shader.FindKernel("RenderData");
                shader.SetTexture(renderKernel, ShaderProperties.result, result);
                shader.SetBuffer(renderKernel, ShaderProperties.areaRead, areasBufferSwap ? areasB : areasA);
                shader.Dispatch(renderKernel, threadSize, threadSize, 1);

                //Upscale pass
                m_upscaleMaterial.SetTexture(ShaderProperties.borderGradient, borderGradient);
                m_upscaleMaterial.SetTexture(ShaderProperties.renderData, result);
                m_upscaleMaterial.SetBuffer(ShaderProperties.areaColors, colorBuffer);
                m_upscaleMaterial.SetInt(ShaderProperties.borderMode, (int)borderMode);
                m_upscaleMaterial.SetFloat(ShaderProperties.borderPower, borderRange);

                Graphics.Blit(Texture2D.whiteTexture, upscaledResult);
                Graphics.Blit(result, upscaledResult, m_upscaleMaterial);

                //Draw result on the renderer
                if (rend != null)
                    rend.sharedMaterial.mainTexture = upscaledResult;
            }

            //Direct Render
            else
            {
                //Render pass
                int renderKernel = shader.FindKernel("Render");
                shader.SetTexture(renderKernel, ShaderProperties.borderGradient, borderGradient);
                shader.SetTexture(renderKernel, ShaderProperties.result, result);
                shader.SetBuffer(renderKernel, ShaderProperties.areaRead, areasBufferSwap ? areasB : areasA);
                shader.SetBuffer(renderKernel, ShaderProperties.areaColors, colorBuffer);
                shader.Dispatch(renderKernel, threadSize, threadSize, 1);


                //Draw result on the renderer
                if (rend != null)
                    rend.sharedMaterial.mainTexture = result;
            }
        }

        private void OnDrawGizmos()
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
