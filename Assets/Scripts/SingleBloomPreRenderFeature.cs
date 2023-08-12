using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class SingleBloomPreRenderFeature : ScriptableRendererFeature
{
    public class SingleBloomPreRenderPass : ScriptableRenderPass
    {
        FilteringSettings m_FilteringSettings;
        public static readonly int _Bloom_Tex = Shader.PropertyToID("_BloomBlitTex");

        bool m_IsOpaque = false;

        ProfilingSampler m_ProfilingSampler;
        RenderStateBlock m_RenderStateBlock;

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>()
        {
            new ShaderTagId("SingleBloom"),
        };

        public SingleBloomPreRenderPass(RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            m_ProfilingSampler = new ProfilingSampler("SingleBloomPreBlit");
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var colorAttachment = renderingData.cameraData.renderer.cameraColorTarget;
            var depthAttachment = renderingData.cameraData.renderer.cameraDepthTarget;
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            cmd.GetTemporaryRT(_Bloom_Tex, descriptor);

            // 为了某些平台闪烁，需要清空
            cmd.SetRenderTarget(_Bloom_Tex);
            cmd.ClearRenderTarget(false,true,Color.black);

            ConfigureTarget(new RenderTargetIdentifier[] { colorAttachment, _Bloom_Tex }, depthAttachment);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {

                Camera camera = renderingData.cameraData.camera;
                var sortFlags = (m_IsOpaque) ? renderingData.cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
               
                var filterSettings = m_FilteringSettings;

#if UNITY_EDITOR
                // When rendering the preview camera, we want the layer mask to be forced to Everything
                if (renderingData.cameraData.isPreviewCamera)
                {
                    filterSettings.layerMask = -1;
                }
#endif

                DrawingSettings drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortFlags);

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings, ref m_RenderStateBlock);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_Bloom_Tex);
        }
    }

    SingleBloomPreRenderPass m_ScriptablePass;

    public bool IsOpaque = true;
    public LayerMask layerMask;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new SingleBloomPreRenderPass(IsOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,layerMask);

        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


