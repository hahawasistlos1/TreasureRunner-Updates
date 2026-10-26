Shader "TreasureRunner/Comic Surface"
{
 Properties { [MainColor] _BaseColor("Color",Color)=(1,1,1,1) [MainTexture] _BaseMap("Texture",2D)="white" {} _Cutoff("Cutoff",Range(0,1))=0.1 _Rim("Edge light",Range(0,1))=0.15 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   Name "ComicForward" Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor; float4 _BaseMap_ST; half _Cutoff;half _Rim;
   CBUFFER_END
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   struct Attributes { float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3; };
   Varyings vert(Attributes v)
   {
    Varyings o;VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
    o.positionCS=p.positionCS;o.world=p.positionWS;o.normal=TransformObjectToWorldNormal(v.normalOS);
    o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;
   }
   half4 frag(Varyings i):SV_Target
   {
    half4 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(base.a-_Cutoff);
    half3 n=normalize(i.normal);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    half lit=saturate(dot(n,sun.direction));half band=lit>.65 ? 1.0 : lit>.18 ? .82 : .62;
    half shadow=lerp(.73,1,sun.shadowAttenuation);
    half rim=pow(1-saturate(dot(n,GetWorldSpaceNormalizeViewDir(i.world))),3)*_Rim;
    half pigment=.975+.025*sin(i.world.x*2.1+i.world.y*3.7+sin(i.world.z*4));
    half3 color=base.rgb*band*shadow*pigment+base.rgb*half3(.16,.2,.3)*rim;
    return half4(MixFog(color,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
 Fallback "Universal Render Pipeline/Simple Lit"
}
