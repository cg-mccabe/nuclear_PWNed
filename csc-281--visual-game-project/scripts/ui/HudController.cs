using Godot;

/// <summary>
/// HudController — attach to the root node of HUD.tscn (a Control node under CanvasLayer).
///
/// Listens to Player.HealthChanged and Player.WeaponEquipped to drive the
/// animated HUD sprites. 5 hearts = 5 hit states (each heart = 20% max HP).
/// </summary>
public partial class HudController : Control
{
	[Export] public AnimatedSprite2D GlovesHud  { get; set; }
	[Export] public AnimatedSprite2D BatHud     { get; set; }
	[Export] public AnimatedSprite2D PipeHud    { get; set; }

	[Export] public Label WaveLabel   { get; set; }
	[Export] public Label TimerLabel  { get; set; }
	[Export] public Label PromptLabel { get; set; }

	// ── Animation names — must match tscn exactly ─────────────────────────────

	private static readonly string[] GlovesAnims =
	{
		"Hud gloves",        // 5/5 hearts
		"Hub gloves 1 hit",  // 4/5 hearts
		"Hud glove 2 hit",   // 3/5 hearts
		"Hud glove 3 hit",   // 2/5 hearts
		"Hud glove 4 hit",   // 1/5 hearts
		"Hud glove 5 hit",   // 0/5 hearts (dead)
	};

	private static readonly string[] BatAnims =
	{
		"Hud bat normal",
		"Hud Pipe hit 1",
		"Hud Pipe hit 2",
		"Hud Pipe hit 3",
		"Hud Pipe hit 4",
		"Hud Pipe hit 5",
	};

	private static readonly string[] PipeAnims =
	{
		"Hud Pipe normal",
		"Hud Pipe hit 1 ",   // trailing space — matches tscn exactly
		"Hud Pipe hit 2",
		"Hud Pipe hit 3",
		"Hud Pipe hit 4",
		"Hud Pipe hit 5",
	};

	private AnimatedSprite2D _activeHud;
	private string[]         _activeAnims;
	private Player           _player;
	private bool             _connected = false;

	// ── Ready ─────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		if (BatHud  != null) BatHud.Visible  = false;
		if (PipeHud != null) PipeHud.Visible = false;
		if (GlovesHud != null)
		{
			GlovesHud.Visible = true;
			GlovesHud.Play(GlovesAnims[0]);
		}

		_activeHud   = GlovesHud;
		_activeAnims = GlovesAnims;

		if (PromptLabel != null) PromptLabel.Visible = false;
		if (WaveLabel   != null) WaveLabel.Visible   = false;
		if (TimerLabel  != null) TimerLabel.Visible  = false;

		// Defer so the full scene tree (including Player) is ready
		CallDeferred(MethodName.ConnectToPlayer);
	}

	// ── Per-frame fallback: keep trying to connect until we find the player ───

	public override void _Process(double delta)
	{
		if (!_connected)
			ConnectToPlayer();
	}

	// ── Player connection ─────────────────────────────────────────────────────

	private void ConnectToPlayer()
	{
		var players = GetTree().GetNodesInGroup("player");
		if (players.Count == 0 || players[0] is not Player p)
			return;   // not ready yet — _Process will retry

		_player    = p;
		_connected = true;

		_player.HealthChanged  += OnHealthChanged;
		_player.WeaponEquipped += OnWeaponEquipped;

		// Sync HUD to current health immediately (handles scene-load state)
		RefreshFrame();
		GD.Print("HudController: connected to Player.");
	}

	// ── Signal handlers ───────────────────────────────────────────────────────

	private void OnHealthChanged(float current, float max)
	{
		GD.Print($"HudController: health changed {current}/{max} → index {HealthToIndex(current, max)}");
		RefreshFrame();
	}

	private void OnWeaponEquipped(int weaponType)
	{
		switch ((Player.WeaponType)weaponType)
		{
			case Player.WeaponType.Bat:
				_activeHud   = BatHud;
				_activeAnims = BatAnims;
				break;
			case Player.WeaponType.Pipe:
				_activeHud   = PipeHud;
				_activeAnims = PipeAnims;
				break;
			default:
				_activeHud   = GlovesHud;
				_activeAnims = GlovesAnims;
				break;
		}
		ShowOnly(_activeHud);
		RefreshFrame();
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private void RefreshFrame()
	{
		if (_player == null || _activeHud == null) return;

		int index = HealthToIndex(_player.CurrentHealth, _player.MaxHealth);

		// Guard: clamp to valid range in case anim array is shorter than expected
		index = Mathf.Clamp(index, 0, _activeAnims.Length - 1);

		_activeHud.Play(_activeAnims[index]);
	}

	/// <summary>
	/// Maps health percentage to a 0-5 index.
	/// 0 = full health, 5 = dead / no hearts.
	/// Each step = one lost heart (20% of max HP).
	/// </summary>
	private static int HealthToIndex(float current, float max)
	{
		if (max <= 0f) return 5;
		float pct = current / max;

		// Use strict > for upper bounds so a hit that lands exactly on a
		// boundary (e.g. exactly 80%) always steps the display forward.
		if (pct > 0.80f) return 0;   // 5 hearts  (81–100%)
		if (pct > 0.60f) return 1;   // 4 hearts  (61–80%)
		if (pct > 0.40f) return 2;   // 3 hearts  (41–60%)
		if (pct > 0.20f) return 3;   // 2 hearts  (21–40%)
		if (pct > 0.00f) return 4;   // 1 heart   (1–20%)
		return 5;                     // 0 hearts  (dead)
	}

	private void ShowOnly(AnimatedSprite2D target)
	{
		if (GlovesHud != null) GlovesHud.Visible = (GlovesHud == target);
		if (BatHud    != null) BatHud.Visible    = (BatHud    == target);
		if (PipeHud   != null) PipeHud.Visible   = (PipeHud   == target);
	}
}
