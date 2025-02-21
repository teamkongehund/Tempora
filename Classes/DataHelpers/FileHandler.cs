// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

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
}