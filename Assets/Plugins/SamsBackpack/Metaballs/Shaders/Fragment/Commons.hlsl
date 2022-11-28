#define MaxValue 999
#pragma target 5.0

//Structs
struct Emitter
{
    float3 position;
    float radius;
    int channel;
};

struct Area
{
    float distance;
    float2 coords;
    int id;
};

//Properties
StructuredBuffer<Emitter> _Emitters;		//Contains the information of all emitters that must be written in the texture.
int _EmitterCount;							//

StructuredBuffer<float4> _AreaColors;		//
int _AreaCount;								//

RWStructuredBuffer<float> _DistanceFields;	//Distance field for each arena.
StructuredBuffer<Area> _AreasRead;			//Flattened version of all distance fields. (Read only)
RWStructuredBuffer<Area> _AreasWrite;		//Flattened version of all distance fields. (Write only)

float _Smoothing;							//Smoothing factor between the different emitters
int _BorderMode;							//0 = NoBorder, 1 = GroupAllAreas, 2 = SplitAreas
float _BorderPower;							//
int _JumpFloodingStepSize;					//The pixel size of the jump for JumpFlooding.
Texture2D<float> _BorderGradient;			//1D texture containing a gradient to draw the edges of the areas.
SamplerState linear_clamp_sampler;			//

int _MapSize;								//The size of the computed texture
float2 _InvMapSize;							//1.0 / The size of the computed texture
RWTexture2D<float4> _Result;				//The final result            

#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};
//
////Borders
//int _BorderMode;
//float _BorderPower;
//Texture2D _BorderGradient;
//
////Data
//Texture2D _RenderData;
//SamplerState sampler_linear_clamp;
//StructuredBuffer<float4> _AreaColors;

v2f vert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}