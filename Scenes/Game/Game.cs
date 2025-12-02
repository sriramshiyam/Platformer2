using Godot;

namespace Platformer2;

public partial class Game : Node2D
{
	PackedScene arrowScene;

	public override void _Ready()
	{
		arrowScene = GD.Load<PackedScene>("res://Scenes/Arrow/Arrow.tscn");
	}

	public override void _EnterTree()
	{
		SignalBus.I.SpawnArrow += OnSpawnArrow;
	}

	private void OnSpawnArrow(Vector2 position, bool flipH)
	{
		Arrow arrow = arrowScene.Instantiate<Arrow>();
		AddChild(arrow);
		arrow.GlobalPosition = position;
		if (flipH)
		{
			arrow.FlipH();
		}
	}
}
