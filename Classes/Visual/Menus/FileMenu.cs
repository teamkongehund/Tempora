using Godot;
using NAudio.SoundFont;
using System;
using System.Linq;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;
using System.IO;
using SaveConfig = Tempora.Classes.Utility.ProjectFileManager.SaveConfig;

namespace Tempora.Classes.Visual;
public partial class FileMenu : PopupMenu
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        IdPressed += OnIdPressed;
    }

    private void OnIdPressed(long id)
    {
        switch (id)
        {
            case 0:
                ProjectFileManager.Instance.LoadFileDialogPopup();
                break;
            case 1:
                ProjectFileManager.Instance.SaveProjectFileDialogPopup();
                break;
            case 2:
                OsuExporter.ExportAndOpenOsz();
                break;
            case 3:
                ProjectFileManager.Instance.SaveOszFileDialogPopup();
                break;
            case 4:
                ProjectFileManager.Instance.NewProject();
                break;
        }
    }
}