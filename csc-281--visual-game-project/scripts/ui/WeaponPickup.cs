using Godot;

/// <summary>
/// WeaponPickup — Area2D that grants a weapon to the player on contact.
///
/// Setup in Godot:
///   Node type   : Area2D
///   Children    : Sprite2D (bat or pipe graphic) + CollisionShape2D
///   collision_layer = 0
///   collision_mask  = 1  (player body layer)
///
/// Set WeaponToGrant in the Inspector (or via WaveManager when it instantiates the pickup).
/// </summary>
public partial class WeaponPickup : Area2D
{
	[Export] public Player.WeaponType WeaponToGrant { get; set; } = Player.WeaponType.Bat;

	/// <summary>
	/// Label shown above the pickup, e.g. "Baseball Bat — Press E to pick up".
	/// Optional — leave null if you don't have a Label child.
	/// </summary>
	[Export] public Label PromptLabel { get; set; }

	private bool _collected = false;
	

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		if (PromptLabel != null) PromptLabel.Visible = false;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_collected) return;
		if (body is not Player player) return;

		_collected = true;
		player.EquipWeapon(WeaponToGrant);
		GD.Print($"Player picked up {WeaponToGrant}");
		QueueFree();
	}

	// Optional: show a prompt when the player is nearby
	public override void _Process(double delta)
	{
		if (PromptLabel == null) return;
		// Show label when player is within 60px
		var players = GetTree().GetNodesInGroup("player");
		if (players.Count == 0) return;
		var p = players[0] as Node2D;
		PromptLabel.Visible = p != null && GlobalPosition.DistanceTo(p.GlobalPosition) < 60f;
	}
}
