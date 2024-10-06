using System.CommandLine;

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

        var copyOption = new Option<bool>(
            name: "--copy",
            description: "An option for copy file insted of move file.");
        rootCommand.Add(copyOption);

        rootCommand.SetHandler((
            dirArgValue,
            dryRunArgValue,
            copyArgValue) => {
                Console.WriteLine("Arguments: ");
                Console.WriteLine($"  Target directory: {dirArgValue}");
                if (dryRunArgValue) { Console.WriteLine("  Dry run: True"); }
                if (copyArgValue) { Console.WriteLine("  Copy: True"); }
                Console.WriteLine();

                var solver = new Solver(dirArgValue) {
                    IsDryRun = dryRunArgValue,
                    IsCopy = copyArgValue,
                };
                solver.Invoke();

            }, dirArgument, dryRunOption, copyOption);

        await rootCommand.InvokeAsync(args);
    }
}


class Solver(DirectoryInfo targetDir) {

    public bool IsDryRun { get; init; }
    public bool IsCopy { get; init; }
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
        var groupDict = new Dictionary<string, List<FileInfo>>();
        for (int i = 0; i < titleList.Count; i++) {
            var title = titleList[i];
            var groupName = "";
            var maxScore = -0.5;

            for (int b = 0; b < title.Length; b++) {
                for (int e = b + MinimumNameLength; e <= title.Length; e++) { //title[b..e]なのでeはLengthと等しいところまで上がる。

                    var tempGroupName = title[b..e].TrimEnd();
                    if (groupDict.ContainsKey(tempGroupName)) { continue; }

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
                groupDict.Add(groupName, []);
            }
        }

        var fileList = GetFileList().ToList();
        for (int i = 0; i < titleList.Count; i++) {
            var title = titleList[i];

            var maxGroup = groupDict.Keys
                .Where(title.Contains)
                .OrderByDescending(g => g.Length)
                .First();

            groupDict[maxGroup].Add(fileList[i]);
        }


        groupDict.Keys.ToList().ForEach(g => {
            if (groupDict[g].Count <= 0) {
                groupDict.Remove(g);
            }
        });


        //結果表示
        groupDict.Keys.Order().ToList().ForEach(g => {
            Console.WriteLine($"group: {g}");

            var filelist = groupDict[g];
            filelist.OrderBy(f => f.Name).ToList().ForEach(f => {
                Console.WriteLine("  " + f.Name);
            });
        });
        Console.WriteLine();


        if (IsDryRun) { return; }
        //ファイル移動実行
        Console.WriteLine(IsCopy ? "Copying files .." : "Moving files ..");
        var cnt = 0;
        var fileCnt = groupDict.Values.Sum(fList => fList.Count);
        groupDict.Keys.Order().ToList().ForEach(g => {
            var groupDir = Path.Combine(targetDir.FullName, g);
            Directory.CreateDirectory(groupDir);

            var filelist = groupDict[g];
            filelist.OrderBy(f => f.Name).ToList().ForEach(f => {
                if (IsCopy) {
                    File.Copy(f.FullName, Path.Combine(groupDir, f.Name), true);
                } else {
                    File.Move(f.FullName, Path.Combine(groupDir, f.Name), true);
                }
                cnt++;
                Console.Write($"{100 * cnt / fileCnt}%");
                Console.SetCursorPosition(0, Console.CursorTop);
            });
        });
        Console.WriteLine("Completed!");
    }
}
