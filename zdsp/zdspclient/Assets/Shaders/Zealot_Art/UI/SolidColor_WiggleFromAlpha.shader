// Shader created with Shader Forge v1.37 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.37;sub:START;pass:START;ps:flbk:Unlit/Color,iptp:1,cusa:True,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:False,nrmq:1,nrsp:0,vomd:0,spxs:True,tesm:0,olmd:1,culm:0,bsrc:0,bdst:7,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:False,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:1873,x:33613,y:32704,varname:node_1873,prsc:2|emission-1086-OUT,alpha-4805-A,clip-5376-A,voffset-3958-OUT;n:type:ShaderForge.SFN_Tex2d,id:4805,x:32699,y:32536,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:_MainTex,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:True,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Multiply,id:1086,x:33093,y:32739,cmnt:RGB,varname:node_1086,prsc:2|A-4805-A,B-5983-RGB,C-5376-RGB,D-5376-A;n:type:ShaderForge.SFN_Color,id:5983,x:32699,y:32727,ptovrint:False,ptlb:Color,ptin:_Color,varname:_Color,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_VertexColor,id:5376,x:32680,y:32876,varname:node_5376,prsc:2;n:type:ShaderForge.SFN_Time,id:4489,x:32498,y:33370,varname:node_4489,prsc:2;n:type:ShaderForge.SFN_Sin,id:2368,x:33053,y:33111,varname:node_2368,prsc:2|IN-7527-OUT;n:type:ShaderForge.SFN_Multiply,id:7527,x:32867,y:33111,varname:node_7527,prsc:2|A-9959-OUT,B-1109-OUT;n:type:ShaderForge.SFN_Noise,id:9959,x:32680,y:33111,varname:node_9959,prsc:2|XY-7041-UVOUT;n:type:ShaderForge.SFN_TexCoord,id:7041,x:32498,y:33111,varname:node_7041,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Divide,id:3958,x:33231,y:33111,cmnt:Slow down by,varname:node_3958,prsc:2|A-2368-OUT,B-7487-OUT;n:type:ShaderForge.SFN_Vector1,id:5472,x:32967,y:33316,cmnt:Strength,varname:node_5472,prsc:2,v1:20;n:type:ShaderForge.SFN_Multiply,id:1109,x:32680,y:33275,varname:node_1109,prsc:2|A-6458-OUT,B-4489-T;n:type:ShaderForge.SFN_Slider,id:7487,x:32853,y:33410,ptovrint:False,ptlb:Strength,ptin:_Strength,varname:_Strength,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:400,cur:50,max:2;n:type:ShaderForge.SFN_Slider,id:6458,x:32311,y:33286,ptovrint:False,ptlb:Speed,ptin:_Speed,varname:_Speed,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0.5,cur:20,max:50;proporder:4805-5983-7487-6458;pass:END;sub:END;*/

Shader "AAA_UI/Mesh/SolidColor_WiggleFromAlpha" {
    Properties {
        [PerRendererData]_MainTex ("MainTex", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Strength ("Strength", Range(400, 2)) = 50
        _Speed ("Speed", Range(0.5, 50)) = 20
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 n3ds wiiu 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float4 _Color;
            uniform float _Strength;
            uniform float _Speed;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                float2 node_9959_skew = o.uv0 + 0.2127+o.uv0.x*0.3713*o.uv0.y;
                float2 node_9959_rnd = 4.789*sin(489.123*(node_9959_skew));
                float node_9959 = frac(node_9959_rnd.x*node_9959_rnd.y*(1+node_9959_skew.x));
                float4 node_4489 = _Time + _TimeEditor;
                float node_3958 = (sin((node_9959*(_Speed*node_4489.g)))/_Strength); // Slow down by
                v.vertex.xyz += float3(node_3958,node_3958,node_3958);
                o.pos = UnityObjectToClipPos( v.vertex );
                #ifdef PIXELSNAP_ON
                    o.pos = UnityPixelSnap(o.pos);
                #endif
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                clip(i.vertexColor.a - 0.5);
////// Lighting:
////// Emissive:
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float3 emissive = (_MainTex_var.a*_Color.rgb*i.vertexColor.rgb*i.vertexColor.a);
                float3 finalColor = emissive;
                return fixed4(finalColor,_MainTex_var.a);
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Offset 1, 1
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 n3ds wiiu 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform float _Strength;
            uniform float _Speed;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
                float4 vertexColor : COLOR;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                float2 node_9959_skew = o.uv0 + 0.2127+o.uv0.x*0.3713*o.uv0.y;
                float2 node_9959_rnd = 4.789*sin(489.123*(node_9959_skew));
                float node_9959 = frac(node_9959_rnd.x*node_9959_rnd.y*(1+node_9959_skew.x));
                float4 node_4489 = _Time + _TimeEditor;
                float node_3958 = (sin((node_9959*(_Speed*node_4489.g)))/_Strength); // Slow down by
                v.vertex.xyz += float3(node_3958,node_3958,node_3958);
                o.pos = UnityObjectToClipPos( v.vertex );
                #ifdef PIXELSNAP_ON
                    o.pos = UnityPixelSnap(o.pos);
                #endif
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                clip(i.vertexColor.a - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
    CustomEditor "ShaderForgeMaterialInspector"
}
