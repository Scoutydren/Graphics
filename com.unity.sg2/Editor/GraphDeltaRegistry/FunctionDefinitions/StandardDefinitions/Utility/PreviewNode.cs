using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class PreviewNode : IStandardNode
    {
        public static string Name = "Preview";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = In;",
            new ParameterDescriptor("In", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a preview of the shader at a specific point in the graph.",
            categories: new string[1] { "Utility" },
            synonyms: new string[1] { "triangle wave" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the exact same value as the input"
                )
            }
        );
    }
}
