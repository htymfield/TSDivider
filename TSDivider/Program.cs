// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.Globalization;
using System.Runtime.InteropServices;

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

    IEnumerable<FileInfo> GetFileList() => targetDir
        .EnumerateFiles("*.*")
        .Where(f => ExtList.Any(e => f.FullName.Contains(e)));

    static string GetTitle(FileInfo fileinfo) {
        return fileinfo.Name.Split("_")[1]; // _で囲まれた番組名の部分のみ取得
    }


    public void Invoke() {

        var titleList = GetFileList().Select(f => (Title: GetTitle(f), Score: 0)).ToList();
        var groupSet = new HashSet<string>();
        for (int i = 0; i < titleList.Count; i++) {
            var title = titleList[i].Title;
            var groupName = "";

            for (int b = 0; b < title.Length; b++) {
                for (int e = b + MinimumNameLength; e < title.Length; e++) {

                    var tempGroupName = title[b..e];
                    if (groupSet.Contains(tempGroupName)) { continue; }

                    var groupChildren = titleList
                        .Where(t => t.Title.Contains(tempGroupName));
                    var currentChidrenScore = groupChildren.Sum(t => t.Score);

                    //1番組のみのフォルダができるのを防ぐための特殊処理
                    var eachScore = groupChildren.Count() == 1 ? 0 : tempGroupName.Length;
                    var newChildrenScore = groupChildren.Count() * eachScore;

                    if (newChildrenScore >= currentChidrenScore) {
                        groupName = tempGroupName;
                        for (int j = 0; j < titleList.Count; j++) {
                            if (titleList[j].Title.Contains(groupName)) {
                                titleList[j] =
                                    (titleList[j].Title, eachScore);
                            }
                        }
                    }
                }
            }
            groupSet.Add(groupName);
        }

        var groupList = groupSet.ToList();
        groupList.Sort();
        var fileList = GetFileList().ToList();
        var groupChildrenList = groupList.Select(g => new List<FileInfo>()).ToList();
        for (int i = 0; i < titleList.Count; i++) {
            var title = titleList[i].Title;
            var score = titleList[i].Score;

            for (int g = 0; g < groupList.Count; g++) {
                var group = groupList[g];
                if (!title.Contains(group)) { continue; }
                if (group.Length != score) { continue; }
                groupChildrenList[g].Add(fileList[i]);
            }
        }


        for (int i = 0; i < groupList.Count; i++) {
            Console.WriteLine($"group: {groupList[i]}");
            for (int j = 0; j < groupChildrenList[i].Count; j++) {
                Console.WriteLine("  " + groupChildrenList[i][j].Name);
            }
        }

    }
}
