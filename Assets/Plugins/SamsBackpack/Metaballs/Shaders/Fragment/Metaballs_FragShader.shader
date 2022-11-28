//Metaballs © 2022 by Sam's Backpack is licensed under CC BY-SA 4.0 (http://creativecommons.org/licenses/by-sa/4.0/)
//Source page of the project : https://niwala.itch.io/metaballs

Shader "hidden/Metaballs_FragShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        //Clear
        Pass
        {
            CGPROGRAM
            #include "Commons.hlsl"

            half4 frag(v2f input) : SV_Target
            {
                for (int j = 0; j < _AreaCount; j++)
                {
                    uint3 id = uint3(input.uv * _MapSize, j);
                    int i = id.z * _MapSize * _MapSize + id.x * _MapSize + id.y;
                    _DistanceFields[i] = MaxValue;
                }
                return 0;
            }
            ENDCG
        }

        //Assign area
        Pass
        {
            CGPROGRAM
            #include "Commons.hlsl"

            //Exponential smooth min Inigo Quilez - https://iquilezles.org/articles/smin/
            float smin(float a, float b, float k)
            {
                float res = exp2(-k * a) + exp2(-k * b);
                return -log2(res) / k;
            }

            half4 frag(v2f input) : SV_Target
            {
                float2 position = input.uv * 2.0 - 1.0;
                uint2 id = uint2(input.uv * _MapSize);

                for (int j = 0; j < _EmitterCount; j++)
                {
                    Emitter emitter = _Emitters[j];
                    float dist = distance(position, emitter.position.xz);
                    float factor = dist - emitter.radius;
                    int i = emitter.channel * _MapSize * _MapSize + id.x * _MapSize + id.y;
                    _DistanceFields[i] = smin(_DistanceFields[i], factor, _Smoothing);
                }
                return 0;
            }
            ENDCG
        }
                
        //Flatten
        Pass
        {
            CGPROGRAM
            #include "Commons.hlsl"

            half4 frag(v2f input) : SV_Target
            {
                uint2 id = uint2(input.uv * _MapSize);
                float distance = 1;
                int channel = -1;
                int i = id.x * _MapSize + id.y;

                for (int j = 0; j < _AreaCount; j++)
                {
                    int i = j * _MapSize * _MapSize + id.x * _MapSize + id.y;
                    if (_DistanceFields[i].x < distance)
                    {
                        distance = _DistanceFields[i];
                        channel = j;
                    }
                }

                Area area;
                area.id = channel;
                if (distance < 0)
                {
                    area.coords = MaxValue;
                    area.distance = MaxValue;
                }
                else
                {
                    area.coords = id.xy * _InvMapSize;
                    area.distance = 0.0;
                }
                _AreasWrite[i] = area;
                return 0;
            }
            ENDCG
        }

        //Jump flooding
        Pass
        {
            CGPROGRAM
            #include "Commons.hlsl"
            
            half4 frag(v2f input) : SV_Target
            {
                int2 pos = int2(input.uv * _MapSize);
                int i = pos.x * _MapSize + pos.y;
                Area current = _AreasRead[i];
                float2 center = float2(pos * _InvMapSize);
                current.distance = distance(current.coords, center);


                if (current.distance < 0)
                    return 0;

                for (int y = -1; y <= 1; ++y)
                {
                    int posY = pos.y + y * _JumpFloodingStepSize;
                    #ifndef RenderTexBorders 
                    if (posY < 0 || posY >= _MapSize)
                        continue;
                    #endif

                    for (int x = -1; x <= 1; ++x)
                    {
                        int posX = pos.x + x * _JumpFloodingStepSize;
                        #ifndef RenderTexBorders 
                        if (posX < 0 || posX >= _MapSize)
                            continue;
                        #endif

                        Area neighbor = _AreasRead[posX * _MapSize + posY];
                        float d = distance(neighbor.coords, center);

                        //Add borders between areas
                        if (_BorderMode == 2 && neighbor.id != current.id)
                        {
                            neighbor.coords = float2(posX, posY) * _InvMapSize;
                            d = 0.001;
                        }

                        //Add border on the edge of the render texture
                        #ifdef RenderTexBorders
                        if (min(posX, posY) < 0 || max(posX, posY) >= _MapSize)
                        {
                            neighbor.coords = float2(posX, posY) * _InvMapSize;
                            //d = 0;
                        }
                        #endif


                        if (d < current.distance)
                        {
                            current.distance = d;
                            current.coords = neighbor.coords;
                        }
                    }
                }

                _AreasWrite[i] = current;
                return 0;
            }
            ENDCG
        }

        //Render
        Pass
        {
            CGPROGRAM
            #include "Commons.hlsl"
            
            half4 frag(v2f input) : SV_Target
            {
                //Get area color & shape
                uint2 id = uint2(input.uv * _MapSize);
                Area area = _AreasRead[id.x * _MapSize + id.y];
                float4 color = _AreaColors[area.id];
                float alpha = step(0.0001, area.distance);
                color.a *= alpha;

                //Add borders
                if (_BorderMode != 0)
                {
                    float borders = _BorderGradient.SampleLevel(linear_clamp_sampler, pow(abs(area.distance), _BorderPower), 0).x;
                    color.a *= borders;
                }

                //Return result
                _Result[id.xy] = color;
                return 0;
            }
            ENDCG
        }

    }
}
