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
        inputDir = Input.GetVector("move_right", "move_left", "move_back", "move_forward");

        if (Input.IsActionJustPressed("jump"))
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
