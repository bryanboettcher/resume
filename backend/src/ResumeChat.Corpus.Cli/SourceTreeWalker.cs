using System.Security.Cryptography;
using System.Text;

namespace ResumeChat.Corpus.Cli;

sealed class SourceTreeWalker
{
    private const long MaxFileSizeBytes = 500 * 1024;

    private static readonly HashSet<string> IncludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".rs", ".py", ".go",
        ".yaml", ".yml", ".json", ".sql",
        ".csproj", ".sln", ".slnx",
        ".dockerfile", ".md", ".razor",
        ".html", ".css", ".scss",
    };

    private static readonly HashSet<string> SkippedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", ".git", "wwwroot", "dist",
    };

    // wwwroot/lib is skipped via path check; dist and others via directory name
    private static readonly HashSet<string> SkippedFileSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".Designer.cs", ".g.cs", ".AssemblyInfo.cs",
    };

    private static readonly HashSet<string> SkippedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AssemblyAttributes.cs", "GlobalUsings.g.cs",
    };

    private static readonly Dictionary<string, string> LanguageByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"]          = "csharp",
        [".ts"]          = "typescript",
        [".rs"]          = "rust",
        [".py"]          = "python",
        [".go"]          = "go",
        [".yaml"]        = "yaml",
        [".yml"]         = "yaml",
        [".json"]        = "json",
        [".sql"]         = "sql",
        [".csproj"]      = "msbuild",
        [".sln"]         = "msbuild",
        [".slnx"]        = "msbuild",
        [".dockerfile"]  = "dockerfile",
        [".md"]          = "markdown",
        [".razor"]       = "razor",
        [".html"]        = "html",
        [".css"]         = "css",
        [".scss"]        = "css",
    };

    public IEnumerable<SourceFile> Walk(SourceConfig source)
    {
        var root = new DirectoryInfo(source.Path);
        if (!root.Exists)
            yield break;

        foreach (var file in EnumerateFiles(root, root.FullName))
        {
            var ext = file.Extension;

            // Dockerfile has no extension — match by name
            var isDockerfile = file.Name.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase);
            if (!isDockerfile && !IncludedExtensions.Contains(ext))
                continue;

            if (file.Length > MaxFileSizeBytes)
                continue;

            if (IsSkippedByNamePattern(file.Name))
                continue;

            string content;
            try
            {
                content = File.ReadAllText(file.FullName, Encoding.UTF8);
            }
            catch (IOException)
            {
                // Unreadable files are silently skipped — permissions, locked, etc.
                continue;
            }

            var language = isDockerfile ? "dockerfile" : LanguageByExtension.GetValueOrDefault(ext);
            var hash = ComputeHash(content);
            var relativePath = Path.GetRelativePath(source.Path, file.FullName);
            var lineCount = CountLines(content);

            yield return new SourceFile(
                Repo: source.Repo,
                Branch: source.Branch,
                FilePath: relativePath,
                Language: language,
                ContentText: content,
                ContentHash: hash,
                LineCount: lineCount,
                SizeBytes: (int)file.Length
            );
        }
    }

    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo dir, string rootPath)
    {
        IEnumerable<DirectoryInfo> subdirs;
        IEnumerable<FileInfo> files;

        try
        {
            subdirs = dir.EnumerateDirectories();
            files = dir.EnumerateFiles();
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var file in files)
            yield return file;

        foreach (var subdir in subdirs)
        {
            if (SkippedDirectories.Contains(subdir.Name))
                continue;

            // Skip wwwroot/lib as a path segment combination
            if (subdir.Name.Equals("lib", StringComparison.OrdinalIgnoreCase) &&
                subdir.Parent?.Name.Equals("wwwroot", StringComparison.OrdinalIgnoreCase) == true)
                continue;

            foreach (var file in EnumerateFiles(subdir, rootPath))
                yield return file;
        }
    }

    private static bool IsSkippedByNamePattern(string fileName)
    {
        if (SkippedFileNames.Contains(fileName))
            return true;

        foreach (var suffix in SkippedFileSuffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    private static int CountLines(string content)
    {
        if (content.Length == 0)
            return 0;

        var count = 1;
        foreach (var c in content.AsSpan())
        {
            if (c == '\n')
                count++;
        }
        return count;
    }
}
