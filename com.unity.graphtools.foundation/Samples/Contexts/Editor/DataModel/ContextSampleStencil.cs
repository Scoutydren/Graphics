using System;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    class ContextSampleStencil : Stencil
    {
        public static string GraphName => "Contexts";

        /// <inheritdoc />
        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return StencilHelper.IsCommonNodeThatCanBePasted(originalModel) || originalModel is SampleNodeModel;
        }

        public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            return originalModel is VariableDeclarationModel && originalModel.DataType == TypeHandle.Float;
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphModel graphModel)
        {
            return new BlackboardGraphModel { GraphModel = graphModel };
        }

        /// <inheritdoc />
        public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
        {
            return new InspectorModel(inspectedModel);
        }

        /// <inheritdoc />
        public override Type GetConstantType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantType(typeHandle);
        }

        /// <inheritdoc />
        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new ContextDatabaseProvider(this);
        }
    }
}
