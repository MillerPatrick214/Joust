// Player.cs
using Godot;
using System;

public partial class Player : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	private PlayerInput _playerInput;
	private Camera3D _camera;
	
	[Export]
	public int PlayerID
	{
		get => _playerId;
		set
		{
			_playerId = value;
			if (GetNodeOrNull<PlayerInput>("PlayerInput") is Node PlayerInputNode)
			{
				PlayerInputNode.SetMultiplayerAuthority(value);
	
			}

			else
			{
				GD.PrintErr("PlayerInput node not found!");
			}
		}
	}
	private int _playerId = 1;

	public override void _Ready()
	{
		_camera = GetNodeOrNull<Camera3D>("Camera3D");
		_playerInput = GetNodeOrNull<PlayerInput>("PlayerInput");
		
		GD.Print($"Player._Ready() - PlayerID: {PlayerID}, MyUniqueID: {Multiplayer.GetUniqueId()}, Match: {PlayerID == Multiplayer.GetUniqueId()}");
		
		if (PlayerID == Multiplayer.GetUniqueId())
		{
			_camera.Current = true;
			GD.Print($"✓ Camera activated for player {PlayerID}");
		}
	}

	private void SetupCamera()
	{
		if (PlayerID == Multiplayer.GetUniqueId())
		{
			_camera.Current = true;
			GD.Print($"Camera activated for player {PlayerID}");
		}
	}
	

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity += GetGravity() * (float)delta;

        if (_playerInput.jumping && IsOnFloor())
            velocity.Y = JumpVelocity;

        _playerInput.jumping = false;

        Vector2 inputDir = _playerInput.inputDir;
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
