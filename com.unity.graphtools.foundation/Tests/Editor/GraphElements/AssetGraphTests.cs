using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.SubgraphTesting
{
    class AssetGraphTests : GraphViewTester
    {
        IGraphAsset m_ReferenceAssetGraph;
        IGraphAsset m_CurrentGraphAsset;

        const string k_ReferenceGraphName = "Reference Asset Graph";
        const string k_CurrentGraphName = "Current Graph";

        static IGraphAsset CreateGraph<T>(string graphName) where T : GraphAsset
        {
            var template = new GraphTemplate<ClassStencil>(graphName);
            var type = typeof(ClassStencil);
            var path = $"Assets/{graphName}.asset";

            // ReSharper disable once RedundantCast : needed in 2020.3
            return GraphAssetCreationHelpers<T>.CreateGraphAsset(type, graphName, path, template) as IGraphAsset;
        }

        static SearcherDatabase GetSearcherDatabaseWithAssetGraphs(IGraphAsset graphAsset)
        {
            return new GraphElementSearcherDatabase((ClassStencil)graphAsset.GraphModel.Stencil, graphAsset.GraphModel)
                .AddAssetGraphSubgraphs()
                .Build();
        }

        void CreateNodesAndValidateGraphModel(GraphNodeModelSearcherItem item, Action<List<INodeModel>> assertNodesCreation)
        {
            var initialNodes = m_CurrentGraphAsset.GraphModel.NodeModels.ToList();
            item.CreateElement.Invoke(new GraphNodeCreationData(m_CurrentGraphAsset.GraphModel, Vector2.zero));
            assertNodesCreation.Invoke(initialNodes);
        }

        static void CheckVariableAndPort(IVariableDeclarationModel variableDeclaration, IPortModel port)
        {
            Assert.AreEqual(variableDeclaration.Title, (port as IHasTitle)?.Title);
            Assert.AreEqual(variableDeclaration.Guid.ToString(), port.UniqueName);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_ReferenceAssetGraph = CreateGraph<AssetGraphAsset>(k_ReferenceGraphName);
            m_CurrentGraphAsset = CreateGraph<AssetGraphAsset>(k_CurrentGraphName);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            var assetPaths = AssetDatabase.FindAssets($"t:{typeof(AssetGraphAsset)} t:{typeof(ContainerGraphAsset)}").Select(AssetDatabase.GUIDToAssetPath);

            foreach (var assetPath in assetPaths)
                AssetDatabase.DeleteAsset(assetPath);
        }

        [Test]
        public void ShouldFindAssetGraphThatCanBeSubgraphInSearcher()
        {
            // AssetGraphAsset's condition to be a subgraph is that the graph model's name is "I can be a Subgraph" or that there is at least one input/output variable declaration"

            // First condition : Graph model name
            CreateGraph<AssetGraphAsset>("I can be a Subgraph");
            CreateGraph<AssetGraphAsset>("Bob");

            // Second condition: I/O variables
            var graph1 = CreateGraph<AssetGraphAsset>("Graph1");

            graph1.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.Read, true);
            graph1.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.Write, true);

            var graph2 = CreateGraph<AssetGraphAsset>("Graph2");

            graph2.GraphModel.CreateGraphVariableDeclaration(TypeHandle.ExecutionFlow, "Input Trigger", ModifierFlags.Read, true);
            graph2.GraphModel.CreateGraphVariableDeclaration(TypeHandle.ExecutionFlow, "Output Trigger", ModifierFlags.Write, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAsset);
            var results = searcherDatabase.Search("I can be a Subgraph");
            Assert.AreEqual(1, results.Count);

            results = searcherDatabase.Search("Bob");
            Assert.AreEqual(0, results.Count);

            results = searcherDatabase.Search("Graph1");
            Assert.AreEqual(1, results.Count);

            results = searcherDatabase.Search("Graph2");
            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void ShouldFindAssetGraphInItsOwnSearcher()
        {
            // Add I/O variables to the first reference asset graph model
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.Read, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.Write, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_ReferenceAssetGraph);

            var results = searcherDatabase.Search(k_ReferenceGraphName);

            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void ShouldNotFindContainerGraphInSearcher()
        {
            var containerGraphModel = CreateGraph<ContainerGraphAsset>("Container Graph");

            // Add I/O variables to container graph model
            containerGraphModel.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.Read, true);
            containerGraphModel.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.Write, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAsset);

            var results = searcherDatabase.Search("Container Graph");

            Assert.IsEmpty(results);
        }

        [Test]
        public void SubgraphNodeHasSameIOPortsAsReferenceAssetGraphIOVariables()
        {
            // Add data I/O variables to the reference asset graph model
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.Read, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.Write, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAsset);

            var results = searcherDatabase.Search(k_ReferenceGraphName);

            var item = (GraphNodeModelSearcherItem)results[0];

            CreateNodesAndValidateGraphModel(item, initialNodes =>
            {
                var node = m_CurrentGraphAsset.GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(initialNodes.Count + 1, m_CurrentGraphAsset.GraphModel.NodeModels.Count);
                Assert.AreEqual(k_ReferenceGraphName.Nicify(), node.DisplayTitle);
                Assert.AreEqual(m_ReferenceAssetGraph.GraphModel.VariableDeclarations.Count(v => v.IsInputOrOutput()), node.DataInputPortToVariableDeclarationDictionary.Count + node.DataOutputPortToVariableDeclarationDictionary.Count + node.ExecutionInputPortToVariableDeclarationDictionary.Count + node.ExecutionOutputPortToVariableDeclarationDictionary.Count);

                CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[0], node.InputsByDisplayOrder[0]);
                CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[1], node.OutputsByDisplayOrder[0]);
            });
        }

        [Test]
        public void RenameIOVariablesShouldRenamePorts()
        {
            // Add data I/O variables to the asset subgraph to make it discoverable in the searcher
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.Read, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.Write, true);

            // Create the subgraph node
            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAsset);
            var results = searcherDatabase.Search(k_ReferenceGraphName);
            var item = (GraphNodeModelSearcherItem)results[0];

            CreateNodesAndValidateGraphModel(item, initialNodes =>
            {
                var node = m_CurrentGraphAsset.GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(initialNodes.Count + 1, m_CurrentGraphAsset.GraphModel.NodeModels.Count);
                Assert.AreEqual(k_ReferenceGraphName.Nicify(), node.DisplayTitle);
                Assert.AreEqual(m_ReferenceAssetGraph.GraphModel.VariableDeclarations.Count(v => v.IsInputOrOutput()), node.DataInputPortToVariableDeclarationDictionary.Count + node.DataOutputPortToVariableDeclarationDictionary.Count + node.ExecutionInputPortToVariableDeclarationDictionary.Count + node.ExecutionOutputPortToVariableDeclarationDictionary.Count);
            });

            var subgraphNode = m_CurrentGraphAsset.GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.IsNotNull(subgraphNode);

            CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[0], subgraphNode.InputsByDisplayOrder[0]);
            CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[1], subgraphNode.OutputsByDisplayOrder[0]);

            // Rename the I/O variables in the subgraph
            m_ReferenceAssetGraph.GraphModel.VariableDeclarations[0].Title = "A";
            m_ReferenceAssetGraph.GraphModel.VariableDeclarations[1].Title = "B";

            // Load the main graph
            GraphView.Dispatch(new LoadGraphCommand(m_CurrentGraphAsset.GraphModel));

            CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[0], subgraphNode.InputsByDisplayOrder[0]);
            CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[1], subgraphNode.OutputsByDisplayOrder[0]);
        }

        [UnityTest]
        public IEnumerator DeleteIOVariablesShouldCreateMissingPorts()
        {
            // Add data I/O variables to the asset subgraph to make it discoverable in the searcher
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data 1", ModifierFlags.Read, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data 2", ModifierFlags.Read, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data 1", ModifierFlags.Write, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data 2", ModifierFlags.Write, true);

            // Create the subgraph node
            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAsset);
            var results = searcherDatabase.Search(k_ReferenceGraphName);
            var item = (GraphNodeModelSearcherItem)results[0];

            CreateNodesAndValidateGraphModel(item, initialNodes =>
            {
                var node = m_CurrentGraphAsset.GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(initialNodes.Count + 1, m_CurrentGraphAsset.GraphModel.NodeModels.Count);
                Assert.AreEqual(k_ReferenceGraphName.Nicify(), node.DisplayTitle);
                Assert.AreEqual(m_ReferenceAssetGraph.GraphModel.VariableDeclarations.Count(v => v.IsInputOrOutput()), node.DataInputPortToVariableDeclarationDictionary.Count + node.DataOutputPortToVariableDeclarationDictionary.Count + node.ExecutionInputPortToVariableDeclarationDictionary.Count + node.ExecutionOutputPortToVariableDeclarationDictionary.Count);
            });

            var subgraphNode = m_CurrentGraphAsset.GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
            Assert.IsNotNull(subgraphNode);

            var inputPort1 = subgraphNode.InputsByDisplayOrder[0];
            var inputPort2 = subgraphNode.InputsByDisplayOrder[1];
            var outputPort1 = subgraphNode.OutputsByDisplayOrder[0];
            var outputPort2 = subgraphNode.OutputsByDisplayOrder[1];

            Assert.IsNotNull(inputPort1);
            Assert.IsNotNull(inputPort2);
            Assert.IsNotNull(outputPort1);
            Assert.IsNotNull(outputPort2);

            var otherNode1 = m_CurrentGraphAsset.GraphModel.CreateNode<Type0FakeNodeModel>("OtherNode1", new Vector2(0, 0));
            var otherNode2 = m_CurrentGraphAsset.GraphModel.CreateNode<Type0FakeNodeModel>("OtherNode2", new Vector2(0, 0));

            m_CurrentGraphAsset.GraphModel.CreateEdge(inputPort1, otherNode1.Output0);
            m_CurrentGraphAsset.GraphModel.CreateEdge(otherNode2.Input0, outputPort1);

            // The corresponding ports' names on the subgraph node should be identical
            CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[0], inputPort1);
            CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[1], inputPort2);
            CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[2], outputPort1);
            CheckVariableAndPort(m_ReferenceAssetGraph.GraphModel.VariableDeclarations[3], outputPort2);

            Assert.AreEqual(PortType.Data, inputPort1.PortType);
            Assert.AreEqual(PortType.Data, inputPort2.PortType);
            Assert.AreEqual(PortType.Data, outputPort1.PortType);
            Assert.AreEqual(PortType.Data, outputPort2.PortType);

            // Delete the variables: Input Data 1 and Output Data 1
            m_ReferenceAssetGraph.GraphModel.DeleteVariableDeclarations(new[]
            {
                m_ReferenceAssetGraph.GraphModel.VariableDeclarations[0], // Input Data 1
                m_ReferenceAssetGraph.GraphModel.VariableDeclarations[2]  // Output Data 1
            });

            // Load the main graph
            GraphView.Dispatch(new LoadGraphCommand(m_CurrentGraphAsset.GraphModel));

            MarkGraphViewStateDirty();
            yield return null;

            // The corresponding ports' type on the subgraph node should be Missing Port
            Assert.AreEqual(PortType.MissingPort, inputPort1.PortType);
            Assert.AreEqual(PortType.MissingPort, outputPort1.PortType);

            // The other ports should stay of type Data
            Assert.AreEqual(PortType.Data, inputPort2.PortType);
            Assert.AreEqual(PortType.Data, outputPort2.PortType);
        }

        [Test]
        public void DeleteSubgraphWorks()
        {
            // Add data I/O variables to the asset subgraph to make it discoverable in the searcher
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "A", ModifierFlags.Read, true);
            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAsset);
            var results = searcherDatabase.Search(k_ReferenceGraphName);
            var item = (GraphNodeModelSearcherItem)results[0];

            // Create 3 subgraph nodes
            for (var i = 0; i < 3; i++)
            {
                CreateNodesAndValidateGraphModel(item, initialNodes =>
                {
                    var node = m_CurrentGraphAsset.GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
                    Assert.IsNotNull(node);
                    Assert.AreEqual(initialNodes.Count + 1, m_CurrentGraphAsset.GraphModel.NodeModels.Count);
                    Assert.AreEqual(k_ReferenceGraphName.Nicify(), node.DisplayTitle);
                    Assert.AreEqual(m_ReferenceAssetGraph.GraphModel.VariableDeclarations.Count(v => v.IsInputOrOutput()), node.DataInputPortToVariableDeclarationDictionary.Count + node.DataOutputPortToVariableDeclarationDictionary.Count + node.ExecutionInputPortToVariableDeclarationDictionary.Count + node.ExecutionOutputPortToVariableDeclarationDictionary.Count);
                });
            }

            var subgraphNodes = m_CurrentGraphAsset.GraphModel.NodeModels.OfType<SubgraphNodeModel>().ToList();
            foreach (var subgraphNode in subgraphNodes)
            {
                Assert.NotNull(subgraphNode);
                Assert.NotNull(subgraphNode.SubgraphModel);
                Assert.AreEqual(subgraphNode.SubgraphModel, m_ReferenceAssetGraph.GraphModel);
            }

            // Delete the subgraph asset
            Undo.DestroyObjectImmediate(m_ReferenceAssetGraph as GraphAsset);

            foreach (var subgraphNode in subgraphNodes)
                Assert.Null(subgraphNode.SubgraphModel);

            // Load the main graph
            GraphView.Dispatch(new LoadGraphCommand(m_CurrentGraphAsset.GraphModel));

            foreach (var subgraphNode in subgraphNodes)
            {
                Assert.Null(subgraphNode.SubgraphModel);
                Assert.AreNotEqual(subgraphNode.Title, k_ReferenceGraphName);
            }

            // Restore the deleted subgraph asset
            Undo.PerformUndo();

            foreach (var subgraphNode in subgraphNodes)
            {
                Assert.NotNull(subgraphNode.SubgraphModel);
                Assert.AreEqual(subgraphNode.Title, k_ReferenceGraphName);
            }
        }

        [Test]
        public void ShouldNotCreateSubgraphNodeWithContainerGraph()
        {
            var containerGraph = CreateGraph<ContainerGraphAsset>("Container Graph");
            Assert.True(containerGraph.GraphModel.IsContainerGraph());

            var subgraphNode = CreateSubgraphNode(containerGraph);
            Assert.IsNull(subgraphNode);
        }

        [Test]
        public void OpenCloseSubgraphOnUndoWorks()
        {
            var mainGraph = CreateGraph<AssetGraphAsset>("Main graph");
            var subgraph = CreateGraph<AssetGraphAsset>("Subgraph");

            GraphView.Dispatch(new LoadGraphCommand(subgraph.GraphModel, loadStrategy: LoadGraphCommand.LoadStrategies.PushOnStack));
            Window.GraphTool.Update();
            Assert.AreEqual(subgraph, Window.GraphTool.ToolState.CurrentGraph.GetGraphAsset());

            var stickyNote = GraphModel.CreateStickyNote(Rect.zero);
            GraphView.Dispatch(new MoveElementsCommand(new Vector2(1,1), stickyNote));

            GraphView.Dispatch(new LoadGraphCommand(mainGraph.GraphModel));
            Window.GraphTool.Update();

            Assert.AreEqual(mainGraph.GraphModel, GraphModel);

            Undo.PerformUndo();
            Assert.AreEqual(subgraph.GraphModel, GraphModel);
        }
    }
}
