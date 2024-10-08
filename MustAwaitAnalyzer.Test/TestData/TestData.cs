// (c) DeNA Co., Ltd.

using System.IO;
using System.Runtime.CompilerServices;

namespace MustAwaitAnalyzer.Test;

public class TestData
{
    public static string GetPath(string fileName) => Path.Combine(Path.GetDirectoryName(GetFilePath())!, fileName);

    private static string GetFilePath([CallerFilePath] string path = "") => path;
}
