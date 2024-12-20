namespace Program;

using CommandLine;
using Microsoft.CodeAnalysis;
using Spectre.Console;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Locator;
using Types;

public enum RequestSelectionChoices {
    Get,
    Update,
    Insert,
    Upsert,
    Delete,
}

public enum ProjectType {
    Client,
    Dao,
    Microservice,
    Api,
}

public class ProjectSelection(Project project) {
    public Project Project = project;

    public override string ToString() {
        return Project.Name;
    }
}

public class ClientProject {
    public string? Model  { get; set; }
    public string? Dao { get; set; }
}

public class ConnectedProjects {
    public List<ClientProject>? ApiClients { get; set; }
    public string? Model { get; set; }
    public string? Dao { get; set; }
}

public class Program {
    public class Options {
            private string? _SolPath;
            /// <summary>
            /// Valid absolute path to .sln file
            /// </summary>
            [Option('s', "solution-path", Required = true, HelpText = "The relative/absolute path to your .sln file")]
            public required string SolPath {
                get { return _SolPath!; }
                set {
                    var cwd = Environment.CurrentDirectory;
                    var absolute_file_path = Path.Join(cwd, value);
                    
                    // Use provided file path, if none then attempt to find .sln in cwd
                    if(File.Exists(absolute_file_path)) {
                        _SolPath = absolute_file_path;
                    } else {
                        var sln_file = Directory.GetFiles(cwd).First(x => x.EndsWith(".sln"));
                        if(sln_file is not null) {
                            _SolPath = sln_file;
                        } else {
                            AnsiConsole.Markup(
                                $"[red]Could not parse '{absolute_file_path}'" +
                                "as a valid file path[/]",
                                Console.Error
                            );
                            Environment.Exit(1);
                        }
                    }
                }
            }

        [Option('p', "project-path", Required = false, HelpText = "Name of the project to modify")]
        public string? ProjName { get; set; }
    }
    static async Task<int> Main(string[] args) {
        AnsiConsole.Markup(@"[blue]
  _____  _  _     _____           _           _     ____        _ _     _
 / ____|| || |_  |  __ \         (_)         | |   |  _ \      (_) |   | |
| |   |_  __  _| | |__) | __ ___  _  ___  ___| |_  | |_) |_   _ _| | __| | ___ _ __
| |    _| || |_  |  ___/ '__/ _ \| |/ _ \/ __| __| |  _ <| | | | | |/ _` |/ _ \ '__|
| |___|_  __  _| | |   | | | (_) | |  __/ (__| |_  | |_) | |_| | | | (_| |  __/ |
 \_____||_||_|   |_|   |_|  \___/| |\___|\___|\__| |____/ \__,_|_|_|\__,_|\___|_|
                                _/ |
                               |__/[/]");
        Console.WriteLine("");
        ParserResult<Options>? op;
        try {
            op = Parser.Default.ParseArguments<Options>(args);
        } catch(Exception e) {
            AnsiConsole.WriteException(e);
            return -1;
        }
        if(op is null) {
            return 1;
        }
        var options = op.Value;

        MSBuildLocator.RegisterDefaults();

        var workspace = MSBuildWorkspace.Create();

        var solTask = workspace.OpenSolutionAsync(options.SolPath);

        var reqType = AnsiConsole.Prompt(
            new SelectionPrompt<RequestSelectionChoices>()
                .Title("Choose from one of the below endpoint types")
                .PageSize(10)
                .AddChoices([
                    RequestSelectionChoices.Get,
                    RequestSelectionChoices.Update,
                    RequestSelectionChoices.Insert,
                    RequestSelectionChoices.Upsert,
                    RequestSelectionChoices.Delete,
                ])
        );
        var sol = (await solTask).Expect("Fatal error, could not open solution");
        
        // Do not selected any projects that are dlls
        var projSelection = sol.Projects
            .Where(
                x => x.CompilationOptions?.OutputKind != OutputKind.DynamicallyLinkedLibrary
            )
            .Select(x => new ProjectSelection(x))
            .ToList();

        var project = AnsiConsole.Prompt(
            new SelectionPrompt<ProjectSelection>()
                .Title("Choose what project needs and endpoint added")
                .EnableSearch()
                .PageSize(15)
                .AddChoices(
                    projSelection
                )
        );

        var projType = DetermineProjectType(project.Project);

        if(projType == ProjectType.Dao) {

        }

        var depGraph = sol.GetProjectDependencyGraph();

        depGraph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Project.Id);
        var projects = project.Project.ProjectReferences.Select(
            x => sol.Projects.Where(z => z.Id == z.Id).First()
        );


        Console.WriteLine("");
        Console.WriteLine("");
        Console.WriteLine(string.Join(",", projects.Select(x => x.FilePath)));

        return 0;
    }
    private static ProjectType DetermineProjectType(Project proj) {
        return proj.Name.ToLower() switch {
            var name when name.Contains("dao") | name.Contains("datalayer") => ProjectType.Dao,
            var name when name.Contains("api") => ProjectType.Api,
            var name when name.Contains("microservice") => ProjectType.Microservice,
            var name when name.Contains("client") => ProjectType.Client,
            _ => PromptForProjectType()
        };
    }
    private static ProjectType PromptForProjectType() {
        AnsiConsole.Markup("[yellow]Unable to detect project type[/]");
        Console.WriteLine("");
        return AnsiConsole.Prompt(
            new SelectionPrompt<ProjectType>()
                .Title("Choose from one of the below project types")
                .PageSize(10)
                .AddChoices([
                    ProjectType.Api,
                    ProjectType.Client,
                    ProjectType.Dao,
                    ProjectType.Microservice,
                ])
        );
    }
}