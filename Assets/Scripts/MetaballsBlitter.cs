using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetaballsBlitter : MonoBehaviour
{
    [Header("Components")]
    public Material blitter;
    public MeshRenderer rend;

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

    [Tooltip("Number of iterations of the blur. Allows you to have a larger gradient area. A large number is more expensive."), Min(2)]
    public int blurItterations = 10;

    [Tooltip("Allows to enlarge the steps of the blur. Gives the same result as increasing the blur iterations but without being more expensive to calculate. On the other hand it generates banding... (Maybe adjustable with dithering?)"), Min(1.0f)]
    public float blurRange = 1.0f;

    [Header("Colors")]
    [ColorUsage(false, true)]
    public Color redChannelColor = Color.red;
    [ColorUsage(false, true)]
    public Color greenChannelColor = Color.green;
    [ColorUsage(false, true)]
    public Color blueChannelColor = Color.blue;
    [ColorUsage(false, true)]
    public Color alphaChannelColor = Color.white;


    //Hidden
    private bool swap;
    private RenderTexture texA;
    private RenderTexture texB;
    private ComputeBuffer emitterInfosBuffer;
    private EmitterInfos[] emitterInfosArray;
    [HideInInspector]
    public HashSet<MetaballEmitter> emitters = new HashSet<MetaballEmitter>();

    private void OnEnable()
    {
        texA = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        texB = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        emitterInfosBuffer = new ComputeBuffer(maxEmitterCount, EmitterInfos.stride);
        emitterInfosArray = new EmitterInfos[maxEmitterCount];
    }

    private void OnDisable()
    {
        texA.Release();
        texB.Release();
        emitterInfosBuffer.Release();
    }

    struct EmitterInfos
    {
        public Vector3 position;
        public float range;
        public Color channels;

        public EmitterInfos(Vector3 position, float range, Color channel)
        {
            this.position = position;
            this.range = range;
            this.channels = channel;
        }

        public static int stride
        {
            get { return sizeof(float) * 8; }
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
            Color c = emitter.GetColor();
            emitterInfosArray[i] = new EmitterInfos(p, r, c);
            i++;
        }
        emitterInfosBuffer.SetData(emitterInfosArray);


        //Draw metaballs
        blitter.SetBuffer("_Emitters", emitterInfosBuffer);
        blitter.SetInt("_EmitterCount", i);
        blitter.SetFloat("_Metaball_Smooth", smooth);
        Graphics.Blit(swap ? texB : texA, swap ? texA : texB, blitter, 0);
        swap = !swap;


        //Select main color
        Graphics.Blit(swap ? texB : texA, swap ? texA : texB, blitter, 1);
        swap = !swap;

        
        //Vertical blur
        blitter.SetVector("_BlurRange", new Vector4(0.0f, blurRange, 0.0f, 0.0f));
        blitter.SetInt("_BlurItterations", blurItterations);
        Graphics.Blit(swap ? texB : texA, swap ? texA : texB, blitter, 2);
        swap = !swap;


        //Horizontal blur
        blitter.SetVector("_BlurRange", new Vector4(blurRange, 0.0f, 0.0f, 0.0f));
        Graphics.Blit(swap ? texB : texA, swap ? texA : texB, blitter, 2);
        swap = !swap;

        //Isolate edges
        blitter.SetFloat("_EdgeThickness", edgeThickness);
        blitter.SetColor("_RedChannelColor", redChannelColor);
        blitter.SetColor("_GreenChannelColor", greenChannelColor);
        blitter.SetColor("_BlueChannelColor", blueChannelColor);
        blitter.SetColor("_AlphaChannelColor", alphaChannelColor);
        Graphics.Blit(swap ? texB : texA, swap ? texA : texB, blitter, 3);
        swap = !swap;


        //Draw result on the renderer
        if (rend != null)
            rend.sharedMaterial.mainTexture = swap ? texB : texA;
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
