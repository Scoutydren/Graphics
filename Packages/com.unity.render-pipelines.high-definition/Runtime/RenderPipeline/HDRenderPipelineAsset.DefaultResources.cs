using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipelineAsset
    {
        #region Materials

#if UNITY_EDITOR
        private HDRenderPipelineEditorMaterials defaultMaterials => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorMaterials>();

        /// <summary>HDRP default material.</summary>
        public override Material defaultMaterial => defaultMaterials?.defaultMaterial;

        /// <summary>HDRP default Decal material.</summary>
        public Material GetDefaultDecalMaterial() => defaultMaterials?.defaultDecalMaterial;

        /// <summary>HDRP default mirror material.</summary>
        public Material GetDefaultMirrorMaterial() => defaultMaterials?.defaultMirrorMaterial;

        /// <summary>HDRP default particles material.</summary>
        public override Material defaultParticleMaterial => defaultMaterials?.defaultParticleMaterial;

        /// <summary>HDRP default terrain material.</summary>
        public override Material defaultTerrainMaterial => defaultMaterials?.defaultTerrainMaterial;
#endif

#endregion

        #region Shaders

        /// <summary>HDRP default shader.</summary>
        public override Shader defaultShader
        {
            get
            {
                if (globalSettings != null && globalSettings.renderPipelineResources != null)
                    return globalSettings.renderPipelineResources.shaders.defaultPS;

                return null;
            }
        }

#if UNITY_EDITOR

        private HDRenderPipelineEditorShaders defaultShaders => GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineEditorShaders>();

        #region Autodesk

        /// <summary>HDRP default autodesk interactive shader.</summary>
        public override Shader autodeskInteractiveShader => defaultShaders?.autodeskInteractiveShader;

        /// <summary>HDRP default autodesk interactive transparent shader.</summary>
        public override Shader autodeskInteractiveTransparentShader => defaultShaders?.autodeskInteractiveTransparentShader;

        /// <summary>HDRP default autodesk interactive masked shader.</summary>
        public override Shader autodeskInteractiveMaskedShader => defaultShaders?.autodeskInteractiveMaskedShader;

        #endregion

        #region SpeedTree

        /// <summary>HDRP default speed tree v8 shader</summary>
        public override Shader defaultSpeedTree8Shader => defaultShaders.defaultSpeedTree8Shader;

        #endregion

#endif

#endregion
    }
}
