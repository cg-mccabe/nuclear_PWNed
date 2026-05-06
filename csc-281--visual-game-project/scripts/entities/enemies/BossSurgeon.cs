using Godot;

/// <summary>
/// BossSurgeon — Level 2 wave-3 boss.
/// AttackHitbox is a separate Area2D that activates only during the "attack"
/// animation state, dealing a burst of damage.
/// Wire AttackHitbox in the Inspector to the Area2D child node.
/// </summary>
public partial class BossSurgeon : EnemyBase
{
	private AnimatedSprite2D _sprite;

	[Export] public float  AttackPauseDuration { get; set; } = 1.2f;
	[Export] public float  RushDuration        { get; set; } = 2.5f;
	[Export] public float  AttackDamage        { get; set; } = 180f;  // damage per attack hit
	[Export] public Area2D AttackHitbox        { get; set; }          // assign in Inspector

	private enum SurgeonState { Walking, Attacking }
	private SurgeonState _state      = SurgeonState.Walking;
	private float        _stateTimer = 0f;
	private bool         _attackHit  = false;  // only hit once per attack swing

	public override void _Ready()
	{
		Speed  = 100f;
		Health = 5000f;
		Damage = 110f;
		KnockbackDecay = 0.06f;
		base._Ready();

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("walking");
		_stateTimer = RushDuration;

		if (AttackHitbox != null)
		{
			AttackHitbox.Monitoring = false;
			AttackHitbox.BodyEntered += OnAttackHitboxBodyEntered;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		_stateTimer -= (float)delta;

		switch (_state)
		{
			case SurgeonState.Walking:
				if (AttackHitbox != null) AttackHitbox.Monitoring = false;
				base._PhysicsProcess(delta);
				if (_stateTimer <= 0f)
				{
					_state      = SurgeonState.Attacking;
					_stateTimer = AttackPauseDuration;
					_attackHit  = false;
					_sprite.Play("attack");

					// Enable hitbox as soon as attack animation starts
					if (AttackHitbox != null) AttackHitbox.Monitoring = true;
				}
				break;

			case SurgeonState.Attacking:
				Velocity = Vector2.Zero;
				if (_stateTimer <= 0f)
				{
					_state      = SurgeonState.Walking;
					_stateTimer = RushDuration;
					_sprite.Play("walking");

					if (AttackHitbox != null) AttackHitbox.Monitoring = false;
				}
				break;
		}
	}

	private void OnAttackHitboxBodyEntered(Node2D body)
	{
		if (_attackHit) return;   // only damage once per swing
		if (body is not Player player) return;

		_attackHit = true;
		player.TakeDamage(AttackDamage);
		GD.Print($"BossSurgeon attack hit player for {AttackDamage}");
	}

	protected override void OnDamaged(float amount)
	{
		_sprite.Modulate = new Color(1f, 0.5f, 0.5f);
		_ = ResetColorAfterDelay();
	}

	protected override void OnDeath()
	{
		if (AttackHitbox != null) AttackHitbox.Monitoring = false;
		GameManager.Instance?.AddScore(300);
		GD.Print("BossSurgeon defeated — +300 points");
		base.OnDeath();
	}

	private async System.Threading.Tasks.Task ResetColorAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(0.12f), SceneTreeTimer.SignalName.Timeout);
		if (IsInsideTree()) _sprite.Modulate = Colors.White;
	}
}
