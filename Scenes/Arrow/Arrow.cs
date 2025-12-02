using Godot;

namespace Platformer2;

public partial class Arrow : Area2D
{
	Vector2 SPEED;
	Sprite2D sprite;
	VisibleOnScreenNotifier2D onScreenNotifier;

	public override void _Ready()
	{
		SPEED = new Vector2(350f, 0);

		sprite = GetNode<Sprite2D>("Sprite2D");
		onScreenNotifier = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");

		onScreenNotifier.ScreenExited += OnScreenExited;
	}

	private void OnScreenExited()
    {
        QueueFree();
    }

	public override void _Process(double delta)
	{
		Position += SPEED * (float)delta;
	}

	public void FlipH()
	{
		sprite.FlipH = true;
		SPEED.X *= -1f;
	}
}
