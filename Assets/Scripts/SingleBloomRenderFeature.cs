using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SingleBloomRenderFeature : ScriptableRendererFeature
{
    public Material BloomMat;
    [Range(1,14)]
    public int BlitStep = 7;

    [Range(0.0f,1f)]
    public float LuminanceThreshole = 1.0f;

    class SingleBloomRenderPass : ScriptableRenderPass
    {
        Material bloomMat;
        int _blit_step = 7;
        float _LuminanceThreshole = 1.0f;

        readonly int PASS_DOWNSIZE_FIRST = 3;
        readonly int PASS_DOWNSIZE = 0;
        readonly int PASS_UPSIZE = 1;
        readonly int PASS_BLIT = 2;
        readonly int Shader_PreMip = Shader.PropertyToID("_PrewMip");
        readonly int Shader_LuminanceThreshole = Shader.PropertyToID("_luminanceThreshole");


        public void Setup(Material bloomMat,int blitStep,float LuminanceThreshole)
        {
            this.bloomMat = bloomMat;
            this._blit_step = blitStep;
            this._LuminanceThreshole = LuminanceThreshole;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SingleBloom");
            int width = renderingData.cameraData.cameraTargetDescriptor.width;
            int height = renderingData.cameraData.cameraTargetDescriptor.height;
            int downSize = 2;

            int N = this._blit_step;

            int[] RT_DownsizeIds = new int[N];
            int[] RT_UpsizeIds = new int[N - 1];

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.mipCount = 0;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            for (int i = 0; i < N; i++)
            {
                int w = width / downSize;
                int h = height / downSize;
                RenderTextureDescriptor desc = descriptor;
                desc.width = w;
                desc.height = h;
                RT_DownsizeIds[i] = Shader.PropertyToID("BloomDown" + i);
                cmd.GetTemporaryRT(RT_DownsizeIds[i], desc,FilterMode.Bilinear);
                if (i < N - 1)
                {
                    int up_i = RT_UpsizeIds.Length - i - 1;
                    RT_UpsizeIds[up_i] = Shader.PropertyToID("BloomUp" + up_i);
                    cmd.GetTemporaryRT(RT_UpsizeIds[up_i], desc,FilterMode.Bilinear);
                }

                downSize *= 2;
            }

            // downsize
            var source = renderingData.cameraData.renderer.cameraColorTarget;

            cmd.SetGlobalFloat(Shader_LuminanceThreshole, _LuminanceThreshole);
            cmd.Blit( SingleBloomPreRenderFeature.SingleBloomPreRenderPass._Bloom_Tex,RT_DownsizeIds[0],bloomMat,PASS_DOWNSIZE_FIRST);
            for (int i = 1; i < RT_DownsizeIds.Length;i++)
            {
                cmd.Blit(RT_DownsizeIds[i - 1], RT_DownsizeIds[i], bloomMat, PASS_DOWNSIZE);
            }

            cmd.SetGlobalTexture(Shader_PreMip, RT_DownsizeIds[N - 1]);
            cmd.Blit(RT_DownsizeIds[N - 2], RT_UpsizeIds[0], bloomMat, PASS_UPSIZE);
            for (int i = 1; i < N - 1; i++)
            {
                int pre_mip = RT_UpsizeIds[i - 1];
                int curr_mip = RT_DownsizeIds[N - 2 - i];
                cmd.SetGlobalTexture(Shader_PreMip, pre_mip);
                cmd.Blit(curr_mip, RT_UpsizeIds[i], bloomMat, PASS_UPSIZE);
            }

            cmd.Blit(RT_UpsizeIds[N - 2], source, bloomMat, PASS_BLIT);
            
            foreach (var id in RT_DownsizeIds)
            {
                cmd.ReleaseTemporaryRT(id);
            }

            foreach (var id in RT_UpsizeIds)
            {
                cmd.ReleaseTemporaryRT(id);
            }

            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();

            CommandBufferPool.Release(cmd);


        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
    
    SingleBloomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new SingleBloomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.Setup(this.BloomMat, this.BlitStep,this.LuminanceThreshole);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


