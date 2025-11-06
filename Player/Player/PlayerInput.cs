using Godot;
using System;


public partial class PlayerInput : MultiplayerSynchronizer
{
    [Export]
    public bool jumping = false;
    [Export]
    public Vector2 inputDir;
    public override void _Ready()
    {
        SetProcess(Multiplayer.GetUniqueId() == GetMultiplayerAuthority());
    }

    public override void _Process(double delta)
    {
        inputDir = Input.GetVector("Forward", "Back", "Left", "Right");
        if (Input.IsActionJustPressed("Jump"))
        {
            Rpc(nameof(Jump));
        }
    }

    [Rpc(CallLocal = true)]
    public void Jump()
    {
        GD.Print("Attempting to jump");
        jumping = true;
    }


}
