using Godot;

/// <summary>
/// WaveManager — attached to both Level and Level2 root nodes.
///
/// Wave flow per level:
///   • Press Enter  → Wave 1 starts (60-second timer begins)
///   • All enemies dead AND timer up → show "Press Enter"
///   • Press Enter  → Wave 2 starts
///   • After Wave 2 enemies die → spawn weapon pickup (bat in L1, pipe in L2) near player
///   • Press Enter  → Wave 3 starts (boss wave)
///   • On Level2 Wave 3: swaps to BossMusicStream if assigned
///   • All enemies + boss dead → "Press Enter" to go to next level (or win screen on L2)
/// </summary>
public partial class WaveManager : Node
{
	// ── Wave settings ─────────────────────────────────────────────────────────
	[Export] public float WaveDuration       { get; set; } = 75f;
	[Export] public float SpawnInterval      { get; set; } = 2.5f;
	[Export] public bool  IsLevel2           { get; set; } = false;

	// ── Scene references ──────────────────────────────────────────────────────
	[Export] public PackedScene EnemyAScene       { get; set; }
	[Export] public PackedScene EnemyBScene       { get; set; }
	[Export] public PackedScene BossScene         { get; set; }
	[Export] public PackedScene WeaponPickupScene { get; set; }

	[Export] public Node2D  EnemyContainer  { get; set; }
	[Export] public Node2D  PlayerNode      { get; set; }
	[Export] public Label   WaveLabel       { get; set; }
	[Export] public Label   TimerLabel      { get; set; }
	[Export] public Label   PromptLabel     { get; set; }
	[Export] public string  NextScenePath   { get; set; } = "res://scenes/world/Level2.tscn";

	/// <summary>
	/// Optional. Assign a different AudioStream here to play during Level 2 Wave 3.
	/// Leave empty to keep the existing music.
	/// </summary>
	[Export] public AudioStream BossMusicStream { get; set; }

	// ── State ─────────────────────────────────────────────────────────────────
	public int  CurrentWave     { get; private set; } = 0;
	public bool WaveActive      { get; private set; } = false;

	private float  _waveTimeLeft    = 0f;
	private bool   _waveTimerDone   = false;
	private bool   _waitingForEnter = true;
	private bool   _levelFinished   = false;
	private bool   _weaponDropped   = false;

	private Spawner                  _spawner;
	private AudioStreamPlayer2D      _music;
	private RandomNumberGenerator    _rng = new();

