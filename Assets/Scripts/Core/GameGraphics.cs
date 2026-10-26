using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace TreasureRunner
{
    public static class GameGraphics
    {
        private static UniversalRenderPipelineAsset comicPipeline;
        public static void Apply()
        {
            int quality = Mathf.Clamp(ProgressStore.Data.graphicsQuality,0,2);
            Application.targetFrameRate = quality == 0 ? 30 : 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = quality == 0 ? 0 : quality == 1 ? 2 : 4;
            var source=QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline;
            if(source is UniversalRenderPipelineAsset urp)
            {
                if(comicPipeline==null){comicPipeline=Object.Instantiate(urp);comicPipeline.name="TreasureRunner runtime comic quality";}
                comicPipeline.msaaSampleCount=quality==0 ? 1 : quality==1 ? 2 : 4;
                comicPipeline.renderScale=quality==0 ? .7f : quality==1 ? .9f : 1;
                QualitySettings.renderPipeline=comicPipeline;
            }
            QualitySettings.shadowDistance = quality == 0 ? 0 : quality == 1 ? 20 : 35;
            QualitySettings.lodBias = quality == 0 ? .65f : 1;
        }
    }
}
