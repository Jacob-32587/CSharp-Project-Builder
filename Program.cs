namespace Program;
using CommandLine;
using Spectre.Console;

public enum RequestSelectionChoices {
    Get,
    Update,
    Insert,
    Upsert,
    Delete,
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
                        AnsiConsole.Markup($"[red]Could not parse '{Path.Join(Environment.CurrentDirectory, value)}' as a valid file path[/]", Console.Error);
                        Environment.Exit(1);
                    }
                }
            }
    }
    static int Main(string[] args) {
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
        Console.WriteLine(options.ProjectPath);

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
        Console.WriteLine(requestType);
        return 0;
    }

}