using Godot;

/// <summary>
/// EnemyB — A slow, tanky bruiser. Hits hard and absorbs punishment.
/// Has an 8% chance to drop a vial on death (50/50 Blood or Goop).
/// </summary>
public partial class EnemyB : EnemyBase
{
	[Export] public float KnockbackResistance { get; set; } = 0.6f;
	[Export] public PackedScene BloodVialScene { get; set; }
	[Export] public PackedScene GoopVialScene  { get; set; }

	private AnimatedSprite2D _sprite;
	private static readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		Speed  = 45f;
		Health = 250f;
		Damage = 100f;
		base._Ready();

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Walking");
	}

	public override bool TakeDamage(float amount, Vector2 knockback = default)
	{
		return base.TakeDamage(amount, knockback * (1f - KnockbackResistance));
	}

	protected override void OnDamaged(float amount)
	{
		_sprite.Modulate = Colors.White;
		_ = ResetColorAfterDelay();
	}

	protected override void OnDeath()
	{
		_sprite.Play("Dying");
		GameManager.Instance?.AddScore(30);
		TryDropVial();
		base.OnDeath();
	}

	private void TryDropVial()
	{
		if (_rng.Randf() > 0.20f) return;   // 8% chance

		// Pick which vial to drop — 50/50
		PackedScene scene = _rng.RandiRange(0, 1) == 0 ? BloodVialScene : GoopVialScene;
		if (scene == null)
		{
			GD.PrintErr("EnemyB: vial scene not assigned in Inspector!");
			return;
		}

		var vial = scene.Instantiate<Node2D>();
		// Add to parent so it persists after this enemy is freed
		GetParent().AddChild(vial);
		vial.GlobalPosition = GlobalPosition;
		GD.Print($"EnemyB dropped a vial at {GlobalPosition}");
	}

	private async System.Threading.Tasks.Task ResetColorAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(0.08f), SceneTreeTimer.SignalName.Timeout);
		if (IsInsideTree())
			_sprite.Modulate = new Color(1f, 0.4f, 0.4f);
	}
}
