// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.Globalization;

class Program {
    static async Task Main(string[] args) {
        var rootCommand = new RootCommand("TSDivider");

        var dirArgument = new Argument<DirectoryInfo>(
            name: "dir",
            description: "Target directory.").ExistingOnly();
        rootCommand.Add(dirArgument);

        var dryRunOption = new Option<bool>(
            name: "--dry-run",
            description: "An option for dry run.");
        rootCommand.Add(dryRunOption);

        rootCommand.SetHandler((dirArgValue, dryRunArgValue) => {
            Console.WriteLine("Arguments: ");
            Console.WriteLine($"  Target directory: {dirArgValue}");
            if (dryRunArgValue) { Console.WriteLine("  Dry run: True"); }

            var solver = new Solver(
                dirArgValue,
                dryRunArgValue,
                [".mp4", ".ts"]
                );
            solver.Invoke();

        }, dirArgument, dryRunOption);

        await rootCommand.InvokeAsync(args);
    }
}


class Solver(
    DirectoryInfo targetDir,
    bool isDryRun,
    List<string> extList) {

    public void Invoke() {
        var fileList = targetDir.EnumerateFiles("*.*")
            .Where(f => extList.Any(e => f.FullName.Contains(e)));

    }
}