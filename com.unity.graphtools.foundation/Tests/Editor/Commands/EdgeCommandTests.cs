using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [Category("Edge")]
    [Category("Commands")]
    public class EdgeCommandTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateEdgeCommand_OneEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    return new CreateEdgeCommand(node0.Input0, node1.Output0);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.ConnectedTo(node1.Output0));
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(0)));
                });
        }

        // no undo as it doesn't do anything
        [Test]
        public void Test_CreateEdgeCommand_Duplicate([Values(TestingMode.Command)] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            GraphModel.CreateEdge(node0.Input0, node1.Output0);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.ConnectedTo(node1.Output0));
                    return new CreateEdgeCommand(node0.Input0, node1.Output0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.ConnectedTo(node1.Output0));
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(0)));
                });
        }

        public enum ItemizeTestType
        {
            Enabled, Disabled
        }

        static IEnumerable<object[]> GetItemizeTestCases()
        {
            foreach (TestingMode testingMode in Enum.GetValues(typeof(TestingMode)))
            {
                // test both itemize option and non ItemizeTestType option
                foreach (ItemizeTestType itemizeTest in Enum.GetValues(typeof(ItemizeTestType)))
                {
                    yield return MakeItemizeTestCase(testingMode, itemizeTest,
                        graphModel =>
                        {
                            string name = "myInt";
                            var decl = graphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), name, ModifierFlags.None, true);
                            return graphModel.CreateVariableNode(decl, Vector2.zero);
                        }
                    );

                    yield return MakeItemizeTestCase(testingMode, itemizeTest,
                        graphModel =>
                        {
                            string name = "myInt";
                            return graphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), name, Vector2.zero);
                        });
                }
            }
        }

        static object[] MakeItemizeTestCase(TestingMode testingMode, ItemizeTestType itemizeTest, Func<IGraphModel, IInputOutputPortsNodeModel> makeNode)
        {
            return new object[] { testingMode, itemizeTest, makeNode };
        }

        [Test, TestCaseSource(nameof(GetItemizeTestCases))]
        public void Test_CreateEdgeCommand_Itemize(TestingMode testingMode, ItemizeTestType itemizeTest, Func<IGraphModel, IInputOutputPortsNodeModel> makeNode)
        {
            // create int node
            IInputOutputPortsNodeModel node0 = makeNode(GraphModel);

            var opNode = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);

            // connect int to first input
            GraphTool.Dispatch(new CreateEdgeCommand(opNode.Input0, node0.OutputsByDisplayOrder.First()));

            var prevItemizeConstants = Preferences.GetBool(BoolPref.AutoItemizeConstants);
            var prevItemizeVariables = Preferences.GetBool(BoolPref.AutoItemizeVariables);
            if (itemizeTest == ItemizeTestType.Enabled)
            {
                Preferences.SetBool(BoolPref.AutoItemizeConstants, true);
                Preferences.SetBool(BoolPref.AutoItemizeVariables, true);
            }
            else
            {
                Preferences.SetBool(BoolPref.AutoItemizeConstants, false);
                Preferences.SetBool(BoolPref.AutoItemizeVariables, false);
            }

            // test how the node reacts to getting connected a second time
            TestPrereqCommandPostreq(testingMode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref opNode);
                    var binOp = GraphModel.NodeModels.OfType<Type0FakeNodeModel>().First();
                    IPortModel input0 = binOp.Input0;
                    IPortModel input1 = binOp.Input1;
                    IPortModel binOutput = binOp.Output0;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(node0.OutputsByDisplayOrder.First()));
                    Assert.That(input1, Is.Not.ConnectedTo(node0.OutputsByDisplayOrder.First()));
                    Assert.That(binOutput.IsConnected, Is.False);
                    return new CreateEdgeCommand(input1, node0.OutputsByDisplayOrder.First());
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref opNode);
                    var binOp = GraphModel.NodeModels.OfType<Type0FakeNodeModel>().First();
                    IPortModel input0 = binOp.Input0;
                    IPortModel input1 = binOp.Input1;
                    IPortModel binOutput = binOp.Output0;
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.ConnectedTo(node0.OutputsByDisplayOrder.First()));
                    Assert.That(binOutput.IsConnected, Is.False);

                    if (itemizeTest == ItemizeTestType.Enabled)
                    {
                        Assert.That(GetNodeCount(), Is.EqualTo(3));
                        ISingleOutputPortNodeModel newNode = GetNode(2) as ISingleOutputPortNodeModel;
                        Assert.NotNull(newNode);
                        Assert.That(newNode, Is.TypeOf(node0.GetType()));
                        IPortModel output1 = newNode.OutputPort;
                        Assert.That(input1, Is.ConnectedTo(output1));
                        Assert.True(GraphTool.GraphViewSelectionState.IsSelected(newNode));
                        Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));

                    }
                    else
                    {
                        Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(1)));
                        Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                        Assert.That(GetNodeCount(), Is.EqualTo(2));
                    }
                });

            Preferences.SetBool(BoolPref.AutoItemizeConstants, prevItemizeConstants);
            Preferences.SetBool(BoolPref.AutoItemizeVariables, prevItemizeVariables);
        }

        [Test]
        public void Test_CreateEdgeCommand_ManyEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(input0, Is.Not.ConnectedTo(output0));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeCommand(input0, output0);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(0)));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeCommand(input1, output1);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(1)));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeCommand(input2, output2);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                    Assert.That(GraphTool.GraphViewSelectionState.GetSelection(GraphModel).Count, Is.EqualTo(1));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(2)));
                });
        }

        [Test]
        public void Test_CreateNodeFromOutputPort_NoNodeCreated([Values] TestingMode mode)
        {
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            GraphModel.CreateEdge(node2.Input0, node1.Output0);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    return CreateNodeCommand.OnPort(
                        new GraphNodeModelSearcherItem(new NodeSearcherItemData(typeof(int)), _ => null),
                        node1.Output0,
                        Vector2.down);
                },
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                });
        }

        [Test]
        public void Test_CreateNodeOnEdgeSide([Values] TestingMode mode)
        {
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            var node3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);
            const string newNodeName = "newNode";
            GraphModel.CreateEdge(node2.Input0, node1.Output0);
            GraphModel.CreateEdge(node3.Input0, node1.Output0);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node1);
                    RefreshReference(ref node2);
                    RefreshReference(ref node3);

                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(node1.Output0.IsConnectedTo(node2.Input0));
                    Assert.That(node1.Output0.IsConnectedTo(node3.Input0));

                    return CreateNodeCommand.OnEdgeSide(
                        new GraphNodeModelSearcherItem(new NodeSearcherItemData(typeof(int)), d => d.CreateNode(typeof(Type0FakeNodeModel), newNodeName)),
                        node1.Output0.GetConnectedEdges().Select(e => (e, EdgeSide.From)),
                        Vector2.down);
                },
                () =>
                {
                    var createdNode = GetAllNodes().OfType<Type0FakeNodeModel>().Single(n => n.Title == newNodeName);
                    Assert.IsNotNull(createdNode);

                    RefreshReference(ref node2);
                    RefreshReference(ref node3);

                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(createdNode.Output0.IsConnectedTo(node2.Input0));
                    Assert.That(createdNode.Output0.IsConnectedTo(node3.Input0));
                });
        }

        [Test]
        public void Test_CreateNodeFromOutputPort_NoConnection([Values] TestingMode testingMode)
        {
            var gedb = new GraphElementSearcherDatabase(Stencil, GraphModel);
            Type0FakeNodeModel.AddToSearcherDatabase(gedb);
            var db = gedb.Build();
            var item = (GraphNodeModelSearcherItem)db.Search(nameof(Type0FakeNodeModel))[0];

            var node0 = GraphModel.CreateNode<Type1FakeNodeModel>("Node0", Vector2.zero);
            var output0 = node0.Output;

            TestPrereqCommandPostreq(testingMode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return CreateNodeCommand.OnPort(item, output0, Vector2.down);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    var newNode = GetNode(1);
                    Assert.That(newNode, Is.TypeOf<Type0FakeNodeModel>());

                    var portModel = node0.OutputsByDisplayOrder.First();
                    Assert.That(portModel?.GetConnectedPorts().Count(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_CreateNodeFromOutputPort([Values] TestingMode testingMode)
        {
            var gedb = new GraphElementSearcherDatabase(Stencil, GraphModel);
            Type2FakeNodeModel.AddToSearcherDatabase(gedb);
            var db = gedb.Build();
            var item = (GraphNodeModelSearcherItem)db.Search(nameof(Type2FakeNodeModel))[0];

            var node0 = GraphModel.CreateNode<Type2FakeNodeModel>("Node0", Vector2.zero);

            TestPrereqCommandPostreq(testingMode,
                () =>
                {
                    RefreshReference(ref node0);
                    var output0 = node0.FloatOutput;
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return CreateNodeCommand.OnPort(item, output0, Vector2.down);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));

                    var newNode = GetNode(1) as NodeModel;
                    Assert.IsNotNull(newNode);
                    Assert.That(newNode, Is.TypeOf<Type2FakeNodeModel>());

                    var newEdge = GetEdge(0);
                    Assert.That(newEdge.ToPort.DataTypeHandle, Is.EqualTo(newEdge.FromPort.DataTypeHandle));

                    var portModel = node0.FloatOutput;
                    // Check that we connected to the second port (the one with the type we're looking for).
                    Assert.That(portModel.GetConnectedPorts().Single(), Is.EqualTo(newNode.InputsByDisplayOrder[1]));

                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(0)));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(1)));
                });
        }

        [Test]
        public void Test_CreateNodesFromInputSinglePort_TwoNodes([Values] TestingMode testingMode)
        {
            var gedb = new GraphElementSearcherDatabase(Stencil, GraphModel);
            Type3FakeNodeModel.AddToSearcherDatabase(gedb);
            var db = gedb.Build();
            var item = (GraphNodeModelSearcherItem)db.Search(nameof(Type3FakeNodeModel))[0];

            var node0 = GraphModel.CreateNode<Type3FakeNodeModel>("Node0", Vector2.zero);
            Assert.That(node0.Input.Capacity, Is.EqualTo(PortCapacity.Single));

            TestPrereqCommandPostreq(testingMode,
                () =>
                {
                    RefreshReference(ref node0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return CreateNodeCommand.OnPort(item, node0.Input, Vector2.down);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));

                    var node1 = GetNode(1) as Type3FakeNodeModel;
                    Assert.IsNotNull(node1);

                    var newEdge = GetEdge(0);
                    Assert.That(newEdge.ToPort.DataTypeHandle, Is.EqualTo(newEdge.FromPort.DataTypeHandle));

                    var portModel = node0.Input;
                    Assert.That(portModel.GetConnectedPorts().Single(), Is.EqualTo(node1.OutputsByDisplayOrder.First()));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(0)));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(1)));
                });

            TestPrereqCommandPostreq(testingMode,
                () =>
                {
                    RefreshReference(ref node0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    return CreateNodeCommand.OnPort(item, node0.Input, Vector2.down);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));

                    var node2 = GetNode(2) as Type3FakeNodeModel;
                    Assert.IsNotNull(node2);

                    var newEdge = GetEdge(0);
                    Assert.That(newEdge.ToPort.DataTypeHandle, Is.EqualTo(newEdge.FromPort.DataTypeHandle));

                    var portModel = node0.Input;
                    Assert.That(portModel.GetConnectedPorts().Count(), Is.EqualTo(1));
                    Assert.That(portModel.GetConnectedPorts().Single(), Is.EqualTo(node2.OutputsByDisplayOrder.First()));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetEdge(0)));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(2)));
                });
        }

        [Test]
        public void Test_DeleteElementsCommand_OneEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            var input0 = node0.Input0;
            var input1 = node0.Input1;
            var input2 = node0.Input2;
            var output0 = node1.Output0;
            var output1 = node1.Output1;
            var output2 = node1.Output2;
            var edge0 = GraphModel.CreateEdge(input0, output0);
            GraphModel.CreateEdge(input1, output1);
            GraphModel.CreateEdge(input2, output2);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    edge0 = GetEdge(0);
                    input0 = node0.Input0;
                    input1 = node0.Input1;
                    input2 = node0.Input2;
                    output0 = node1.Output0;
                    output1 = node1.Output1;
                    output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                    return new DeleteElementsCommand(edge0);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    RefreshReference(ref edge0);
                    input0 = node0.Input0;
                    input1 = node0.Input1;
                    input2 = node0.Input2;
                    output0 = node1.Output0;
                    output1 = node1.Output1;
                    output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.Not.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                });
        }

        [Test]
        public void Test_DeleteElementsCommand_ManyEdges([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            GraphModel.CreateEdge(node0.Input0, node1.Output0);
            GraphModel.CreateEdge(node0.Input1, node1.Output1);
            GraphModel.CreateEdge(node0.Input2, node1.Output2);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(node0.Input0, Is.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                    var edge0 = GraphModel.EdgeModels.First(e => e.ToPort.Equivalent(node0.Input0));
                    Assert.IsTrue(edge0.IsDeletable());
                    return new DeleteElementsCommand(edge0);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                    var edge1 = GraphModel.EdgeModels.First(e => e.ToPort.Equivalent(node0.Input1));
                    Assert.IsTrue(edge1.IsDeletable());
                    return new DeleteElementsCommand(edge1);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.Not.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.Not.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                    var edge2 = GraphModel.EdgeModels.First(e => e.ToPort.Equivalent(node0.Input2));
                    Assert.IsTrue(edge2.IsDeletable());
                    return new DeleteElementsCommand(edge2);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.Not.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.Not.ConnectedTo(node1.Output2));
                });
        }

        [Test]
        public void Test_SplitEdgeAndInsertNodeCommand([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateEdge(binary0.Input0, constant.OutputPort);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    var edge = GetEdge(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(binary0.Input0, Is.ConnectedTo(constant.OutputPort));
                    return new SplitEdgeAndInsertExistingNodeCommand(edge, binary1);
                },
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(binary1.Input0, Is.ConnectedTo(constant.OutputPort));
                    Assert.That(binary0.Input0, Is.ConnectedTo(binary1.Output0));
                });
        }

        [Test]
        public void TestCreateNodeOnEdge_BothPortsConnected([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "int", Vector2.zero);
            var unary = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var edge = GraphModel.CreateEdge(unary.Input0, constant.OutputPort);

            var gedb = new GraphElementSearcherDatabase(Stencil, GraphModel);
            Type0FakeNodeModel.AddToSearcherDatabase(gedb);
            var db = gedb.Build();
            var item = (GraphNodeModelSearcherItem)db.Search(nameof(Type0FakeNodeModel))[0];

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref unary);
                    RefreshReference(ref constant);
                    edge = GetEdge(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(unary.Input0, Is.ConnectedTo(constant.OutputPort));
                    return CreateNodeCommand.OnEdge(item, edge);
                },
                () =>
                {
                    RefreshReference(ref unary);
                    RefreshReference(ref constant);
                    RefreshReference(ref edge);
                    var unary2 = GraphModel.NodeModels.OfType<Type0FakeNodeModel>().ToList()[1];

                    Assert.IsNotNull(unary2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(unary2.Input0));
                    Assert.That(unary2.Output0, Is.ConnectedTo(unary.Input0));
                    Assert.IsFalse(GraphModel.EdgeModels.Contains(edge));
                    Assert.True(GraphTool.GraphViewSelectionState.IsSelected(GetNode(2)));
                }
            );
        }

        [Test]
        public void TestCreateNodeOnEdge_WithOutputNodeConnectedToUnknown([Values] TestingMode mode)
        {
            var constantNode = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "int1", Vector2.zero);
            var addNode = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateEdge(addNode.Input0, constantNode.OutputPort);
            GraphModel.CreateEdge(addNode.Input1, constantNode.OutputPort);

            var gedb = new GraphElementSearcherDatabase(Stencil, GraphModel);
            Type0FakeNodeModel.AddToSearcherDatabase(gedb);
            var db = gedb.Build();
            var item = (GraphNodeModelSearcherItem)db.Search(nameof(Type0FakeNodeModel))[0];

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref addNode);
                    RefreshReference(ref constantNode);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));

                    Assert.That(addNode, Is.Not.Null);
                    Assert.That(addNode.Input0, Is.ConnectedTo(constantNode.OutputPort));
                    var edge = GraphModel.EdgeModels.First();
                    return CreateNodeCommand.OnEdge(item, edge);
                },
                () =>
                {
                    RefreshReference(ref addNode);
                    RefreshReference(ref constantNode);
                    var multiplyNode = GraphModel.NodeModels.OfType<Type0FakeNodeModel>().ToList()[1];

                    Assert.IsNotNull(multiplyNode);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(constantNode.OutputPort, Is.ConnectedTo(multiplyNode.Input0));
                    Assert.That(multiplyNode.Output0, Is.ConnectedTo(addNode.Input0));
                    Assert.That(constantNode.OutputPort, Is.Not.ConnectedTo(addNode.Input0));
                }
            );
        }

        [Test]
        public void TestEdgeReorderCommand([Values] TestingMode mode, [Values] ReorderType reorderType)
        {
            var originNode = GraphModel.CreateNode<Type0FakeNodeModel>("Origin", Vector2.zero);
            var destNode1 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest1", Vector2.zero);
            var destNode2 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest2", Vector2.zero);
            var destNode3 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest3", Vector2.zero);
            var destNode4 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest4", Vector2.zero);
            var destNode5 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest5", Vector2.zero);

            var edge1 = GraphModel.CreateEdge(destNode1.ExeInput0, originNode.ExeOutput0);
            var edge2 = GraphModel.CreateEdge(destNode2.ExeInput0, originNode.ExeOutput0);
            var edge3 = GraphModel.CreateEdge(destNode3.ExeInput0, originNode.ExeOutput0);
            var edge4 = GraphModel.CreateEdge(destNode4.ExeInput0, originNode.ExeOutput0);
            var edge5 = GraphModel.CreateEdge(destNode5.ExeInput0, originNode.ExeOutput0);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref originNode);
                    RefreshReference(ref edge1);
                    RefreshReference(ref edge2);
                    RefreshReference(ref edge3);
                    RefreshReference(ref edge4);
                    RefreshReference(ref edge5);

                    var port = originNode.ExeOutput0 as IReorderableEdgesPortModel;
                    Assert.IsTrue(port?.HasReorderableEdges);
                    Assert.AreEqual(2, port.GetEdgeOrder(edge3));

                    return new ReorderEdgeCommand(edge3, reorderType);
                },
                () =>
                {
                    RefreshReference(ref originNode);
                    RefreshReference(ref edge1);
                    RefreshReference(ref edge2);
                    RefreshReference(ref edge3);
                    RefreshReference(ref edge4);
                    RefreshReference(ref edge5);

                    int expectedIdx;
                    switch (reorderType)
                    {
                        case ReorderType.MoveFirst:
                            expectedIdx = 0;
                            break;
                        case ReorderType.MoveUp:
                            expectedIdx = 1;
                            break;
                        case ReorderType.MoveDown:
                            expectedIdx = 3;
                            break;
                        case ReorderType.MoveLast:
                            expectedIdx = 4;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(reorderType), reorderType, "Unexpected value");
                    }
                    var port = originNode.ExeOutput0 as IReorderableEdgesPortModel;
                    Assert.IsTrue(port?.HasReorderableEdges);
                    Assert.AreEqual(expectedIdx, port.GetEdgeOrder(edge3));
                }
            );
        }

        [Test]
        public void TestEdgeReorderCommandWorksOnlyWithReorderableEdgePorts([Values] ReorderType reorderType)
        {
            var originNode = GraphModel.CreateNode<Type0FakeNodeModel>("Origin", Vector2.zero);
            var destNode1 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest1", Vector2.zero);
            var destNode2 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest2", Vector2.zero);
            var destNode3 = GraphModel.CreateNode<Type0FakeNodeModel>("Dest3", Vector2.zero);

            GraphModel.CreateEdge(destNode1.Input0, originNode.Output0);
            var edge2 = GraphModel.CreateEdge(destNode2.Input0, originNode.Output0);
            GraphModel.CreateEdge(destNode3.Input0, originNode.Output0);

            const int immutableIdx = 1;

            Assert.IsFalse(((PortModel)originNode.Output0)?.HasReorderableEdges ?? false);
            Assert.AreEqual(immutableIdx, GraphModel.EdgeModels.IndexOfInternal(edge2));

            GraphTool.Dispatch(new ReorderEdgeCommand(edge2, reorderType));

            // Nothing has changed.
            Assert.AreEqual(immutableIdx, GraphModel.EdgeModels.IndexOfInternal(edge2));
        }

        [Test]
        public void TestMoveEdgeCommandPreservesEdges([Values] TestingMode mode)
        {
            var originNode = GraphModel.CreateNode<Type0FakeNodeModel>("Origin", Vector2.zero);
            var destNode = GraphModel.CreateNode<Type0FakeNodeModel>("Dest", Vector2.zero);

            var edge1 = GraphModel.CreateEdge(destNode.Input0, originNode.Output0);
            var edge2 = GraphModel.CreateEdge(destNode.Input1, originNode.Output0);
            var label1 = "label1ToPreserve";
            var label2 = "label2ToPreserve";
            edge1.EdgeLabel = label1;
            edge2.EdgeLabel = label2;
            var edgeId1 = edge1.Guid;
            var edgeId2 = edge2.Guid;

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref originNode);
                    RefreshReference(ref destNode);

                    Assert.AreEqual(2, GraphModel.EdgeModels.Count);
                    edge1 = GraphModel.EdgeModels.Single(e => e.Guid == edgeId1);
                    edge2 = GraphModel.EdgeModels.Single(e => e.Guid == edgeId2);
                    Assert.AreEqual(originNode.Output0, edge1.FromPort);
                    Assert.AreEqual(originNode.Output0, edge2.FromPort);
                    Assert.AreEqual(destNode.Input0, edge1.ToPort);
                    Assert.AreEqual(destNode.Input1, edge2.ToPort);
                    Assert.AreEqual(label1, edge1.EdgeLabel);
                    Assert.AreEqual(label2, edge2.EdgeLabel);
                    return new MoveEdgeCommand(originNode.Output1, edge1, edge2);
                },
                () =>
                {
                    RefreshReference(ref originNode);
                    RefreshReference(ref destNode);

                    Assert.AreEqual(2, GraphModel.EdgeModels.Count);
                    edge1 = GraphModel.EdgeModels.Single(e => e.Guid == edgeId1);
                    edge2 = GraphModel.EdgeModels.Single(e => e.Guid == edgeId2);
                    Assert.AreEqual(originNode.Output1, edge1.FromPort);
                    Assert.AreEqual(originNode.Output1, edge2.FromPort);
                    Assert.AreEqual(destNode.Input0, edge1.ToPort);
                    Assert.AreEqual(destNode.Input1, edge2.ToPort);
                    Assert.AreEqual(label1, edge1.EdgeLabel);
                    Assert.AreEqual(label2, edge2.EdgeLabel);
                });
        }

        [TestCase(TestingMode.Command, PortDirection.Input, EdgeSide.To)]
        [TestCase(TestingMode.Command, PortDirection.Output, EdgeSide.From)]
        [TestCase(TestingMode.UndoRedo, PortDirection.Input, EdgeSide.To)]
        [TestCase(TestingMode.UndoRedo, PortDirection.Output, EdgeSide.From)]
        [Test]
        public void TestMoveEdgeCommandImplicitMovesCorrectSide(TestingMode mode, PortDirection destPortDirection, EdgeSide expectedMovedSide)
        {
            var originNode = GraphModel.CreateNode<Type0FakeNodeModel>("Origin", Vector2.zero);
            var destNode = GraphModel.CreateNode<Type0FakeNodeModel>("Dest", Vector2.zero);

            var edge = GraphModel.CreateEdge(destNode.Input0, originNode.Output0);
            var movingPortName = edge.GetPort(expectedMovedSide).UniqueName;
            var nonMovingPortName = edge.GetOtherPort(expectedMovedSide).UniqueName;

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref originNode);
                    RefreshReference(ref destNode);

                    var destPort = destPortDirection == PortDirection.Input ? destNode.Input1 : originNode.Output1;
                    Assert.AreEqual(1, GraphModel.EdgeModels.Count);
                    edge = GraphModel.EdgeModels[0];
                    Assert.AreEqual(originNode.Output0, edge.FromPort);
                    Assert.AreEqual(destNode.Input0, edge.ToPort);
                    Assert.AreNotEqual(edge.GetPort(expectedMovedSide), destPort);
                    return new MoveEdgeCommand(destPort, edge);
                },
                () =>
                {
                    RefreshReference(ref originNode);
                    RefreshReference(ref destNode);

                    var destPort = destPortDirection == PortDirection.Input ? destNode.Input1 : originNode.Output1;
                    Assert.AreEqual(1, GraphModel.EdgeModels.Count);
                    edge = GraphModel.EdgeModels[0];
                    Assert.AreNotEqual(edge.GetPort(expectedMovedSide).UniqueName, movingPortName);
                    Assert.AreEqual(edge.GetOtherPort(expectedMovedSide).UniqueName, nonMovingPortName);
                    Assert.AreEqual(edge.GetPort(expectedMovedSide), destPort);
                });
        }

        [TestCase(TestingMode.Command, EdgeSide.To)]
        [TestCase(TestingMode.Command, EdgeSide.From)]
        [TestCase(TestingMode.UndoRedo, EdgeSide.To)]
        [TestCase(TestingMode.UndoRedo, EdgeSide.From)]
        [Test]
        public void TestMoveEdgeCommandExplicitMovesCorrectSide(TestingMode mode, EdgeSide expectedMovedSide)
        {
            var originNode = GraphModel.CreateNode<Type0FakeNodeModel>("Origin", Vector2.zero);
            var destNode = GraphModel.CreateNode<Type0FakeNodeModel>("Dest", Vector2.zero);

            var edge = GraphModel.CreateEdge(destNode.Input0, originNode.Output0);
            var movingPortName = edge.GetPort(expectedMovedSide).UniqueName;
            var nonMovingPortName = edge.GetOtherPort(expectedMovedSide).UniqueName;

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshReference(ref originNode);
                    RefreshReference(ref destNode);

                    var destPort = expectedMovedSide == EdgeSide.To ? destNode.Input1 : originNode.Output1;
                    Assert.AreEqual(1, GraphModel.EdgeModels.Count);
                    edge = GraphModel.EdgeModels[0];
                    Assert.AreEqual(originNode.Output0, edge.FromPort);
                    Assert.AreEqual(destNode.Input0, edge.ToPort);
                    Assert.AreNotEqual(edge.GetPort(expectedMovedSide), destPort);
                    return new MoveEdgeCommand(destPort, expectedMovedSide, edge);
                },
                () =>
                {
                    RefreshReference(ref originNode);
                    RefreshReference(ref destNode);

                    var destPort = expectedMovedSide == EdgeSide.To ? destNode.Input1 : originNode.Output1;
                    Assert.AreEqual(1, GraphModel.EdgeModels.Count);
                    edge = GraphModel.EdgeModels[0];
                    Assert.AreNotEqual(edge.GetPort(expectedMovedSide).UniqueName, movingPortName);
                    Assert.AreEqual(edge.GetOtherPort(expectedMovedSide).UniqueName, nonMovingPortName);
                    Assert.AreEqual(edge.GetPort(expectedMovedSide), destPort);
                });
        }
    }
}
