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
    /// <summary>
    /// This version of the script tries to bypass the Web limitations on the computeShader by performing operations on fragment shaders only.
    /// </summary>
    public class MetaballsFragmentRenderer : MetaballsRenderer
    {
        [Header("Components")]
        [SerializeField] private MeshRenderer rend;
        [SerializeField] private Shader shader;
        public Material material;
        public RenderTexture swapTex;

        protected override void AllocateBuffers()
        {
            base.AllocateBuffers();
            swapTex = new RenderTexture(m_computeTexSize, m_computeTexSize, 0, RenderTextureFormat.ARGBFloat);
            swapTex.enableRandomWrite = true;
            swapTex.filterMode = FilterMode.Bilinear;
        }

        protected override void ReleaseBuffers()
        {
            base.ReleaseBuffers();
            swapTex.Release();
        }

        protected void Update()
        {
            //Pack emitter in a compute buffer
            Matrix4x4 w2l = transform.worldToLocalMatrix;
            int i = 0;
            foreach (var emitter in emitters)
            {
                Vector3 p = -w2l.MultiplyPoint(emitter.transform.position) / (range * 0.5f);
                float r = emitter.radius / (range * 0.5f) * emitter.transform.localScale.x;
                emitterInfosArray[i] = new EmitterInfo(p, r, emitter.area);
                i++;
            }
            emitterInfosBuffer.SetData(emitterInfosArray);

            Graphics.ClearRandomWriteTargets();
            bool areasBufferSwap = false;

            if (material == null)
                material = new Material(shader);


            //Global
            material.SetInt(ShaderProperties.borderMode, (int)borderMode);
            material.SetInt(ShaderProperties.mapSize, m_computeTexSize);
            material.SetVector(ShaderProperties.invMapSize, new Vector2(1.0f / m_computeTexSize, 1.0f / m_computeTexSize));
            material.SetInt(ShaderProperties.areaCount, colors.Length);
            material.SetInt(ShaderProperties.emitterCount, i);
            material.SetFloat(ShaderProperties.smoothing, smooth);
            material.SetFloat(ShaderProperties.borderPower, borderRange);
            material.SetBuffer(ShaderProperties.emitters, emitterInfosBuffer);
            material.SetBuffer(ShaderProperties.areaColors, colorBuffer);
            material.SetBuffer(ShaderProperties.distanceFields, distancePerArea);
            Graphics.SetRandomWriteTarget(1, distancePerArea);


            //Clear data
            Graphics.Blit(swapTex, result, material, 0);

            //Asign areas
            Graphics.Blit(result, swapTex, material, 1);


            //Flatten
            material.SetBuffer(ShaderProperties.areaWrite, areasBufferSwap ? areasA : areasB);
            Graphics.SetRandomWriteTarget(2, areasA);
            Graphics.SetRandomWriteTarget(3, areasB);
            Graphics.Blit(swapTex, result, material, 2);
            areasBufferSwap = !areasBufferSwap;

            //Jump flooding
            if (borderMode != BorderMode.NoBorders)
            {
                int itterationCount = (int)m_computeResolution;
                for (int j = 0; j < itterationCount; j++)
                {
                    material.SetInt(ShaderProperties.jumpFloodingStepSize, (int)Mathf.Pow(2, ((int)m_computeResolution) - (j + 1)));
                    material.SetBuffer(ShaderProperties.areaRead, areasBufferSwap ? areasB : areasA);
                    material.SetBuffer(ShaderProperties.areaWrite, areasBufferSwap ? areasA : areasB);
                    //Graphics.SetRandomWriteTarget(2, areasBufferSwap ? areasA : areasB);
                    Graphics.Blit(areasBufferSwap ? result : swapTex, areasBufferSwap ? swapTex : result, material, 3);
                    areasBufferSwap = !areasBufferSwap;
                }
            }

            //Draw result on the renderer
            if (rend != null)
                rend.sharedMaterial.mainTexture = areasBufferSwap ? result : swapTex;
            return;


            //Upscale render
            //if (m_useUpscaledVersion)
            //{
            //    //Render data pass
            //    int renderKernel = shader.FindKernel("RenderData");
            //    shader.SetTexture(renderKernel, ShaderProperties.result, result);
            //    shader.SetBuffer(renderKernel, ShaderProperties.areaRead, areasBufferSwap ? areasB : areasA);
            //    shader.Dispatch(renderKernel, threadSize, threadSize, 1);

            //    //Upscale pass
            //    m_upscaleMaterial.SetTexture(ShaderProperties.borderGradient, borderGradient);
            //    m_upscaleMaterial.SetTexture(ShaderProperties.renderData, result);
            //    m_upscaleMaterial.SetBuffer(ShaderProperties.areaColors, colorBuffer);
            //    m_upscaleMaterial.SetInt(ShaderProperties.borderMode, (int)borderMode);
            //    m_upscaleMaterial.SetFloat(ShaderProperties.borderPower, borderRange);

            //    Graphics.Blit(Texture2D.whiteTexture, upscaledResult);
            //    Graphics.Blit(result, upscaledResult, m_upscaleMaterial);

            //    //Draw result on the renderer
            //    if (rend != null)
            //        rend.sharedMaterial.mainTexture = upscaledResult;
            //}

            ////Direct Render
            //else
            {

                //Render pass
                material.SetBuffer(ShaderProperties.distanceFields, distancePerArea);
                material.SetTexture(ShaderProperties.borderGradient, borderGradient);
                //material.SetTexture(ShaderProperties.result, result);
                material.SetBuffer(ShaderProperties.areaRead, areasBufferSwap ? areasB : areasA);
                material.SetBuffer(ShaderProperties.areaColors, colorBuffer);
                Graphics.Blit(swapTex, result, material, 4);


                //Draw result on the renderer
                if (rend != null)
                    rend.sharedMaterial.mainTexture = result;
            }
        }
    }
}
