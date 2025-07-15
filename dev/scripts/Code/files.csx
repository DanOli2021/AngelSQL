// GLOBALS
// These lines of code go in each script
#load "..\Globals.csx"
// END GLOBALS

// Process to send messages to user
// Daniel() Oliver Rojas
// 2024-08-25

// This script works as an API so that different applications
// can affect sales, purchases, inventory entries and exits,
// physical inventories, accounts receivable and payable.

#load "..\AngelComm\AngelComm.csx"

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


private AngelApiOperation api = JsonConvert.DeserializeObject<AngelApiOperation>(message);
api.db = db;
api.server_db = server_db;

// This is the main function that will be called by the API
return api.OperationType switch
{
    "GetDirectoryTree" => GetDirectoryTree(),
    "GetFileContent" => GetFileContent(),
    "SaveContent" => SaveContent(),
    "CreateFolder" => CreateFolder(),
    "RenameItem" => RenameItem(),
    "DeleteItem" => DeleteItem(),
    _ => $"Error: No service found {api.OperationType}",
};


public string GetDirectoryTree()
{
    try
    {
        string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        string rootPath = server_db.Prompt("VAR db_app_directory");

        var tree = DirectoryExplorer.GetDirectoryTree(rootPath);
        return JsonConvert.SerializeObject(tree, Formatting.Indented);
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message} (1)";
    }
}


public string GetFileContent()
{
    try
    {
        string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        string filePath = api.DataMessage.filePath.ToString();

        string rootPath = server_db.Prompt("VAR db_app_directory");

        filePath = Path.Combine(rootPath, filePath);

        if (!File.Exists(filePath))
        {
            return $"Error: File not found {filePath}";
        }

        if (Path.GetExtension(filePath).ToLower() != ".csx" &&
           Path.GetExtension(filePath).ToLower() != ".html" &&
           Path.GetExtension(filePath).ToLower() != ".js" &&
           Path.GetExtension(filePath).ToLower() != ".py" &&
           Path.GetExtension(filePath).ToLower() != ".json")
        {
            return $"Error: Unsupported file type {Path.GetExtension(filePath)}";
        }

        string content = File.ReadAllText(filePath);
        return content;
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message} (2)";
    }
}


public string SaveContent()
{
    try
    {
        string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");

        if (result.StartsWith("Error:"))
        {
            return result;
        }

        if (api.DataMessage == null)
        {
            return "Error: No data message provided";
        }

        if (api.DataMessage.filePath == null)
        {
            return "Error: filePath is missing in the data message";
        }

        if (api.DataMessage.content == null)
        {
            return "Error: content is missing in the data message";
        }

        string filePath = api.DataMessage.filePath.ToString();
        string content = api.DataMessage.content.ToString();

        string rootPath = server_db.Prompt("VAR db_app_directory");
        filePath = Path.Combine(rootPath, filePath);

        if (Path.GetExtension(filePath).ToLower() != ".csx" &&
           Path.GetExtension(filePath).ToLower() != ".html" &&
           Path.GetExtension(filePath).ToLower() != ".js" &&
           Path.GetExtension(filePath).ToLower() != ".py" &&
           Path.GetExtension(filePath).ToLower() != ".json")
        {
            return $"Error: Unsupported file type {Path.GetExtension(filePath)}";
        }

        if (File.Exists(filePath))
        {
            string savedDir = Path.Combine(rootPath, "Saved");
            Directory.CreateDirectory(savedDir); // Ensures the directory exists

            string backupFile = "__" + Path.GetFileName(filePath);
            string extension = Path.GetExtension(filePath);
            backupFile = backupFile.Replace(extension, $"_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
            string backupPath = Path.Combine(savedDir, backupFile);
            File.Copy(filePath, backupPath, true);
        }
        else
        {
            // If file does not exist, ensure the directory for the new file exists.
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        File.WriteAllText(filePath, content);
        return "File saved successfully";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message} (3)";
    }
}

public string CreateFolder()
{
    try
    {
        string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");
        if (result.StartsWith("Error:")) return result;

        string newFolderPath = api.DataMessage.path.ToString();
        string rootPath = server_db.Prompt("VAR db_app_directory");
        string fullPath = Path.Combine(rootPath, newFolderPath);

        if (Directory.Exists(fullPath) || File.Exists(fullPath))
        {
            return $"Error: A file or directory with that name already exists.";
        }

        Directory.CreateDirectory(fullPath);
        return "Folder created successfully";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message} (4)";
    }
}

