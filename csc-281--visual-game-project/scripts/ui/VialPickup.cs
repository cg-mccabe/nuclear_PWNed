using Godot;

/// <summary>
/// VialPickup — Area2D that applies an effect to the player on contact.
///
/// VialType.Blood → restores 100 HP
/// VialType.Goop  → permanently increases Speed by 20
///
/// Assign VialType in the Inspector, or leave default (Blood).
/// </summary>
public partial class VialPickup : Area2D
{
	public enum VialType { Blood, Goop }

	[Export] public VialType Type         { get; set; } = VialType.Blood;
	[Export] public float    HealAmount   { get; set; } = 100f;
	[Export] public float    SpeedBoost   { get; set; } = 20f;
	/// <summary>How long the pickup bobs before auto-despawning (0 = never).</summary>
	[Export] public float    Lifetime     { get; set; } = 15f;

	private bool _collected = false;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		// Auto-despawn after Lifetime seconds so drops don't pile up forever
		if (Lifetime > 0f)
		{
			var timer = GetTree().CreateTimer(Lifetime);
			timer.Timeout += QueueFree;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_collected) return;
		if (body is not Player player) return;

		_collected = true;

		switch (Type)
		{
			case VialType.Blood:
				player.Heal(HealAmount);
				GD.Print($"Player drank Blood Vial — healed {HealAmount} HP");
				break;

			case VialType.Goop:
				player.Speed += SpeedBoost;
				GD.Print($"Player drank Goop Vial — speed +{SpeedBoost} (now {player.Speed})");
				break;
		}

		QueueFree();
	}
}
