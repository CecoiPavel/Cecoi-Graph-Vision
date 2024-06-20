using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Locator;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Project = EnvDTE.Project;

public class SolutionScanner
{
    private readonly DTE2 _dte;

    public SolutionScanner(DTE2 dte)
    {
        _dte = dte;
    }

    public async Task<List<ProjectInfo>> ScanSolutionAsync()
    {
        // Register MSBuild instance
        MSBuildLocator.RegisterDefaults();

        var solution = _dte.Solution;
        var projects = GetAllProjects(solution);
        var projectInfoList = new List<ProjectInfo>();

        foreach (Project project in projects)
        {
            var projectInfo = await ScanProjectAsync(project);
            projectInfoList.Add(projectInfo);
        }

        return projectInfoList;
    }

    private IEnumerable<Project> GetAllProjects(Solution solution)
    {
        var projects = new List<Project>();

        foreach (Project project in solution.Projects)
        {
            projects.Add(project);
            GetSubProjects(project, projects);
        }

        return projects;
    }

    private void GetSubProjects(Project project, List<Project> projects)
    {
        if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
        {
            foreach (ProjectItem item in project.ProjectItems)
            {
                var subProject = item.SubProject;
                if (subProject != null)
                {
                    projects.Add(subProject);
                    GetSubProjects(subProject, projects);
                }
            }
        }
    }

    private async Task<ProjectInfo> ScanProjectAsync(Project project)
    {
        var projectInfo = new ProjectInfo
        {
            Name = project.Name,
            FilePath = project.FullName,
            Dependencies = new List<string>()
        };

        using (var workspace = MSBuildWorkspace.Create())
        {
            var roslynProject = await workspace.OpenProjectAsync(project.FullName);
            var compilation = await roslynProject.GetCompilationAsync();

            foreach (var reference in roslynProject.MetadataReferences)
            {
                projectInfo.Dependencies.Add(reference.Display);
            }

            foreach (var document in roslynProject.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                var root = await syntaxTree.GetRootAsync();
                var classes = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>();

                foreach (var classNode in classes)
                {
                    projectInfo.Classes.Add(new ClassInfo { Name = classNode.Identifier.Text });
                }
            }
        }

        return projectInfo;
    }
}

public class ProjectInfo
{
    public string Name { get; set; }
    public string FilePath { get; set; }
    public List<string> Dependencies { get; set; } = new List<string>();
    public List<ClassInfo> Classes { get; set; } = new List<ClassInfo>();
}

public class ClassInfo
{
    public string Name { get; set; }
}
