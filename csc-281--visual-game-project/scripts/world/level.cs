using Godot;

/// <summary>
/// Root script for Level and Level2.
///
/// • StartingWeapon — set to Bat (1) on Level2 so the player enters already armed.
/// • Connects Player signals → HudController.
/// • Shows in-level Game Over label on death; Enter retries.
/// </summary>
public partial class Level : Node2D
{
    [Export] public string            GameOverScenePath { get; set; } = "res://scenes/ui/GameOver.tscn";
    [Export] public float             GameOverDelay     { get; set; } = 1.0f;

    /// <summary>
    /// Weapon the player starts with. 0 = Gloves (default), 1 = Bat, 2 = Pipe.
    /// Set to 1 on the Level2 root node in the Inspector (already done in Level2.tscn).
    /// </summary>
    [Export] public Player.WeaponType StartingWeapon    { get; set; } = Player.WeaponType.Gloves;

    private Player        _player;
    private HudController _hud;
    private bool          _gameOverTriggered = false;
    private bool          _waitingForRetry   = false;

    public override void _Ready()
    {
        // ── Find player ───────────────────────────────────────────────────────
        _player = GetNodeOrNull<Player>("Player");
        if (_player == null)
        {
            GD.PrintErr("Level: Could not find Player node — game-over will not work!");
            return;
        }

        // ── Connect player signals ────────────────────────────────────────────
        _player.HealthChanged += OnPlayerHealthChanged;
        _player.PlayerDied    += OnPlayerDied;
        GD.Print("Level: PlayerDied signal connected successfully.");

        // ── Wire HUD (best-effort, won't crash if absent) ─────────────────────
        _hud = GetNodeOrNull<HudController>("CanvasLayer/Hud");
        if (_hud != null)
        {
            if (_hud.GlovesHud == null)
                _hud.GlovesHud = _hud.GetNodeOrNull<AnimatedSprite2D>("GlovesHud");
            if (_hud.BatHud == null)
                _hud.BatHud = _hud.GetNodeOrNull<AnimatedSprite2D>("BatHud");
            if (_hud.PipeHud == null)
                _hud.PipeHud = _hud.GetNodeOrNull<AnimatedSprite2D>("PipeHud");
            if (_hud.WaveLabel == null)
                _hud.WaveLabel = _hud.GetNodeOrNull<Label>("WaveLabel");
            if (_hud.TimerLabel == null)
                _hud.TimerLabel = _hud.GetNodeOrNull<Label>("TimerLabel");
            if (_hud.PromptLabel == null)
                _hud.PromptLabel = _hud.GetNodeOrNull<Label>("PromptLabel");
        }

        // ── Apply starting weapon one frame later ─────────────────────────────
        if (StartingWeapon != Player.WeaponType.Gloves)
            ApplyStartingWeaponDeferred();
    }

    private async void ApplyStartingWeaponDeferred()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _player.EquipWeapon(StartingWeapon);
    }

    private void OnPlayerHealthChanged(float current, float max)
    {
        GD.Print($"HP: {current}/{max}");
    }

    private async void OnPlayerDied()
    {
        if (_gameOverTriggered) return;
        _gameOverTriggered = true;

        GD.Print("Level: OnPlayerDied fired — triggering game over.");

        // Stop enemies and spawner
        var wm = GetNodeOrNull<WaveManager>("WaveManager");
        if (wm != null) wm.ProcessMode = ProcessModeEnum.Disabled;

        var spawner = GetNodeOrNull<Spawner>("Spawner");
        if (spawner != null) spawner.ProcessMode = ProcessModeEnum.Disabled;

        // Short delay so the death feels impactful
        await ToSignal(GetTree().CreateTimer(GameOverDelay), SceneTreeTimer.SignalName.Timeout);

        ShowGameOverLabel();
        _waitingForRetry = true;
    }

    private void ShowGameOverLabel()
    {
        var canvas   = new CanvasLayer();
        canvas.Layer = 10;
        AddChild(canvas);

        // Semi-transparent dark overlay
        var panel          = new ColorRect();
        panel.Color        = new Color(0f, 0f, 0f, 0.6f);
        panel.AnchorLeft   = 0f;
        panel.AnchorTop    = 0f;
        panel.AnchorRight  = 1f;
        panel.AnchorBottom = 1f;
        canvas.AddChild(panel);

        // "You Died" label
        var label                 = new Label();
        label.Text                = "You Died!\nPress Enter to retry.";
        label.AnchorLeft          = 0.5f;
        label.AnchorTop           = 0.5f;
        label.AnchorRight         = 0.5f;
        label.AnchorBottom        = 0.5f;
        label.OffsetLeft          = -250f;
        label.OffsetRight         =  250f;
        label.OffsetTop           = -60f;
        label.OffsetBottom        =  60f;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.LabelSettings       = new LabelSettings { FontSize = 40, FontColor = Colors.White };
        canvas.AddChild(label);

        GD.Print("Level: Game over label shown.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_waitingForRetry && @event.IsActionPressed("ui_accept"))
        {
            GD.Print("Level: Returning to title screen.");
            // Go back to the title / starting screen
            GetTree().ChangeSceneToFile("res://Game_Starting.tscn");
            return;
        }

        if (@event.IsActionPressed("ui_cancel"))
            GetTree().Paused = !GetTree().Paused;
    }
}