public string RenameItem()
{
    try
    {
        string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");
        if (result.StartsWith("Error:")) return result;

        string oldPath = api.DataMessage.oldPath.ToString();
        string newPath = api.DataMessage.newPath.ToString();
        string rootPath = server_db.Prompt("VAR db_app_directory");

        string fullOldPath = Path.Combine(rootPath, oldPath);
        string fullNewPath = Path.Combine(rootPath, newPath);

        if (!File.Exists(fullOldPath) && !Directory.Exists(fullOldPath))
        {
            return "Error: Source item not found.";
        }

        if (File.Exists(fullNewPath) || Directory.Exists(fullNewPath))
        {
            return "Error: Destination item already exists.";
        }

        if (Directory.Exists(fullOldPath))
        {
            Directory.Move(fullOldPath, fullNewPath);
        }
        else
        {
            File.Move(fullOldPath, fullNewPath);
        }

        return "Item renamed successfully";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message} (5)";
    }
}

public string DeleteItem()
{
    try
    {
        string result = IsTokenValid(api, "STAKEHOLDER, SUPERVISOR");
        if (result.StartsWith("Error:")) return result;

        string itemPath = api.DataMessage.path.ToString();
        string rootPath = server_db.Prompt("VAR db_app_directory");
        string fullPath = Path.Combine(rootPath, itemPath);

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            return "Error: Item not found.";
        }

        string savedDir = Path.Combine(rootPath, "Saved");
        Directory.CreateDirectory(savedDir);

        string destName = Path.GetFileName(itemPath) + $"_{DateTime.Now:yyyyMMdd_HHmmss}";
        string destPath = Path.Combine(savedDir, destName);

        if (Directory.Exists(fullPath))
        {
            Directory.Move(fullPath, destPath);
        }
        else
        {
            File.Move(fullPath, destPath);
        }

        return "Item deleted successfully";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message} (6)";
    }
}


public class DirectoryExplorer
{
    private static string _rootPath;

    public static FileNode GetDirectoryTree(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath); // Asegura que sea absoluta
        var info = new DirectoryInfo(_rootPath);
        return GetNode(info);
    }

    private static FileNode GetNode(FileSystemInfo fsi)
    {
        try
        {
            fsi.Refresh();

            // Excluir archivos/carpetas ocultos
            if ((fsi.Attributes & FileAttributes.Hidden) != 0)
                return null;

            //if (fsi.Name == "Saved") // Excluir la carpeta Saved
            //    return null;

            if (fsi.Name.StartsWith("."))
                return null;

            if( fsi.Name.ToLower() == "config.html")
                return null;

            if (fsi.Name.ToLower() == "code") 
                return null;
        
            if (fsi.Name.ToLower() == "config") 
                return null;
                
            if (fsi.Name.ToLower() == "data") 
                return null;

            if (fsi.Extension.ToLower() == ".webmi")
                return null;

            var fullPath = Path.GetFullPath(fsi.FullName);
            var relativePath = Path.GetRelativePath(_rootPath, fullPath).Replace('\\', '/');

            var node = new FileNode
            {
                Name = fsi.Name,
                FullPath = relativePath,
                Type = fsi is DirectoryInfo ? "directory" : "file",
                CreationTime = fsi.CreationTime,
                LastWriteTime = fsi.LastWriteTime,
                SizeBytes = fsi is FileInfo file ? file.Length : null
            };

            if (fsi is DirectoryInfo dirInfo)
            {
                foreach (var item in dirInfo.GetFileSystemInfos())
                {
                    var child = GetNode(item);
                    if (child != null)
                        node.Children.Add(child);
                }
            }

            return node;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

public class FileNode
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public string Type { get; set; } // "file" o "directory"
    public DateTime CreationTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public long? SizeBytes { get; set; } // Solo para archivos
    public List<FileNode> Children { get; set; } = new List<FileNode>();
}