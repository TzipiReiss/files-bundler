//fib bundle -o file.txt -a author -l "js html" -r -n -s type

using System.CommandLine;

var rootCommand = new RootCommand("Root command for File Bundler CLI");

#region bundle

var bundleCommand = new Command("bundle", "Bundle code files to a single file");

var outputOption = new Option<FileInfo>(new string[] { "--output", "-o" }, "file fath and name");
var authorOption = new Option<string>(new string[] { "--author", "-a" }, "author name");
var languagesOption = new Option<string>(new string[] { "--languages", "-l" }, "languages of the desired files");
var removeEmptyLinesOption = new Option<bool>(new string[] { "--remove-empty-lines", "-r" }, "delete empty lines from the files");
var includeNoteOption = new Option<bool>(new string[] { "--note", "-n" }, "specify the source of the code");
var sortOption = new Option<string>(new string[] { "--sort", "-s" }, "sort by type or by name");

bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(authorOption);
bundleCommand.AddOption(languagesOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(includeNoteOption);
bundleCommand.AddOption(sortOption);

bundleCommand.SetHandler((output, author, languages, removeEmptyLines, includeNote, sort) =>
{
    if (output == null)
    {
        Console.WriteLine("Error: output option is required!");
        return;
    }
    if (languages == null)
    {
        Console.WriteLine("Error: languages option is required!");
        return;
    }
    try
    {
        using (FileStream file = File.Create(output.FullName))
        {

            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.WriteLine($"output:\n'{output.FullName}'\n");
                if (author != null)
                    writer.WriteLine($"author: {author}\n");

                writer.WriteLine("-------------------------------------------");

                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories);

                string[] excludedFolders = { "bin", "debug", "obj", ".vs", ".config", ".vscode", ".git", "build" };
                string[] excludedExtensions = { ".csproj", ".json", ".dockerignore", ".csproj.user", ".txt", ".rsp", ".png", ".gif", ".jpg" };
                string[] excludedFileNames = { "Dockerfile" };

                files = files.Where(file =>
                        !excludedFolders.Any(folder => Path.GetDirectoryName(file).Contains(folder))
                        && !excludedExtensions.Contains(Path.GetExtension(file))
                        && !excludedFileNames.Contains(Path.GetFileName(file))).ToArray();

                files = (sort == "type") ?
                        files.OrderBy(f => Path.GetExtension(f)).ToArray() :
                        files.OrderBy(f => Path.GetFileName(f)).ToArray();

                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string fileContent = File.ReadAllText(filePath);
                    if (languages == "all")
                    {
                        WriteFileContents(writer, fileName, fileContent, removeEmptyLines, includeNote, filePath);
                    }
                    else
                    {
                        string fileExtension = Path.GetExtension(filePath).TrimStart('.');
                        if (languages.Split(' ').Contains(fileExtension))
                            WriteFileContents(writer, fileName, fileContent, removeEmptyLines, includeNote, filePath);
                    }
                }
            }
        }
        Console.WriteLine("File was created!");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: File fath is invalid");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}, outputOption, authorOption, languagesOption, removeEmptyLinesOption, includeNoteOption, sortOption);

void WriteFileContents(StreamWriter writer, string fileName, string fileContent, bool removeEmptyLines, bool includeNote, string filePath)
{
    writer.WriteLine("File Name: " + fileName);
    writer.WriteLine("File Content:");

    if (removeEmptyLines)
    {
        string[] lines = fileContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            writer.WriteLine(line);
        }
    }
    else
    {
        writer.WriteLine(fileContent);
    }

    if (includeNote)
    {
        writer.WriteLine(GetRelativeFilePath(filePath));
    }
    writer.WriteLine("-------------------------------------------");
}

string GetRelativeFilePath(string filePath)
{
    string currentDirectory = Directory.GetCurrentDirectory();
    Uri currentUri = new Uri(currentDirectory + Path.DirectorySeparatorChar);
    Uri fileUri = new Uri(filePath);
    Uri relativeUri = currentUri.MakeRelativeUri(fileUri);
    string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
    string fileName = Path.GetFileName(filePath);
    return $"###Source: {fileName} (Relative Path: {relativePath})###";
}

rootCommand.AddCommand(bundleCommand);

#endregion

#region create-rsp

var createRspCommand = new Command("create-rsp", "");

createRspCommand.SetHandler(() =>
{
    Console.WriteLine("Creating response file...");
    string? input;

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
        file.WriteLine($"--output {outputOption}");
        if (!string.IsNullOrEmpty(authorOption))
            file.WriteLine($"--author \"{authorOption}\"");
        file.WriteLine($"--languages \"{languagesOption}\"");
        if (removeEmptyLinesOption)
            file.WriteLine("--remove-empty-lines");
        if (includeNoteOption)
            file.WriteLine("--note");
        if (!string.IsNullOrEmpty(sortOption))
            file.WriteLine($"--sort {sortOption}");
    }
    Console.WriteLine("Response file created successfully.");
});

rootCommand.AddCommand(createRspCommand);

#endregion

rootCommand.InvokeAsync(args);