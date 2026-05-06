using Godot;

/// <summary>
/// BossFrog — Level 1 wave-3 boss.
/// Contact damage comes from EnemyBase as usual.
/// AttackHitbox is a separate Area2D that activates only during the "licking"
/// animation, dealing a big burst of damage with its own cooldown.
/// Wire AttackHitbox in the Inspector to the Area2D child node.
/// </summary>
public partial class BossFrog : EnemyBase
{
	private AnimatedSprite2D _sprite;

	[Export] public float LickDuration  { get; set; } = 1.8f;
	[Export] public float LickDamage    { get; set; } = 150f;  // damage per lick hit
	[Export] public Area2D AttackHitbox { get; set; }          // assign in Inspector

	private bool  _isLicking   = false;
	private float _lickTimer   = 0f;
	private float _chaseTimer  = 0f;
	private bool  _lickHit     = false;  // only hit once per lick cycle
	private const float ChaseInterval = 3.5f;

	public override void _Ready()
	{
		Speed  = 65f;
		Health = 1000f;
		Damage = 90f;
		KnockbackDecay = 0.08f;
		base._Ready();

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("talking");

		// Connect attack hitbox if assigned
		if (AttackHitbox != null)
		{
			AttackHitbox.Monitoring = false;
			AttackHitbox.BodyEntered += OnAttackHitboxBodyEntered;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isLicking)
		{
			_lickTimer -= (float)delta;

			// Enable hitbox for the middle portion of the lick
			if (AttackHitbox != null)
				AttackHitbox.Monitoring = true;

			if (_lickTimer <= 0f)
			{
				_isLicking = false;
				_lickHit   = false;
				_chaseTimer = ChaseInterval;
				_sprite.Play("talking");

				if (AttackHitbox != null)
					AttackHitbox.Monitoring = false;
			}
			return;
		}

		_chaseTimer -= (float)delta;
		if (_chaseTimer <= 0f)
		{
			_isLicking  = true;
			_lickTimer  = LickDuration;
			_sprite.Play("licking");
			return;
		}

		base._PhysicsProcess(delta);
	}

	private void OnAttackHitboxBodyEntered(Node2D body)
	{
		if (_lickHit) return;   // only damage once per lick
		if (body is not Player player) return;

		_lickHit = true;
		player.TakeDamage(LickDamage);
		GD.Print($"BossFrog lick hit player for {LickDamage}");
	}

	protected override void OnDamaged(float amount)
	{
		_sprite.Modulate = new Color(1f, 0.4f, 0.4f);
		_ = ResetColorAfterDelay();
	}

	protected override void OnDeath()
	{
		if (AttackHitbox != null) AttackHitbox.Monitoring = false;
		GameManager.Instance?.AddScore(200);
		GD.Print("BossFrog defeated — +200 points");
		base.OnDeath();
	}

	private async System.Threading.Tasks.Task ResetColorAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
		if (IsInsideTree()) _sprite.Modulate = Colors.White;
	}
}
