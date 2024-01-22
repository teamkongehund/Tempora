using Godot;

namespace Tempora.Classes.Visual;

public partial class VersionLabel : Label
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Text = ThisAssembly.Git.Tag;
    }
}