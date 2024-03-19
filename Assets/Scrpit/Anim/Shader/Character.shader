Shader "Unlit/Character"
{
    Properties
    {
        _SpriteQuadCount ("Sprite Quad数目", Int) = 1

        [NoScaleOffset] _EquipSpriteTex ("装备精灵贴图", 2D) = "white" {}
        _EquipIndexTexSizeData ("XY:装备索引贴图尺寸", Vector) = (1,1,1,1)
        _EquipIndexBufferIndex ("装备索引的索引(采样_EquipIndexTex)", Int) = 0

        _AnimTexSizeData ("XY:动画贴图尺寸", Vector) = (1,1,1,1)
        [NoScaleOffset] _PosAndScaleAnimTex ("位移缩放动画", 2D) = "black" {}
        [NoScaleOffset] _RotationRadiusAnimTex ("旋转角度动画", 2D) = "black" {}
        _AnimationIndex ("动画索引", Int) = 0
        _Surface ("test",float) = 1
        _AnimationStartTime ("动画开始时间", Float) = 0
        _BaseColor ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SpriteData
            {
                float4 TillOffset;
                float4 SizePivot;
            };

            struct CharacterPositionData
            {
                float2 Position;
                float2 Scale;
                float RotationRadius;
            };

            struct ClipInfo
            {
                int StartFrameTexIndex;
                int FrameCount;
                float Duration;
            };

            float _AnimationTime;

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float2 _AnimTexSizeData;
            float2 _EquipIndexTexSizeData;
            float _AnimationStartTime;
            int _SpriteQuadCount;
            int _AnimationIndex;
            int _EquipIndexBufferIndex;
            CBUFFER_END
            float _WorldPosButton;


            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                    UNITY_DOTS_INSTANCED_PROP(int, _AnimationIndex)
                    UNITY_DOTS_INSTANCED_PROP(int, _EquipIndexBufferIndex)
                    UNITY_DOTS_INSTANCED_PROP(float, _AnimationStartTime)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
                #define _AnimationIndex UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int, _AnimationIndex)
                #define _EquipIndexBufferIndex UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int, _EquipIndexBufferIndex)
                #define _AnimationStartTime UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _AnimationStartTime)
            #endif


            struct CharacterEquipInstanceIndex
            {
                int Instance;
            };

            StructuredBuffer<SpriteData> _EquipInfoBuffer;
            StructuredBuffer<ClipInfo> _AnimClipInfoBuffer;
            StructuredBuffer<CharacterEquipInstanceIndex> _EquipTexPosIdBuffer;
            StructuredBuffer<float4> _EquipColorBuffer;
            TEXTURE2D(_EquipSpriteTex);
            SAMPLER(sampler_EquipSpriteTex);
            TEXTURE2D(_PosAndScaleAnimTex);
            SAMPLER(sampler_PosAndScaleAnimTex);
            TEXTURE2D(_RotationRadiusAnimTex);
            SAMPLER(sampler_RotationRadiusAnimTex);

            CharacterPositionData SampleCharacterPositionData(int quadIndex)
            {
                ClipInfo clip_info = _AnimClipInfoBuffer[_AnimationIndex];


                CharacterPositionData characterPositionData;


                float indexX = 0.5;
                float indexY = quadIndex + .5f;

                if (clip_info.StartFrameTexIndex > 0)
                {
                    float anim_time = fmod(_AnimationTime - _AnimationStartTime, clip_info.Duration) / clip_info.Duration;
                    float frame = anim_time * (clip_info.FrameCount) + clip_info.StartFrameTexIndex + .5f;
                    indexY = int(frame / (_AnimTexSizeData.x)) * _SpriteQuadCount + quadIndex + .5f;
                    indexX = fmod(frame, _AnimTexSizeData.x);
                }

                float2 uv = float2(indexX / _AnimTexSizeData.x, indexY / _AnimTexSizeData.y);
                float4 posScale = SAMPLE_TEXTURE2D_LOD(_PosAndScaleAnimTex, sampler_PosAndScaleAnimTex, uv, 0);
                characterPositionData.Position = posScale.xy;
                characterPositionData.Scale = posScale.zw;
                characterPositionData.RotationRadius = -SAMPLE_TEXTURE2D_LOD(
                    _RotationRadiusAnimTex, sampler_RotationRadiusAnimTex, uv, 0).x;

                return characterPositionData;
            }

            struct EquipDataIndexAndColor
            {
                float4 EquipColor;
                uint EquipIndex;
            };

            EquipDataIndexAndColor GetEquipDataIndexAndColor(int spriteOffset)
            {
                EquipDataIndexAndColor equipDataIndexAndColor;
                // 根据bufferId 获取装备索引id
                uint equipId = _EquipTexPosIdBuffer[_EquipIndexBufferIndex * _SpriteQuadCount + spriteOffset].Instance;
                float4 equipColor = _EquipColorBuffer[_EquipIndexBufferIndex * _SpriteQuadCount + spriteOffset];

                if (equipId == 0)
                {
                    equipId = _EquipTexPosIdBuffer[spriteOffset].Instance;
                    equipColor = _EquipColorBuffer[spriteOffset];
                }

                equipDataIndexAndColor.EquipIndex = equipId - 1;
                equipDataIndexAndColor.EquipColor = equipColor;
                return equipDataIndexAndColor;
            }

            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                uint i = input.vertexID / 4;
                EquipDataIndexAndColor equipDataIndexAndColor = GetEquipDataIndexAndColor(i);
                int spriteDataIndex = equipDataIndexAndColor.EquipIndex;
                if (spriteDataIndex < 99999 && spriteDataIndex >= 0)
                {
                    SpriteData spriteData = _EquipInfoBuffer[spriteDataIndex];
                    CharacterPositionData characterPositionData = SampleCharacterPositionData(i);

                    // 这里主要是检测非法值
                    if (spriteData.SizePivot.x < 99999 && spriteData.SizePivot.x > -99999)
                    {
                        // pvoit 计算
                        input.vertex.xy = (input.vertex.xy + (.5 - spriteData.SizePivot.zw)) * spriteData.SizePivot.xy;

                        // 缩放计算
                        input.vertex.xy = input.vertex.xy * characterPositionData.Scale;
                        // 旋转计算
                        float c = cos(characterPositionData.RotationRadius);
                        float s = sin(characterPositionData.RotationRadius);
                        float2x2 rotationMatrix = float2x2(c, -s, s, c);
                        input.vertex.xy = mul(input.vertex.xy, rotationMatrix);
                        // 平移计算
                        input.vertex.xy = input.vertex.xy + characterPositionData.Position;
                        input.uv = input.uv * spriteData.TillOffset.zw + spriteData.TillOffset.xy;
                    }
                    else
                    {
                        input.vertex.xy = input.vertex.xy * 0.0f;
                    }
                }
                else if (spriteDataIndex < 0)
                {
                    input.vertex.xy = input.vertex.xy * 0.0f;
                }
                input.vertex.z = TransformObjectToWorld(0).y - _WorldPosButton;


                output.vertex = TransformObjectToHClip(input.vertex);


                output.uv = input.uv;
                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 col = SAMPLE_TEXTURE2D(_EquipSpriteTex, sampler_EquipSpriteTex, input.uv) * _BaseColor;
                clip(col.a - 0.01);
                return col;
            }
            ENDHLSL
        }
    }
}