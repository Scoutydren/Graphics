using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class Vector2Node : IStandardNode
    {
        public static string Name = "Vector2";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"
    Out.x = X;
    Out.y = Y;
",
            new ParameterDescriptor("X", TYPE.Float, Usage.In),
            new ParameterDescriptor("Y", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Vector 2",
            tooltip: "Creates a user-defined value with 2 channels.",
            categories: new string[2] { "Input", "Basic" },
            synonyms: new string[4] { "2", "v2", "vec2", "float2" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "X",
                    tooltip: "the first component"
                ),
                new ParameterUIDescriptor(
                    name: "Y",
                    tooltip: "the second component"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a user-defined value with 2 channels"
                )
            }
        );
    }
}
