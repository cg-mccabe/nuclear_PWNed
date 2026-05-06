using Godot;

/// <summary>
/// EnemyA — Fast, fragile chaser. Rushes the player quickly but dies easily.
/// Attach to EnemyA.tscn which inherits from Enemy.tscn.
/// </summary>
public partial class EnemyA : EnemyBase
{
	[Export] public float FlashDuration { get; set; } = 0.1f;

	private AnimatedSprite2D _sprite;
	private bool _isFlashing = false;

	public override void _Ready()
	{
		Speed  = 130f;
		Health = 75f;   // was 15 — scaled 5x to match player's 500 HP
		Damage = 25f;   // was 5  — one hit = one heart (100 HP per heart)
		base._Ready();

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Walking");
	}

	protected override void OnDamaged(float amount)
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Getting Hit");
		if (!_isFlashing) FlashWhite();
	}

	protected override void OnDeath()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Dying");
		GameManager.Instance?.AddScore(10);
		GD.Print("EnemyA died — +10 points");
		base.OnDeath();
	}

	private async void FlashWhite()
	{
		_isFlashing      = true;
		_sprite.Modulate = Colors.White * 3f;
		await ToSignal(GetTree().CreateTimer(FlashDuration), SceneTreeTimer.SignalName.Timeout);
		_sprite.Modulate = Colors.White;
		_isFlashing      = false;
	}
}
