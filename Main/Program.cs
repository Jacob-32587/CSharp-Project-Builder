﻿namespace Program;

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

public class ProjectSelection(Project project)
{
    public Project Project = project;

    public override string ToString() {
        return Project.Name;
    }
}

public class ClientProject {
    public string? Model  { get; set; }
    public string? DataLayer { get; set; }
}

public class ConnectedProjects {
    public List<ClientProject>? ApiClients { get; set; }
    public string? Model { get; set; }
    public string? DataLayer { get; set; }
}

public class Program {
    public class Options {
            private string? _ProjectPath;
            /// <summary>
            /// Valid absolute path to .sln file
            /// </summary>
            [Option('p', "project-path", Required = true, HelpText = "The relative/absolute path to your .csproj file")]
            public string SolPath {
                get { return _ProjectPath!; }
                set {
                    var cwd = Environment.CurrentDirectory;
                    var absolute_file_path = Path.Join(cwd, value);
                    
                    // Use provided file path, if none then attempt to find .sln in cwd
                    if(File.Exists(absolute_file_path)) {
                        _ProjectPath = absolute_file_path;
                    } else {
                        var sln_file = Directory.GetFiles(cwd).First(x => x.EndsWith(".sln"));
                        if(sln_file is not null) {
                            _ProjectPath = sln_file;
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
                               |__/
        [/]");
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

        var requestType = AnsiConsole.Prompt(
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
        
        var projectSelection = sol.Projects.Where(
            x => x.CompilationOptions?.OutputKind != OutputKind.DynamicallyLinkedLibrary
        ).Select(x => new ProjectSelection(x)).ToList();

        var project = AnsiConsole.Prompt(
            new SelectionPrompt<ProjectSelection>()
                .Title("Choose what project needs and endpoint added")
                .EnableSearch()
                .PageSize(15)
                .AddChoices(
                    projectSelection
                )
        );

        // project.Project

        // var rawFileData = System.Text.Encoding.UTF8.GetString(await fileReadTask);
        // var projDoc = XElement.Parse(rawFileData ?? "");
        
        // var projRefEls = projDoc.Elements("ItemGroup").Elements("ProjectReference");

        // var clients = projRefEls.ToList().Where(x => x.Attributes().Any(x => x.Name == "Client")).ToList();
        // var model = projRefEls.Where(x => x.Attributes().Any(x => x.Name == "Models")).ToList();
        // if(model.Count > 1) {
        //     AnsiConsole.WriteLine(
        //         $"[red] You may only assign 1 project reference as your model project, found {model.Count}"
        //     );
        // }

        // Console.WriteLine(string.Join(",", clients));
        // Console.WriteLine(string.Join(",", model));

        return 0;
    }

}