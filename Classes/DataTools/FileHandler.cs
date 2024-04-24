using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using FileAccess = Godot.FileAccess;

// Autoload class to load and save files.
namespace Tempora.Classes.Utility;

public partial class FileHandler : Node
{
    // Correct usage: "using var file = ReadFile(path)". The 'using' operator is required to free/dispose file from memory.
    public static FileAccess ReadFile(string path)
    {
        FileAccess? file = FileAccess.FileExists(path)
            ? FileAccess.Open(path, FileAccess.ModeFlags.Read)
            : null;
        return file ?? throw new Exception($"File \"{path}\" does not exist.");
    }

    public static byte[] GetFileAsBuffer(string path)
    {
        using FileAccess file = ReadFile(path);
        ulong size = file.GetLength();
        return file.GetBuffer((long)size);
    }

    public static void CopyFile(string pathFrom, string pathTo)
    {
        using var fileNew = FileAccess.Open(pathTo, FileAccess.ModeFlags.Write);
        fileNew.StoreBuffer(GetFileAsBuffer(pathFrom));
    }

    public static AudioStreamMP3 LoadFileAsAudioStreamMp3(string path)
    {
        var sound = new AudioStreamMP3
        {
            Data = GetFileAsBuffer(path)
        };
        return sound;
    }

    public static string[] LoadFileAsTextArraySplittingByNewlines(string path)
    {
        using FileAccess file = ReadFile(path);
        string text = file.GetAsText();
        string[] textAsArray = text.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        return textAsArray;
    }

    public static void SaveText(string path, string text)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreString(text);
    }

    //public static void SaveMP3(string path, AudioStreamMP3 audioStreamMP3)
    //{
    //    using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
    //    file.StoreBuffer(audioStreamMP3.Data);
    //}

    public static string LoadText(string path)
    {
        if (FileAccess.FileExists(path))
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            string text = file.GetAsText();
            return text;
        }

        throw new Exception("File does not exist.");
    }

    public static List<string> GetFilePathsInDirectory(string directoryPath)
    {
        var filePaths = new List<string>();

        using var dir = DirAccess.Open(directoryPath);
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (dir.CurrentIsDir())
                {
                    // Do nothing if fileName is a folder
                }
                else
                {
                    filePaths.Add(directoryPath + "/" + fileName);
                    //filePaths.Add(fileName);
                }

                fileName = dir.GetNext();
            }
        }
        else
        {
            throw new Exception("An error occurred while trying to retrieve files from directory.");
        }

        return filePaths;
    }

    public static string GetDirectory(string filePath)
    {
        string dir = "";
        string[] pathParts = filePath.Split('/');
        for (int i = 0; i < pathParts.Length - 1; i++)
            dir += pathParts[i] + "/";
        return dir;
    }

    public static string GetExtension(string filePath)
    {
        string[] pathParts = filePath.Split('.');
        if (pathParts.Length < 2)
            return null!;

        string fileExtension = pathParts[^1];

        return fileExtension;
    }
}