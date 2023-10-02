using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawNormal2DPass : ScriptableRenderPass
    {
        static readonly string k_NormalPass = "Normal2D Pass";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_NormalPass);
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");

        private class PassData
        {
            internal RendererListHandle rendererList;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new NotImplementedException();
        }

        private static void Execute(RasterCommandBuffer cmd, PassData passData)
        {
            CustomClear2D.Clear(cmd, RendererLighting.k_NormalClearColor);
            cmd.DrawRendererList(passData.rendererList);
        }

        public void Render(RenderGraph graph, ContextContainer frameData, Renderer2DData rendererData, ref LayerBatch layerBatch)
        {
            if (!layerBatch.lightStats.useNormalMap)
                return;

            Universal2DResourceData resourceData = frameData.Get<Universal2DResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            using (var builder = graph.AddRasterRenderPass<PassData>(k_NormalPass, out var passData, m_ProfilingSampler))
            {
                var filterSettings = new FilteringSettings();
                filterSettings.renderQueueRange = RenderQueueRange.all;
                filterSettings.layerMask = -1;
                filterSettings.renderingLayerMask = 0xFFFFFFFF;
                filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, layerBatch.layerRange.upperBound);

                var drawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                var sortSettings = drawSettings.sortingSettings;
                RendererLighting.GetTransparencySortingMode(rendererData, cameraData.camera, ref sortSettings);
                drawSettings.sortingSettings = sortSettings;

                builder.AllowPassCulling(false);
                builder.UseTextureFragment(resourceData.normalsTexture, 0);
                builder.UseTextureFragmentDepth(resourceData.intermediateDepth, IBaseRenderGraphBuilder.AccessFlags.Write);

                var param = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                passData.rendererList = graph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Execute(context.cmd, data);
                });
            }
        }
    }
}
