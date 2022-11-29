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
    public class MetaballsBlitter : MetaballsRenderer
    {
        [Header("Components")]
        [SerializeField] private MeshRenderer rend;
        [SerializeField] private ComputeShader shader;
        [SerializeField] private Shader m_upscaleShader;
        private Material m_upscaleMaterial;


        protected override void OnEnable()
        {
            base.OnEnable();


            if (Application.isPlaying)
            {
                UpdateUpscaleBuffer();
            }
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
    }
}
