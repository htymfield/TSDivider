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

            var solver = new Solver(dirArgValue) {
                IsDryRun = dryRunArgValue,
            };
            solver.Invoke();

        }, dirArgument, dryRunOption);

        await rootCommand.InvokeAsync(args);
    }
}


class Solver(DirectoryInfo targetDir) {

    public bool IsDryRun { get; init; } = true;
    public List<string> ExtList { get; init; } = [".mp4", ".ts"];
    public int MinimumNameLength { get; init; } = 3;


    public void Invoke() {
        var fileNameList = targetDir.EnumerateFiles("*.*")
            .Where(f => ExtList.Any(e => f.FullName.Contains(e)))
            .Select(f => f.Name.Split("_")[1]) // _で囲まれた番組名の部分のみ取得
            .ToList();

        var groupList = new List<string>();
        for (int i = 0; i < fileNameList.Count; i++) {
            var filename = fileNameList[i];
            var scoreMax = 0;
            var groupName = "";

            for (int b = 0; b < filename.Length; b++) {
                for (int e = b + MinimumNameLength; e < filename.Length; e++) {

                    var tempGroupName = filename[b..e];

                    var tempScore = fileNameList
                        .Where(f => f.Contains(tempGroupName))
                        .Count() * tempGroupName.Length;

                    if (tempScore > scoreMax) {
                        scoreMax = tempScore;
                        groupName = tempGroupName;
                    }
                }
            }
            groupList.Add(groupName);
            fileNameList = fileNameList.Where(f=>!f.Contains(groupName)).ToList();
        }


        groupList.Sort();
        foreach (var item in groupList)
        {
            Console.WriteLine(item);
        }

    }
}
