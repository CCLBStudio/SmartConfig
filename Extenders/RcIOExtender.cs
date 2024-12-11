using System.IO;
using UnityEngine;

namespace CCLBStudio.RemoteConfig
{
    public static class RcIOExtender
    {
        public static string GenerateUniquePath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
            {
                Debug.LogError("Path is null or empty.");
                return string.Empty;
            }
        
            if (!File.Exists(originalPath))
            {
                return originalPath;
            }
        
            string directory = Path.GetDirectoryName(originalPath);
            string fileWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);

            int counter = 1;
            string uniqueFileName;
        
            do
            {
                uniqueFileName = $"{fileWithoutExtension}_{counter}{extension}";
            } while (File.Exists(Path.Combine(directory, uniqueFileName)));

            return Path.Combine(directory, uniqueFileName);
        }

        public static bool IsFileInsideProject(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Empty path cannot be a project file.");
                return false;
            }
        
            string projectPath = Application.dataPath;

            string normalizedFilePath = Path.GetFullPath(filePath).Replace('\\', '/');
            string normalizedProjectPath = Path.GetFullPath(projectPath).Replace('\\', '/');

            return normalizedFilePath.StartsWith(normalizedProjectPath);
        }

        public static string AbsoluteToProjectRelativePath(string absolutePath)
        {
            string projectPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            string normalizedAbsolutePath = Path.GetFullPath(absolutePath).Replace('\\', '/');
        
            if (!normalizedAbsolutePath.StartsWith(projectPath))
            {
                Debug.LogWarning($"Provided path is not a path relative to this Unity project : {absolutePath}");
                return string.Empty;
            }
        
            return "Assets/" + normalizedAbsolutePath.Substring(projectPath.Length + 1);
        }

        public static string RelativeToAbsolutePath(string relativePath)
        {
            if (relativePath.StartsWith("Assets"))
            {
                return Path.GetFullPath(relativePath);
            }
        
            Debug.LogError($"Path {relativePath} is not a valid relative path.");
            return string.Empty;
        }
    }
}