	// ── Ready ─────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		_spawner = GetParent().GetNodeOrNull<Spawner>("Spawner");
		_music   = GetParent().GetNodeOrNull<AudioStreamPlayer2D>("AudioStreamPlayer2D");
		ShowPrompt("Press Enter to Start Wave 1");
	}

	// ── Per-frame ─────────────────────────────────────────────────────────────

	public override void _Process(double delta)
	{
		if (_levelFinished) return;

		if (Input.IsActionJustPressed("ui_accept") && _waitingForEnter)
		{
			_waitingForEnter = false;
			HidePrompt();
			AdvanceWave();
			return;
		}

		if (!WaveActive) return;

		if (!_waveTimerDone)
		{
			_waveTimeLeft -= (float)delta;
			UpdateTimerLabel();
			if (_waveTimeLeft <= 0f)
			{
				_waveTimeLeft  = 0f;
				_waveTimerDone = true;
				_spawner?.StopSpawning();
				UpdateTimerLabel();
			}
		}

		if (_waveTimerDone && EnemyContainer.GetChildCount() == 0)
			OnWaveCleared();
	}

	// ── Wave control ──────────────────────────────────────────────────────────

	private void AdvanceWave()
	{
		CurrentWave++;
		WaveActive     = true;
		_waveTimerDone = false;
		_waveTimeLeft  = WaveDuration;

		UpdateWaveLabel();
		UpdateTimerLabel();

		if (WaveLabel  != null) WaveLabel.Visible  = true;
		if (TimerLabel != null) TimerLabel.Visible = true;

		if (CurrentWave == 3)
			StartBossWave();
		else
			_spawner?.StartSpawning(SpawnInterval);

		GD.Print($"Wave {CurrentWave} started.");
	}

	private void StartBossWave()
	{
		_spawner?.StartSpawning(SpawnInterval);

		// Swap to boss music on Level 2 wave 3
		if (IsLevel2 && BossMusicStream != null && _music != null)
		{
			_music.Stream = BossMusicStream;
			_music.Play();
			GD.Print("WaveManager: switched to boss music.");
		}

		var bossTimer = GetTree().CreateTimer(5.0);
		bossTimer.Timeout += SpawnBoss;
	}

	private void SpawnBoss()
	{
		if (BossScene == null) { GD.PrintErr("WaveManager: BossScene not assigned!"); return; }

		var boss = BossScene.Instantiate<Node2D>();
		EnemyContainer.AddChild(boss);
		boss.GlobalPosition = GetSpawnPosition();

		if (boss is EnemyBase eb)
		{
			eb.Health *= 8f;
			eb.Speed  *= 0.7f;
			eb.Damage *= 2f;
			eb.Scale   = new Vector2(2f, 2f);
		}

		GD.Print($"Boss spawned at {boss.GlobalPosition}");
	}

	private void OnWaveCleared()
	{
		WaveActive = false;
		_spawner?.StopSpawning();
		GD.Print($"Wave {CurrentWave} cleared!");

		if (CurrentWave == 2 && !_weaponDropped)
		{
			_weaponDropped = true;
			DropWeapon();
		}

		if (CurrentWave >= 3)
		{
			_levelFinished = true;
			string msg = IsLevel2
				? "You Win!  Press Enter to return to menu"
				: "Wave 3 cleared!  Press Enter to continue to Level 2";
			ShowPrompt(msg);
			_waitingForEnter = true;
		}
		else
		{
			int next = CurrentWave + 1;
			ShowPrompt($"Wave {CurrentWave} cleared!  Press Enter to start Wave {next}");
			_waitingForEnter = true;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!_levelFinished) return;
		if (@event.IsActionPressed("ui_accept"))
		{
			if (IsLevel2)
				GetTree().ChangeSceneToFile("res://scenes/ui/Game_Starting.tscn");
			else
				GetTree().ChangeSceneToFile(NextScenePath);
		}
	}

	// ── Weapon drop ───────────────────────────────────────────────────────────

	private void DropWeapon()
	{
		if (WeaponPickupScene == null) { GD.PrintErr("WaveManager: WeaponPickupScene not assigned!"); return; }

		var pickup = WeaponPickupScene.Instantiate<Node2D>();
		GetParent().AddChild(pickup);

		Vector2 dropPos = PlayerNode != null
			? PlayerNode.GlobalPosition + new Vector2(60f, 0f)
			: Vector2.Zero;
		pickup.GlobalPosition = dropPos;

		GD.Print($"Weapon pickup dropped at {dropPos}");
	}

	// ── Spawn helper ──────────────────────────────────────────────────────────

	private Vector2 GetSpawnPosition()
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		var camera = GetViewport().GetCamera2D();
		var center = camera != null ? camera.GlobalPosition : viewportSize / 2f;
		float halfW = viewportSize.X / 2f + 32f;
		float halfH = viewportSize.Y / 2f + 32f;
		int edge = _rng.RandiRange(0, 3);
		return edge switch
		{
			0 => new Vector2(center.X + _rng.RandfRange(-halfW, halfW), center.Y - halfH),
			1 => new Vector2(center.X + halfW,  center.Y + _rng.RandfRange(-halfH, halfH)),
			2 => new Vector2(center.X + _rng.RandfRange(-halfW, halfW), center.Y + halfH),
			_ => new Vector2(center.X - halfW,  center.Y + _rng.RandfRange(-halfH, halfH)),
		};
	}

	// ── HUD helpers ───────────────────────────────────────────────────────────

	private void UpdateWaveLabel()
	{
		if (WaveLabel != null) WaveLabel.Text = $"Wave {CurrentWave} / 3";
	}

	private void UpdateTimerLabel()
	{
		if (TimerLabel == null) return;
		int mins = (int)(_waveTimeLeft / 60f);
		int secs = (int)(_waveTimeLeft % 60f);
		TimerLabel.Text = $"{mins}:{secs:D2}";
	}

	private void ShowPrompt(string msg)
	{
		if (PromptLabel == null) return;
		PromptLabel.Text    = msg;
		PromptLabel.Visible = true;
	}

	private void HidePrompt()
	{
		if (PromptLabel != null) PromptLabel.Visible = false;
	}
}
