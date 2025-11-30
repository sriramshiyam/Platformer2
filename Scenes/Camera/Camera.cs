using Godot;

namespace Platformer2;

public partial class Camera : Camera2D
{
	const float X_POSITION = 50f;

	public void ChangePosition(bool left)
	{
		Vector2 position = Position;
		position.X = left ? -X_POSITION : X_POSITION;
		Position = position;
	}
}
