using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Autoload class to load and save files.
public partial class FileHandler : Node
{
    // Correct usage: "using var file = ReadFile(path)". The 'using' operator is required to free/dispose file from memory.
    public static Godot.FileAccess ReadFile(string path)
    {
        Godot.FileAccess file = Godot.FileAccess.FileExists(path)
            ? Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read)
            : null;
        if (file == null) throw new Exception($"File \"{path}\" does not exist.");
        return file;
    }

    public static byte[] GetFileAsBuffer(string path)
    {
        using var file = ReadFile(path);
        ulong size = file.GetLength();
        return file.GetBuffer((long)size);
    }

    public static void CopyFile(string pathFrom, string pathTo)
    {
        using var fileNew = Godot.FileAccess.Open(pathTo, Godot.FileAccess.ModeFlags.Write);
        fileNew.StoreBuffer(GetFileAsBuffer(pathFrom));
        GD.Print($"FileHandler.CopyFile: Copied {pathFrom} to {pathTo}");
    }

    public static AudioStreamMP3 LoadFileAsAudioStreamMP3(string path)
    {
        var sound = new AudioStreamMP3();
        sound.Data = GetFileAsBuffer(path);
        //Byte[] buffer = LoadFileAsBuffer(path);
        //GD.Print("Is (AudioStreamMp3.Data = LoadFileAsBuffer(path)) the same as (LoadFileAsBuffer(path))? " + (sound.Data.SequenceEqual<Byte>(buffer)));
        return sound;
    }

    public static string[] LoadFileAsTextArraySplittingByNewlines(string path)
    {
        using var file = ReadFile(path);
        string text = file.GetAsText();
        GD.Print($"FileHandler.LoadFileAsTextArray: Loaded the follwing text: {text}");
        string[] textAsArray = text.Split("\n",StringSplitOptions.RemoveEmptyEntries);
        return textAsArray;
    }

    public static void SaveText(string path, string text)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        file.StoreString(text);
    }

    public static string LoadText(string path)
    {
        if (Godot.FileAccess.FileExists(path))
        {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            string text = file.GetAsText();
            return text;
        }
        else { throw new Exception("File does not exist."); }
    }

    public static List<string> GetFilePathsInDirectory(string directoryPath)
    {
        List<string> filePaths = new List<string>();

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
                    filePaths.Add(directoryPath+"/"+fileName);
                    //filePaths.Add(fileName);
                }
                fileName = dir.GetNext();
            }
        }
        else throw new Exception($"An error occurred while trying to retrieve files from directory.");

        return filePaths;
    }

    public static string GetExtension(string filePath)
    {
        string[] pathParts = filePath.Split('.');
        if (pathParts.Length < 2)
        {
            throw new NotSupportedException("Unsupported audio file format.");
        }

        string fileExtension = pathParts[pathParts.Length - 1];

        return fileExtension;
    }
}
