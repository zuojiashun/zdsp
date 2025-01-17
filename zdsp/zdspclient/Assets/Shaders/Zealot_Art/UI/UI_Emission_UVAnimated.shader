// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.26 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.26;sub:START;pass:START;ps:flbk:,iptp:1,cusa:False,bamd:0,lico:0,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,vtps:0,hqsc:False,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:0,dpts:2,wrdp:False,dith:0,rfrpo:False,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:True,fgod:False,fgor:False,fgmd:0,fgcr:0,fgcg:0,fgcb:0,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:True,fnfb:False;n:type:ShaderForge.SFN_Final,id:4013,x:32823,y:32774,varname:node_4013,prsc:2|emission-1655-OUT;n:type:ShaderForge.SFN_Panner,id:3188,x:31962,y:32420,varname:node_3188,prsc:2,spu:0.5,spv:0.5|UVIN-7163-OUT,DIST-1339-OUT;n:type:ShaderForge.SFN_TexCoord,id:6687,x:31014,y:32767,varname:node_6687,prsc:2,uv:0;n:type:ShaderForge.SFN_Time,id:8454,x:31612,y:32599,varname:node_8454,prsc:2;n:type:ShaderForge.SFN_Slider,id:5129,x:31598,y:32778,ptovrint:False,ptlb:UVSpeed,ptin:_UVSpeed,varname:_UVSpeed,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:-10,cur:0,max:10;n:type:ShaderForge.SFN_Multiply,id:1339,x:31798,y:32564,varname:node_1339,prsc:2|A-8454-T,B-5129-OUT;n:type:ShaderForge.SFN_Color,id:8182,x:31997,y:32725,ptovrint:False,ptlb:EmissionColor,ptin:_EmissionColor,varname:_EmissionColor,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:0,c2:0,c3:0,c4:0;n:type:ShaderForge.SFN_Multiply,id:1655,x:32593,y:32770,varname:node_1655,prsc:2|A-3760-RGB,B-7544-OUT,C-8015-OUT,D-3238-RGB;n:type:ShaderForge.SFN_Tex2d,id:3760,x:32179,y:32505,ptovrint:False,ptlb:UVAnimatedTex,ptin:_UVAnimatedTex,varname:_UVAnimatedTex,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-3188-UVOUT;n:type:ShaderForge.SFN_Multiply,id:7544,x:32209,y:32713,varname:node_7544,prsc:2|A-8182-RGB,B-3146-OUT;n:type:ShaderForge.SFN_Vector1,id:3146,x:32022,y:32952,varname:node_3146,prsc:2,v1:5;n:type:ShaderForge.SFN_Multiply,id:7163,x:31668,y:32406,varname:node_7163,prsc:2|A-4840-OUT,B-8015-OUT;n:type:ShaderForge.SFN_Tex2d,id:3238,x:32189,y:32983,ptovrint:False,ptlb:EmissionTex,ptin:_EmissionTex,varname:_EmissionTex,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False;n:type:ShaderForge.SFN_RemapRange,id:9775,x:31178,y:32795,varname:node_9775,prsc:2,frmn:0,frmx:0.5,tomn:0.5,tomx:0|IN-6687-UVOUT;n:type:ShaderForge.SFN_Length,id:8015,x:31326,y:32840,varname:node_8015,prsc:2|IN-9775-OUT;n:type:ShaderForge.SFN_Vector1,id:4840,x:31380,y:32399,varname:node_4840,prsc:2,v1:1;proporder:3760-8182-5129-3238;pass:END;sub:END;*/

Shader "AAA_UI/UI_Emission_UVAnimated" {
    Properties {
        _UVAnimatedTex ("UVAnimatedTex", 2D) = "white" {}
        _EmissionColor ("EmissionColor", Color) = (0,0,0,0)
        _UVSpeed ("UVSpeed", Range(-10, 10)) = 0
        _EmissionTex ("EmissionTex", 2D) = "white" {}
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers d3d11_9x xbox360 xboxone ps3 ps4 psp2 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform float _UVSpeed;
            uniform float4 _EmissionColor;
            uniform sampler2D _UVAnimatedTex; uniform float4 _UVAnimatedTex_ST;
            uniform sampler2D _EmissionTex; uniform float4 _EmissionTex_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
////// Lighting:
////// Emissive:
                float4 node_8454 = _Time + _TimeEditor;
                float node_8015 = length((i.uv0*-1.0+0.5));
                float2 node_3188 = ((1.0*node_8015)+(node_8454.g*_UVSpeed)*float2(0.5,0.5));
                float4 _UVAnimatedTex_var = tex2D(_UVAnimatedTex,TRANSFORM_TEX(node_3188, _UVAnimatedTex));
                float4 _EmissionTex_var = tex2D(_EmissionTex,TRANSFORM_TEX(i.uv0, _EmissionTex));
                float3 emissive = (_UVAnimatedTex_var.rgb*(_EmissionColor.rgb*5.0)*node_8015*_EmissionTex_var.rgb);
                float3 finalColor = emissive;
                return fixed4(finalColor,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
