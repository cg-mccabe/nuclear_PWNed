using Godot;

/// <summary>
/// Rat — Level 2 fast, fragile enemy (analogous to EnemyA in Level 1).
/// Attach to a scene that inherits from Enemy.tscn, using ratSheet.png sprites.
/// </summary>
public partial class Rat : EnemyBase
{
	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		Speed  = 145f;
		Health = 100f;
		Damage = 35f;
		base._Ready();

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Walking");
	}

	protected override void OnDamaged(float amount)
	{
		_sprite.Modulate = Colors.White * 3f;
		_ = ResetAfterDelay();
	}

	protected override void OnDeath()
	{
		_sprite.Play("Dying");
		GameManager.Instance?.AddScore(15);
		GD.Print("Rat defeated — +15 points");
		base.OnDeath();
	}

	private async System.Threading.Tasks.Task ResetAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(0.08f), SceneTreeTimer.SignalName.Timeout);
		if (IsInsideTree()) _sprite.Modulate = Colors.White;
	}
}
