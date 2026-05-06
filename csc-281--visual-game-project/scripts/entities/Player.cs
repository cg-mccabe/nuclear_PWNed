using Godot;
/// <summary>
/// Player controller. Handles 8-directional movement, health, melee attacks, and damage response.
/// Added to the "player" group so enemies can find it via GetNodesInGroup("player").
///
/// Weapons
/// -------
/// EquipWeapon(WeaponType) switches the active sprite and bumps AttackDamage / AttackRange.
///   Gloves (default) → 100 dmg, 60px range, 110° arc
///   Bat              → 175 dmg, 80px range, 120° arc
///   Pipe             → 275 dmg, 95px range, 130° arc
/// </summary>
public partial class Player : CharacterBody2D
{
	public enum WeaponType { Gloves, Bat, Pipe }

	// ── Movement ──────────────────────────────────────────────────────────────
	[Export] public float Speed { get; set; } = 200f;

	private NavigationAgent2D _navAgent;

	// ── Health ────────────────────────────────────────────────────────────────
	[Export] public float MaxHealth { get; set; } = 500f;

	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Signal] public delegate void PlayerDiedEventHandler();
	[Signal] public delegate void WeaponEquippedEventHandler(int weaponType);

	public  float CurrentHealth { get; private set; }
	private bool  _isDead       = false;
	private bool  _isInvincible = false;

	[Export] public float InvincibilityDuration { get; set; } = 0.6f;

	// ── Combat ────────────────────────────────────────────────────────────────
	[Export] public float AttackDamage   { get; set; } = 100f;
	[Export] public float AttackCooldown { get; set; } = 0.4f;
	[Export] public float KnockbackForce { get; set; } = 300f;
	[Export] public float AttackRange    { get; set; } = 60f;
	[Export] public float AttackAngleDeg { get; set; } = 110f;

	private bool    _isAttacking      = false;
	private bool    _attackOnCooldown = false;
	private bool    _isHit            = false;
	private Vector2 _facingDirection  = Vector2.Right;

	// ── Weapon / Sprites ──────────────────────────────────────────────────────
	public WeaponType CurrentWeapon { get; private set; } = WeaponType.Gloves;

	private AnimatedSprite2D _spriteGloves;
	private AnimatedSprite2D _spriteBat;
	private AnimatedSprite2D _spritePipe;
	private AnimatedSprite2D _sprite;

	// ── Ready ─────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		AddToGroup("player");
		CurrentHealth = MaxHealth;
		_navAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
		_navAgent.PathDesiredDistance   = 4f;
		_navAgent.TargetDesiredDistance = 4f;

		_spriteGloves = GetNode<AnimatedSprite2D>("AnimatedSprite2DGloves");
		_spriteBat    = GetNode<AnimatedSprite2D>("AnimatedSprite2DBat");
		_spritePipe   = GetNode<AnimatedSprite2D>("AnimatedSprite2DPipe");

		_sprite = _spriteGloves;
		_spriteBat.Visible  = false;
		_spritePipe.Visible = false;
	}

	// ── Physics ───────────────────────────────────────────────────────────────

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;

		var direction = GetInputDirection();

		if (direction.LengthSquared() > 0.01f)
		{
			_facingDirection = direction;
			FlipSprite(direction);

			if (!_isAttacking && !_isHit) _sprite.Play("walking");

			_navAgent.TargetPosition = GlobalPosition + direction * 64f;
			var nextPos = _navAgent.GetNextPathPosition();
			Velocity = (nextPos - GlobalPosition).Normalized() * Speed;
		}
		else
		{
			if (!_isAttacking && !_isHit) _sprite.Play("normal");
			Velocity = Vector2.Zero;
		}
		MoveAndSlide();

		if (Input.IsActionJustPressed("attack") && !_isAttacking && !_attackOnCooldown)
			StartAttack();
	}

	// ── Input helpers ─────────────────────────────────────────────────────────

	private Vector2 GetInputDirection() =>
		new Vector2(
			Input.GetAxis("ui_left", "ui_right"),
			Input.GetAxis("ui_up", "ui_down")
		).Normalized();

	private void FlipSprite(Vector2 dir)
	{
		if (dir.X != 0) _sprite.FlipH = dir.X > 0;
	}

	// ── Weapon equip ──────────────────────────────────────────────────────────

	public void EquipWeapon(WeaponType weapon)
	{
		CurrentWeapon = weapon;

		_spriteGloves.Visible = false;
		_spriteBat.Visible    = false;
		_spritePipe.Visible   = false;

		switch (weapon)
		{
			case WeaponType.Bat:
				_sprite        = _spriteBat;
				AttackDamage   = 175f;
				AttackRange    = 80f;
				AttackAngleDeg = 120f;
				KnockbackForce = 380f;
				break;

			case WeaponType.Pipe:
				_sprite        = _spritePipe;
				AttackDamage   = 275f;
				AttackRange    = 95f;
				AttackAngleDeg = 130f;
				KnockbackForce = 460f;
				break;

			default:
				_sprite        = _spriteGloves;
				AttackDamage   = 100f;
				AttackRange    = 60f;
				AttackAngleDeg = 110f;
				KnockbackForce = 300f;
				break;
		}

		_sprite.Visible = true;
		EmitSignal(SignalName.WeaponEquipped, (int)weapon);
	}

	// ── Melee Attack ──────────────────────────────────────────────────────────

	private async void StartAttack()
	{
		_isAttacking      = true;
		_attackOnCooldown = true;

		_sprite.Play("attack");

		float halfAngleRad = Mathf.DegToRad(AttackAngleDeg / 2f);

		foreach (var node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not EnemyBase enemy) continue;

			Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
			float   dist    = toEnemy.Length();

			if (dist > AttackRange) continue;

			float angle = _facingDirection.AngleTo(toEnemy.Normalized());
			if (Mathf.Abs(angle) > halfAngleRad) continue;

			enemy.TakeDamage(AttackDamage, toEnemy.Normalized() * KnockbackForce);
		}

		await ToSignal(GetTree().CreateTimer(AttackCooldown), SceneTreeTimer.SignalName.Timeout);
		_isAttacking      = false;
		_attackOnCooldown = false;
	}

	// ── Receiving Damage ──────────────────────────────────────────────────────

	public void TakeDamage(float amount)
	{
		if (_isDead || _isInvincible) return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

		if (CurrentHealth <= 0) Die();
		else                    PlayHitAnimation();
	}

	public void Heal(float amount)
	{
		if (_isDead) return;
		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	// ── Hit animation ─────────────────────────────────────────────────────────

	private async void PlayHitAnimation()
	{
		_isHit = true;
		_sprite.Play("getting hit");

		if (_sprite.SpriteFrames != null)
		{
			bool loops = _sprite.SpriteFrames.GetAnimationLoop("getting hit");

			if (!loops)
			{
				await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);
			}
			else
			{
				int   frameCount = _sprite.SpriteFrames.GetFrameCount("getting hit");
				float fps        = (float)_sprite.SpriteFrames.GetAnimationSpeed("getting hit");
				float duration   = (float)(frameCount / Mathf.Max(fps, 1f));
				await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
			}
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
		}

		_isHit = false;
		StartInvincibility();
	}

	// ── Death ─────────────────────────────────────────────────────────────────

	private async void Die()
	{
		_isDead = true;
		EmitSignal(SignalName.PlayerDied);

		// Disable collision
		var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (col != null)
			col.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		// Stop waves and spawner
		var tree    = GetTree();
		var wm      = tree.Root.FindChild("WaveManager", true, false) as WaveManager;
		var spawner = tree.Root.FindChild("Spawner",     true, false) as Spawner;
		if (wm      != null) wm.ProcessMode      = ProcessModeEnum.Disabled;
		if (spawner != null) spawner.ProcessMode  = ProcessModeEnum.Disabled;

		// Wait 1 second
		await ToSignal(tree.CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

		// Build overlay directly — no dependency on Level at all
		var canvas   = new CanvasLayer();
		canvas.Layer = 100;
		tree.Root.AddChild(canvas);

		var panel          = new ColorRect();
		panel.Color        = new Color(0f, 0f, 0f, 0.65f);
		panel.AnchorLeft   = 0f;
		panel.AnchorTop    = 0f;
		panel.AnchorRight  = 1f;
		panel.AnchorBottom = 1f;
		canvas.AddChild(panel);

		var label                 = new Label();
		label.Text                = "You Died!\nPress Enter to retry.";
		label.AnchorLeft          = 0.5f;
		label.AnchorTop           = 0.5f;
		label.AnchorRight         = 0.5f;
		label.AnchorBottom        = 0.5f;
		label.OffsetLeft          = -260f;
		label.OffsetRight         =  260f;
		label.OffsetTop           = -70f;
		label.OffsetBottom        =  70f;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.LabelSettings       = new LabelSettings { FontSize = 40, FontColor = Colors.White };
		canvas.AddChild(label);

		// Wait for Enter, then remove overlay and go to title
		while (true)
		{
			await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
			if (Input.IsActionJustPressed("ui_accept"))
			{
				canvas.QueueFree();
				tree.ChangeSceneToFile("res://scenes/world/Game_Starting.tscn");
				return;
			}
		}
	}

	// ── Invincibility ─────────────────────────────────────────────────────────

	private async void StartInvincibility()
	{
		_isInvincible = true;
		BlinkSprite();
		await ToSignal(GetTree().CreateTimer(InvincibilityDuration), SceneTreeTimer.SignalName.Timeout);
		_isInvincible    = false;
		_sprite.Modulate = Colors.White;
		_sprite.Visible  = true;
	}

	private async void BlinkSprite()
	{
		const float interval = 0.1f;
		while (_isInvincible)
		{
			_sprite.Visible = !_sprite.Visible;
			await ToSignal(GetTree().CreateTimer(interval), SceneTreeTimer.SignalName.Timeout);
		}
		_sprite.Visible = true;
	}
}
