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

    IEnumerable<FileInfo> GetFileList() => targetDir
        .EnumerateFiles("*.*")
        .Where(f => ExtList.Any(e => f.FullName.Contains(e)));

    static string GetTitle(FileInfo fileinfo) {
        return fileinfo.Name.Split("_")[1]; // _で囲まれた番組名の部分のみ取得
    }


    public void Invoke() {

        var titleList = GetFileList().Select(GetTitle).ToList();
        var groupList = new List<string>();
        for (int i = 0; i < titleList.Count; i++) {
            var title = titleList[i];
            var scoreMax = 0;
            var groupName = "";

            for (int b = 0; b < title.Length; b++) {
                for (int e = b + MinimumNameLength; e < title.Length; e++) {

                    var tempGroupName = title[b..e];

                    var cnt = titleList
                        .Where(f => f.Contains(tempGroupName))
                        .Count();
                    //1番組のみのフォルダができるのを防ぐために0始まりにしている。
                    var tempScore = Math.Max(0, (cnt-1))
                        * (int)Math.Pow(2, tempGroupName.Length);

                    //1番組のみのグループ名でも更新されるように必ず＝をつけないといけない。
                    if (tempScore >= scoreMax) {
                        scoreMax = tempScore;
                        groupName = tempGroupName;
                    }
                }
            }
            groupList.Add(groupName);
            titleList = titleList.Where(f => !f.Contains(groupName)).ToList();
        }
        groupList.Sort();


        var fileList = GetFileList().ToList();
        var groupChildrenList = new List<List<FileInfo>>();
        for (int i = 0; i < groupList.Count; i++) {
            var groupChildren = new List<FileInfo>();
            groupChildrenList.Add(groupChildren);
            var group = groupList[i];

            for (int j = fileList.Count - 1; j >= 0; j--) {
                var file = fileList[j];
                var title = GetTitle(file);
                if (!title.Contains(group)) { continue; }

                groupChildren.Add(file);
                fileList.RemoveAt(j);
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
