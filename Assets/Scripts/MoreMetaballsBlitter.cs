using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MoreMetaballsBlitter : MonoBehaviour
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
    public HashSet<MoreMetaballEmitter> emitters = new HashSet<MoreMetaballEmitter>();
    private Texture2D borderGradient;

    private void OnEnable()
    {
        result = new RenderTexture(m_computeTexSize, m_computeTexSize, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Bilinear;

        if (m_useUpscaledVersion)
        {
            upscaledResult = new RenderTexture(m_renderTexSize, m_renderTexSize, 0, RenderTextureFormat.ARGBFloat);
            upscaledResult.enableRandomWrite = true;
            upscaledResult.filterMode = FilterMode.Bilinear;
            m_upscaleMaterial = new Material(m_upscaleShader);
        }

        emitterInfosBuffer = new ComputeBuffer(maxEmitterCount, EmitterInfo.stride);
        emitterInfosArray = new EmitterInfo[maxEmitterCount];

        areasA = new ComputeBuffer(m_computeTexSize * m_computeTexSize, AreaInfo.stride);
        areasB = new ComputeBuffer(m_computeTexSize * m_computeTexSize, AreaInfo.stride);
        distancePerArea = new ComputeBuffer(m_computeTexSize * m_computeTexSize * colors.Length, sizeof(float));
        colorBuffer = new ComputeBuffer(colors.Length, sizeof(float) * 4);
        colorBuffer.SetData(colors);

        BuildBorderGradient();
    }

    private void OnDisable()
    {
        result.Release();
        if (upscaledResult != null)
            upscaledResult.Release();

        emitterInfosBuffer.Release();
        distancePerArea.Release();
        areasA.Release();
        areasB.Release();
        colorBuffer.Release();
    }

    private void OnValidate()
    {
        BuildBorderGradient();
    }

    public void BuildBorderGradient()
    {
        const int gradientResolution = 512;
        borderGradient = new Texture2D(gradientResolution, 1, TextureFormat.R8, false);
        for (int i = 0; i < gradientResolution; i++)
        {
            float t = i / (gradientResolution - 1.0f);
            borderGradient.SetPixel(i, 0, Color.white * borderOpacity.Evaluate(t));
        }
        borderGradient.Apply();
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
        bool areasBufferSwap = false;

        //Pack emitter in a compute buffer
        Matrix4x4 w2l = transform.worldToLocalMatrix;
        int i = 0;
        foreach (var emitter in emitters)
        {
            Vector3 p = -w2l.MultiplyPoint(emitter.transform.position) / (range * 0.5f);
            float r = emitter.radius / (range * 0.5f) * emitter.transform.localScale.x;
            emitterInfosArray[i] = new EmitterInfo(p, r, (int) emitter.channel);
            i++;
        }
        emitterInfosBuffer.SetData(emitterInfosArray);

        int threadSize = Mathf.CeilToInt(m_computeTexSize / 8.0f);
        int channelThreadSize = Mathf.CeilToInt(colors.Length / 8.0f);

        //Global
        shader.SetInt("_BorderMode", (int) borderMode);
        shader.SetInt("_MapSize", m_computeTexSize);
        shader.SetVector("_InvMapSize", new Vector2(1.0f / m_computeTexSize, 1.0f / m_computeTexSize));
        shader.SetInt("_ChannelCount", colors.Length);
        shader.SetInt("_EmitterCount", i);
        shader.SetFloat("_Metaball_Smooth", smooth);
        shader.SetFloat("_BorderPower", borderRange);

        //Clear data
        int clearKernel = shader.FindKernel("Clear");
        shader.SetBuffer(clearKernel, "_Distances", distancePerArea);
        shader.Dispatch(clearKernel, threadSize, threadSize, channelThreadSize);

        //Asign areas
        int asignAreaKernel = shader.FindKernel("AsignAreas");
        shader.SetBuffer(asignAreaKernel, "_Emitters", emitterInfosBuffer);
        shader.SetBuffer(asignAreaKernel, "_Distances", distancePerArea);
        shader.Dispatch(asignAreaKernel, threadSize, threadSize, 1);

        //Flatten
        int FlattenKernel = shader.FindKernel("Flatten");
        shader.SetBuffer(FlattenKernel, "_Distances", distancePerArea);
        shader.SetBuffer(FlattenKernel, "_AreasRead", areasBufferSwap ? areasB : areasA);
        shader.SetBuffer(FlattenKernel, "_AreasWrite", areasBufferSwap ? areasA : areasB);
        shader.Dispatch(FlattenKernel, threadSize, threadSize, 1);
        areasBufferSwap = !areasBufferSwap;

        //Jump flooding
        if (borderMode != BorderMode.NoBorders)
        {
            int JumpFloodingrH = shader.FindKernel("JumpFlooding");

            int itterationCount = (int)m_computeResolution;
            for (int j = 0; j < itterationCount; j++)
            {
                shader.SetBuffer(JumpFloodingrH, "_AreasRead", areasBufferSwap ? areasB : areasA);
                shader.SetBuffer(JumpFloodingrH, "_AreasWrite", areasBufferSwap ? areasA : areasB);

                shader.SetInt("_JumpFloodingStep", j + 1);
                shader.SetInt("_JumpFloodingStepSize", (int)Mathf.Pow(2, ((int)m_computeResolution) - (j + 1)));
                shader.Dispatch(JumpFloodingrH, threadSize, threadSize, 1);
                areasBufferSwap = !areasBufferSwap;
            }
        }
        

        //Upscale render
        if (m_useUpscaledVersion)
        {
            //Render data pass
            int renderKernel = shader.FindKernel("RenderData");
            shader.SetTexture(renderKernel, "_Result", result);
            shader.SetBuffer(renderKernel, "_AreasRead", areasBufferSwap ? areasB : areasA);
            shader.Dispatch(renderKernel, threadSize, threadSize, 1);

            //Upscale pass
            m_upscaleMaterial.SetTexture("_BorderGradient", borderGradient);
            m_upscaleMaterial.SetTexture("_RenderData", result);
            m_upscaleMaterial.SetBuffer("_Colors", colorBuffer);
            m_upscaleMaterial.SetInt("_BorderMode", (int) borderMode);
            m_upscaleMaterial.SetFloat("_BorderPower", borderRange);

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
            shader.SetTexture(renderKernel, "_BorderGradient", borderGradient);
            shader.SetTexture(renderKernel, "_Result", result);
            shader.SetBuffer(renderKernel, "_AreasRead", areasBufferSwap ? areasB : areasA);
            shader.SetBuffer(renderKernel, "_Colors", colorBuffer);
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
