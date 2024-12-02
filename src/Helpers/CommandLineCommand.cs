namespace Conesoft.Server_Host.Helpers;

record CommandLineCommand(string Command, string[] Arguments)
{
    public static IEnumerable<CommandLineCommand> Parse(string? commandLine = null)
    {
        commandLine ??= Environment.CommandLine;

        foreach (var command in commandLine.SplitExceptQuotes(" -").Skip(1))
        {
            var segments = command.SplitExceptQuotes(" ");
            if (segments.Length >= 1)
            {
                yield return new(segments[0], segments[1..]);
            }
        }
    }
}