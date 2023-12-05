//fib bundle -o file.txt -a author -l "js html" -r -n -s type

using System.CommandLine;

var rootCommand = new RootCommand("Root command for File Bundler CLI");

#region bundle

var bundleCommand = new Command("bundle", "Bundle code files to a single file");

var outputOption = new Option<FileInfo>(new string[] { "--output", "-o" }, "file path and name");
var authorOption = new Option<string>(new string[] { "--author", "-a" }, "author name");
var languagesOption = new Option<string>(new string[] { "--languages", "-l" }, "languages of the desired files");
var sortOption = new Option<string>(new string[] { "--sort", "-s" }, "sort by type or by name");
var removeEmptyLinesOption = new Option<bool>(new string[] { "--remove-empty-lines", "-r" }, "delete empty lines from the files");
var includeNoteOption = new Option<bool>(new string[] { "--note", "-n" }, "specify the source of the code");

bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(authorOption);
bundleCommand.AddOption(languagesOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(includeNoteOption);

bundleCommand.SetHandler((output, author, languages, sort, removeEmptyLines, includeNote) =>
{
    if (!IsValidInputs(output, languages, sort))
        return;
    CreateFile(output, author, languages, sort, removeEmptyLines, includeNote);

}, outputOption, authorOption, languagesOption, sortOption, removeEmptyLinesOption, includeNoteOption);

bool IsValidInputs(FileInfo output, string languages, string sort)
{
    if (output == null)
    {
        Console.WriteLine("Error: Output option is required!");
        return false;
    }
    if (string.IsNullOrEmpty(languages))
    {
        Console.WriteLine("Error: Languages option is required!");
        return false;
    }
    if (!string.IsNullOrEmpty(sort) && sort != "type" && sort != "name")
    {
        Console.WriteLine("Error: The sort value is invalid!");
        return false;
    }
    return true;
}

void CreateFile(FileInfo output, string author, string languages, string sort, bool removeEmptyLines, bool includeNote)
{
    try
    {
        using (StreamWriter outputFile = new StreamWriter(output.FullName))
        {
            WriteOutputHeader(outputFile, output, author);

            string[] files = SortFiles(FilterFiles(languages), sort);

            CopyFilesContents(outputFile, files, removeEmptyLines, includeNote);
        }

        Console.WriteLine("File was created successfully!");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: File path is invalid");
    }
}

void WriteOutputHeader(StreamWriter outputFile, FileInfo output, string author)
{
    outputFile.WriteLine($"output: '{output.FullName}'");

    if (author != null)
        outputFile.WriteLine($"author: {author}");

    outputFile.WriteLine("-------------------------------------------");
}

string[] FilterFiles(string languages)
{
    string[] files;
    files = FilterRelevantFiles(Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories));
    files = FilterByLanguages(files, languages);
    return files;
}

string[] FilterRelevantFiles(string[] files)
{
    string[] excludedFolders = { "bin", "debug", "obj", ".vs", ".config", ".vscode", ".git", "Properties", "packages", "build", "out", ".idea" };
    string[] excludedExtensions = { ".config", ".csproj", ".json", ".dockerignore", ".db", ".user", ".gitignore", ".sln", ".txt", ".rsp", ".png", ".gif", ".jpg", ".mp4", ".iml" };
    string[] excludedFileNames = { "Dockerfile" };

    files = files.Where(file =>
                        !excludedFolders.Any(folder => Path.GetDirectoryName(file).Contains(folder))
                        && !excludedExtensions.Contains(Path.GetExtension(file))
                        && !excludedFileNames.Contains(Path.GetFileName(file))
                        ).ToArray();
    return files;
}

string[] FilterByLanguages(string[] files, string languages)
{
    if (languages == "all")
        return files;

    string[] filteredFiles = files.Where(filePath =>
                                         languages.Split(' ')
                                         .Contains(Path.GetExtension(filePath).TrimStart('.'))
                                         ).ToArray();
    return filteredFiles;
}

string[] SortFiles(string[] files, string sort)
{
    if (sort == "type")
        return files.OrderBy(f => Path.GetExtension(f)).ToArray();

    return files.OrderBy(f => Path.GetFileName(f)).ToArray();
}

void CopyFilesContents(StreamWriter outputFile, string[] files, bool removeEmptyLines, bool includeNote)
{
    foreach (string filePath in files)
    {
        WriteFileContents(outputFile, filePath, removeEmptyLines, includeNote);
        outputFile.WriteLine("-------------------------------------------");
    }
}

void WriteFileContents(StreamWriter outputFile, string filePath, bool removeEmptyLines, bool includeNote)
{
    string fileName = Path.GetFileName(filePath);
    string fileContent = File.ReadAllText(filePath);

    outputFile.WriteLine("File Name: " + fileName);

    WriteContent(outputFile, fileContent, removeEmptyLines);

    if (includeNote)
        outputFile.WriteLine(GetRelativeFilePath(filePath));
}

void WriteContent(StreamWriter outputFile, string fileContent, bool removeEmptyLines)
{
    outputFile.WriteLine("File Content:");

    if (removeEmptyLines)
    {
        string[] lines = fileContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            outputFile.WriteLine(line);
        }
    }
    else
        outputFile.WriteLine(fileContent);
}

string GetRelativeFilePath(string filePath)
{
    string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
    string fileName = Path.GetFileName(filePath);
    return $"#Source: {fileName} (Relative Path: {relativePath})#";
}

rootCommand.AddCommand(bundleCommand);

#endregion

#region create-rsp

var createRspCommand = new Command("create-rsp", "");

createRspCommand.SetHandler(() =>
{
    Console.WriteLine("Creating response file...");

    string? outputOption;
    do
    {
        Console.Write("Enter output option value: ");
        outputOption = Console.ReadLine();
    } while (string.IsNullOrEmpty(outputOption));

    Console.Write("Enter author option value: ");
    string? authorOption = Console.ReadLine();

    string? languagesOption;
    do
    {
        Console.Write("Enter languages option value (separated by spaces). If you want to include everything, enter \"all\": ");
        languagesOption = Console.ReadLine();
    } while (string.IsNullOrEmpty(languagesOption));

    string? input;

    Console.Write("Enter remove empty lines option value (true/false): ");
    input = Console.ReadLine();
    bool removeEmptyLinesOption = string.IsNullOrEmpty(input) ? false : bool.Parse(input);

    Console.Write("Enter include note option value (true/false): ");
    input = Console.ReadLine();
    bool includeNoteOption = string.IsNullOrEmpty(input) ? false : bool.Parse(input);

    Console.Write("Enter sort option value (type/name): ");
    input = Console.ReadLine();
    string sortOption = string.IsNullOrEmpty(input) ? "name" : input;

    using (StreamWriter file = new StreamWriter("responseFile.rsp"))
    {
        file.WriteLine($"-o {outputOption}");
        if (!string.IsNullOrEmpty(authorOption))
            file.WriteLine($"-a \"{authorOption}\"");
        file.WriteLine($"-l \"{languagesOption}\"");
        if (removeEmptyLinesOption)
            file.WriteLine("-r");
        if (includeNoteOption)
            file.WriteLine("-n");
        if (!string.IsNullOrEmpty(sortOption))
            file.WriteLine($"-s {sortOption}");
    }
    Console.WriteLine("Response file was created successfully!");
});

rootCommand.AddCommand(createRspCommand);

#endregion

rootCommand.InvokeAsync(args);