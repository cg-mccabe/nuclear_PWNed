using Godot;

/// <summary>
/// Spawner — passive enemy factory driven by WaveManager.
/// Call StartSpawning(interval) to begin and StopSpawning() to halt.
/// Assign EnemyAScene, EnemyBScene, and EnemyContainer in the Inspector.
/// </summary>
public partial class Spawner : Node2D
{
	[Export] public PackedScene EnemyAScene    { get; set; }
	[Export] public PackedScene EnemyBScene    { get; set; }
	[Export] public Node2D      EnemyContainer { get; set; }
	[Export] public float       SpawnMargin    { get; set; } = 32f;

	private Timer                    _timer;
	private RandomNumberGenerator    _rng = new();

	public override void _Ready()
	{
		if (EnemyContainer == null)
			GD.PrintErr("Spawner: EnemyContainer not assigned!");

		_timer          = new Timer();
		_timer.OneShot  = false;
		AddChild(_timer);
		_timer.Timeout += SpawnEnemy;
		// Do NOT auto-start — WaveManager calls StartSpawning()
	}

	// ── Public API ────────────────────────────────────────────────────────────

	public void StartSpawning(float interval)
	{
		if (EnemyAScene == null && EnemyBScene == null)
		{
			GD.PrintErr("Spawner: No enemy scenes assigned!");
			return;
		}
		_timer.WaitTime = interval;
		_timer.Start();
		GD.Print($"Spawner started (interval={interval}s)");
	}

	public void StopSpawning()
	{
		_timer.Stop();
		GD.Print("Spawner stopped.");
	}

	// ── Internal ──────────────────────────────────────────────────────────────

	private void SpawnEnemy()
	{
		PackedScene scene;
		if (EnemyAScene != null && EnemyBScene != null)
			scene = _rng.RandiRange(0, 1) == 0 ? EnemyAScene : EnemyBScene;
		else
			scene = EnemyAScene ?? EnemyBScene;

		var enemy = scene.Instantiate<Node2D>();
		EnemyContainer.AddChild(enemy);
		enemy.GlobalPosition = GetSpawnPosition();

		GD.Print($"Spawned {enemy.Name} at {enemy.GlobalPosition}");
	}

	private Vector2 GetSpawnPosition()
	{
		var viewportSize = GetViewportRect().Size;
		var camera       = GetViewport().GetCamera2D();
		var center       = camera != null ? camera.GlobalPosition : viewportSize / 2f;

		float halfW = viewportSize.X / 2f + SpawnMargin;
		float halfH = viewportSize.Y / 2f + SpawnMargin;

		int edge = _rng.RandiRange(0, 3);
		return edge switch
		{
			0 => new Vector2(center.X + _rng.RandfRange(-halfW, halfW), center.Y - halfH),
			1 => new Vector2(center.X + halfW,  center.Y + _rng.RandfRange(-halfH, halfH)),
			2 => new Vector2(center.X + _rng.RandfRange(-halfW, halfW), center.Y + halfH),
			_ => new Vector2(center.X - halfW,  center.Y + _rng.RandfRange(-halfH, halfH)),
		};
	}
}
