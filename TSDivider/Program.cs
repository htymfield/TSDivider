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

    string[] TrimEndList { get; init; } = [
        "第",
        "#",
        "＃",
        " ★",
        "　★",
        ];

    string GetTitle(FileInfo fileinfo) {
        var sp = fileinfo.Name.Split("_")[1]; // _で囲まれた番組名の部分のみ取得
        foreach (var key in TrimEndList) {
            if (!sp.Contains(key)) { continue; }
            var i = sp.IndexOf(key);
            sp = sp[0..i];
        }
        return sp;
    }


    public void Invoke() {

        var titleList = GetFileList().Select(GetTitle).ToList();
        var groupSet = new HashSet<string>();
        for (int i = 0; i < titleList.Count; i++) {
            var title = titleList[i];
            var groupName = "";
            var maxScore = -0.5;

            for (int b = 0; b < title.Length; b++) {
                for (int e = b + MinimumNameLength; e <= title.Length; e++) { //title[b..e]なのでeはLengthと等しいところまで上がる。

                    var tempGroupName = title[b..e].TrimEnd();
                    if (groupSet.Contains(tempGroupName)) { continue; }

                    var groupChildren = titleList
                        .Where(t => t.Contains(tempGroupName));

                    //1番組のみのフォルダができるのを防ぐための特殊処理
                    var eachScore = groupChildren.Count() == 1 ? tempGroupName.Length / 2 : tempGroupName.Length;
                    var newChildrenScore = groupChildren.Count() * eachScore;


                    if (newChildrenScore >= maxScore) {
                        groupName = tempGroupName;
                        maxScore = newChildrenScore;
                    }
                }
            }
            if (groupName != "") {
                groupSet.Add(groupName);
            }
        }

        var groupList = groupSet.ToList();
        groupList.Sort();
        var fileList = GetFileList().ToList();
        var groupChildrenList = groupList.Select(g => new List<FileInfo>()).ToList();
        for (int i = 0; i < titleList.Count; i++) {
            var title = titleList[i];

            var maxScore = 0;
            var maxIndex = 0;
            for (int g = 0; g < groupList.Count; g++) {
                var group = groupList[g];
                if (!title.Contains(group)) { continue; }
                if (group.Length < maxScore) { continue; }
                maxScore = group.Length;
                maxIndex = g;
            }
            groupChildrenList[maxIndex].Add(fileList[i]);
        }


        for (int i = 0; i < groupList.Count; i++) {
            if (groupChildrenList[i].Count <= 0) { continue; }
            Console.WriteLine($"group: {groupList[i]}");
            for (int j = 0; j < groupChildrenList[i].Count; j++) {
                Console.WriteLine("  " + groupChildrenList[i][j].Name);
            }
        }

    }
}
