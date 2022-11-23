//Metaballs by Sam's Backpack is licensed under CC BY-SA 4.0 (http://creativecommons.org/licenses/by-sa/4.0/)
//Source page of the project : https://niwala.itch.io/metaballs

using UnityEngine;

namespace SamsBackpack.Metaballs
{
    public static class ShaderProperties
    {
        public static int smoothing = Shader.PropertyToID("_Smoothing");
        public static int borderMode = Shader.PropertyToID("_BorderMode");
        public static int borderPower = Shader.PropertyToID("_BorderPower");
        public static int borderGradient = Shader.PropertyToID("_BorderGradient");

        public static int emitters = Shader.PropertyToID("_Emitters");
        public static int emitterCount = Shader.PropertyToID("_EmitterCount");

        public static int jumpFloodingStepSize = Shader.PropertyToID("_JumpFloodingStepSize");

        public static int areaCount = Shader.PropertyToID("_AreaCount");
        public static int areaRead = Shader.PropertyToID("_AreasRead");
        public static int areaWrite = Shader.PropertyToID("_AreasWrite");
        public static int areaColors = Shader.PropertyToID("_AreaColors");

        public static int result = Shader.PropertyToID("_Result");
        public static int renderData = Shader.PropertyToID("_RenderData");
        public static int mapSize = Shader.PropertyToID("_MapSize");
        public static int invMapSize = Shader.PropertyToID("_InvMapSize");
        public static int distanceFields = Shader.PropertyToID("_DistanceFields");
    }
}