using Godot;

/// <summary>
/// TitleScreen — attached to Game_Starting.tscn.
/// Shows the title image and a "Press Enter to Start" label.
/// Enter transitions to Level 1.
/// </summary>
public partial class TitleScreen : Node2D
{
    [Export] public string      Level1Path  { get; set; } = "res://scenes/world/Level.tscn";
    [Export] public AudioStream MenuMusic   { get; set; }

    private AudioStreamPlayer _music;

    public override void _Ready()
    {
        if (MenuMusic != null)
        {
            _music        = new AudioStreamPlayer();
            _music.Stream = MenuMusic;
            _music.Autoplay = true;
            AddChild(_music);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept"))
        {
            if (_music != null) _music.Stop();
            GetTree().ChangeSceneToFile(Level1Path);
        }
    }
}
