using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using System.Collections.Generic;
using System.IO;

public class GraphVisualizer
{
    public void GenerateGraph(List<ProjectInfo> projectInfos, string outputPath)
    {
        var graph = new AdjacencyGraph<string, Edge<string>>();

        foreach (var project in projectInfos)
        {
            graph.AddVertex(project.Name);

            foreach (var dependency in project.Dependencies)
            {
                graph.AddVertex(dependency);
                graph.AddEdge(new Edge<string>(project.Name, dependency));
            }
        }

        var graphviz = new GraphvizAlgorithm<string, Edge<string>>(graph);
        graphviz.FormatVertex += (sender, args) => args.VertexFormatter.Label = args.Vertex;
        var dotOutput = graphviz.Generate();

        File.WriteAllText(outputPath, dotOutput);
    }
}
