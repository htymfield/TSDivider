// See https://aka.ms/new-console-template for more information

using System.CommandLine;

class Program {
    static async Task Main(string[] args) {
        var rootCommand = new RootCommand("TSDivider");

        var dirArgument = new Argument<string>(
            name: "dir",
            description: "Target directory");
        rootCommand.Add(dirArgument);

        rootCommand.SetHandler((dirArgValue) => {
            Console.WriteLine("test");
        }, dirArgument);

        await rootCommand.InvokeAsync(args);
    }
}