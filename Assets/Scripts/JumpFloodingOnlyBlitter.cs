using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpFloodingOnlyBlitter : MonoBehaviour
{
    [Header("Components")]
    public MeshRenderer rend;

    public ComputeShader shader;

    [Header("Settings")]
    [Tooltip("Resolution in pixels of the texture.")]
    public int textureSize = 512;

    [Tooltip("Max. number of emitters If there are more emitters, they will be ignored.")]
    public int maxEmitterCount = 64;

    [Tooltip("Range of the area. Emitters outside this area will not be drawn.")]
    public float range = 10.0f;

    [Tooltip("Smoothness of the metaballs.")]
    public float smooth = 25.0f;

    [Tooltip("Thickness of the edges around blobs."), Range(-0.2f, 0.4f)]
    public float edgeThickness = 0.1f;

    [Tooltip("Number of iterations of the jumpFlooding. Allows you to have a larger gradient area. A large number is more expensive.")]
    public int jumpFloodingItterations = 10;

    [ColorUsage(true, true)]
    public Color[] colors = new Color[4];

    //Hidden
    private RenderTexture result;
    private ComputeBuffer emitterInfosBuffer;
    private EmitterInfos[] emitterInfosArray;
    private ComputeBuffer channelsBuffer;
    [HideInInspector]
    public HashSet<JumpFloodingOnlyEmitter> emitters = new HashSet<JumpFloodingOnlyEmitter>();

    public ComputeBuffer areaIds;

    private void OnEnable()
    {
        result = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;
        emitterInfosBuffer = new ComputeBuffer(maxEmitterCount, EmitterInfos.stride);
        areaIds = new ComputeBuffer(textureSize * textureSize, sizeof(float) + sizeof(int) * 3);
        emitterInfosArray = new EmitterInfos[maxEmitterCount];

        channelsBuffer = new ComputeBuffer(textureSize * textureSize * colors.Length, sizeof(float) * 4);
    }

    private void OnDisable()
    {
        result.Release();
        emitterInfosBuffer.Release();
        channelsBuffer.Release();
        areaIds.Release();
    }

    struct EmitterInfos
    {
        public Vector3 position;
        public float range;
        public int channel;

        public EmitterInfos(Vector3 position, float range, int channel)
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

    void Update()
    {
        //Pack emitter in a compute buffer
        Matrix4x4 w2l = transform.worldToLocalMatrix;
        int i = 0;
        foreach (var emitter in emitters)
        {
            Vector3 p = -w2l.MultiplyPoint(emitter.transform.position) / (range * 0.5f);
            float r = emitter.radius / (range * 0.5f) * emitter.transform.localScale.x;
            emitterInfosArray[i] = new EmitterInfos(p, r, (int) emitter.channel);
            i++;
        }
        emitterInfosBuffer.SetData(emitterInfosArray);

        int threadSize = Mathf.CeilToInt(textureSize / 8.0f);
        int channelThreadSize = Mathf.CeilToInt(colors.Length / 8.0f);


        //Clear data
        int clearKernel = shader.FindKernel("Clear");
        shader.SetInt("_MapSize", textureSize);
        shader.SetInt("_ChannelCount", colors.Length);
        shader.SetBuffer(clearKernel, "_Distances", channelsBuffer);
        shader.SetBuffer(clearKernel, "_AreaIds", areaIds);
        shader.Dispatch(clearKernel, threadSize, threadSize, 1);

        //Asign areas
        int asignAreaKernel = shader.FindKernel("AsignAreas");
        shader.SetBuffer(asignAreaKernel, "_Emitters", emitterInfosBuffer);
        shader.SetBuffer(asignAreaKernel, "_Distances", channelsBuffer);
        shader.SetBuffer(asignAreaKernel, "_AreaIds", areaIds);
        shader.SetInt("_EmitterCount", i);
        shader.SetFloat("_Metaball_Smooth", smooth);
        shader.SetVector("_InvMapSize", new Vector2(1.0f / textureSize, 1.0f / textureSize));
        shader.Dispatch(asignAreaKernel, threadSize, threadSize, 1);

        //Flatten
        int FlattenKernel = shader.FindKernel("Flatten");
        shader.SetBuffer(FlattenKernel, "_Distances", channelsBuffer);
        shader.SetBuffer(FlattenKernel, "_AreaIds", areaIds);
        shader.Dispatch(FlattenKernel, threadSize, threadSize, 1);


        //Jump flooding
        int JumpFloodingrH = shader.FindKernel("JumpFlooding");
        shader.SetInt("_JumpFloodingStepCount", jumpFloodingItterations);
        shader.SetBuffer(JumpFloodingrH, "_Distances", channelsBuffer);
        shader.SetBuffer(JumpFloodingrH, "_AreaIds", areaIds);
        for (int j = 0; j < jumpFloodingItterations; j++)
        {
            shader.SetInt("_JumpFloodingStep", j + 1);
            shader.Dispatch(JumpFloodingrH, threadSize, threadSize, 1);
        }
        
        //Render
        int renderKernel = shader.FindKernel("Render");
        shader.SetTexture(renderKernel, "_Result", result);
        shader.SetBuffer(renderKernel, "_Distances", channelsBuffer);
        shader.SetBuffer(renderKernel, "_AreaIds", areaIds);
        shader.SetFloat("_BorderSize", edgeThickness);
        Vector4[] c = new Vector4[colors.Length];
        for (int j = 0; j < colors.Length; j++)
            c[j] = colors[j];
        shader.SetVectorArray("_Colors", c);
        shader.Dispatch(renderKernel, threadSize, threadSize, 1);

        //Draw result on the renderer
        if (rend != null)
            rend.sharedMaterial.mainTexture = result;
    }


    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 size = Vector3.one * range;
        size.y = 0.01f;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
