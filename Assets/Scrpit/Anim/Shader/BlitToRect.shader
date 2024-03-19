Shader "Custom/BlitToRect"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
        _Rect ("Rect", Vector) = (0, 0, 1, 1)
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"


            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 srcUV : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _SrcTex_ST;
            float4 _Rect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.srcUV = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 检查当前像素是否在目标区域内
                if (i.srcUV.x >= _Rect.x && i.srcUV.x <= (_Rect.x + _Rect.z) &&
                    i.srcUV.y >= _Rect.y && i.srcUV.y <= (_Rect.y + _Rect.w))
                {
                    // 转化rect 到 0 1
                    float2 uv = (i.srcUV - _Rect.xy) / _Rect.zw;
                    float2 resuv = uv * _SrcTex_ST.zw + _SrcTex_ST.xy;
                    return float4(tex2D(_MainTex, resuv));
                }
                else
                {
                    clip(-1);
                }
                return 1;
            }
            ENDCG
        }
    }
}