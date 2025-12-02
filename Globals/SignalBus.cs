using Godot;

namespace Platformer2;

public partial class SignalBus : Node
{
    public static SignalBus I { get; private set; }
    [Signal] public delegate void SpawnArrowEventHandler(Vector2 position, bool flipH);

    public override void _EnterTree()
    {
        I = this;
    }

    public void EmitSpawnArrow(Vector2 position, bool flipH)
    {
        EmitSignal(SignalName.SpawnArrow, position, flipH);
    }
}