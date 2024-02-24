namespace Program;

using System.Xml.Linq;
using CommandLine;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Spectre.Console;

public enum RequestSelectionChoices {
    Get,
    Update,
    Insert,
    Upsert,
    Delete,
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
            [Option('p', "project-path", Required = true, HelpText = "The relative/absolute path to your .csproj file")]
            public string ProjectPath {
                get { return _ProjectPath!; }
                set {
                    var absolute_file_path = Path.Join(Environment.CurrentDirectory, value);
                    if(File.Exists(absolute_file_path)) {
                        _ProjectPath = absolute_file_path;
                        Console.WriteLine("Good");
                    } else {
                        AnsiConsole.Markup(
                            $"[red]Could not parse '{Path.Join(Environment.CurrentDirectory, value)}'" +
                            "as a valid file path[/]",
                            Console.Error
                        );
                        Environment.Exit(1);
                    }
                }
            }
    }
    static async Task<int> Main(string[] args) {
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

        var fileReadTask = File.ReadAllBytesAsync(options.ProjectPath);

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
        var rawFileData = System.Text.Encoding.UTF8.GetString(await fileReadTask);
        var projDoc = XElement.Parse(rawFileData ?? "");
        
        var projRefEls = projDoc.Elements("ItemGroup").Elements("ProjectReference");

        var clients = projRefEls.ToList().Where(x => x.Attributes().Any(x => x.Name == "Client")).ToList();
        var model = projRefEls.Where(x => x.Attributes().Any(x => x.Name == "Models")).ToList();
        if(model.Count > 1) {
            AnsiConsole.WriteLine(
                $"[red] You may only assign 1 project reference as your model project, found {model.Count}"
            );
        }

        Console.WriteLine(string.Join(",", clients));
        Console.WriteLine(string.Join(",", model));

        return 0;
    }

}