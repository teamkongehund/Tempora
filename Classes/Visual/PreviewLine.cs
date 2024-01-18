using Godot;

namespace OsuTimer.Classes.Visual;

public partial class PreviewLine : Line2D
{
    [Export]
    public Label TimeLabel = null!;
}
