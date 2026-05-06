using Godot;

/// <summary>
/// Fly — Level 2 heavy mob (analogous to EnemyB in Level 1).
/// Has an 8% chance to drop a vial on death (50/50 Blood or Goop).
/// </summary>
public partial class Fly : EnemyBase
{
	[Export] public PackedScene BloodVialScene { get; set; }
	[Export] public PackedScene GoopVialScene  { get; set; }

	private AnimatedSprite2D _sprite;
	private static readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		Speed  = 55f;
		Health = 200f;
		Damage = 100f;
		KnockbackDecay = 0.12f;
		base._Ready();

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("flying");
	}

	protected override void OnDamaged(float amount)
	{
		_sprite.Modulate = new Color(1f, 0.45f, 0.45f);
		_ = ResetAfterDelay();
	}

	protected override void OnDeath()
	{
		_sprite.Play("dying");
		GameManager.Instance?.AddScore(25);
		GD.Print("Fly defeated — +25 points");
		TryDropVial();
		base.OnDeath();
	}

	private void TryDropVial()
	{
		if (_rng.Randf() > 0.20f) return;

		PackedScene scene = _rng.RandiRange(0, 1) == 0 ? BloodVialScene : GoopVialScene;
		if (scene == null)
		{
			GD.PrintErr("Fly: vial scene not assigned in Inspector!");
			return;
		}

		var vial = scene.Instantiate<Node2D>();
		GetParent().AddChild(vial);
		vial.GlobalPosition = GlobalPosition;
		GD.Print($"Fly dropped a vial at {GlobalPosition}");
	}

	private async System.Threading.Tasks.Task ResetAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(0.09f), SceneTreeTimer.SignalName.Timeout);
		if (IsInsideTree()) _sprite.Modulate = Colors.White;
	}
}
