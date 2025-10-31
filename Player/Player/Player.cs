// Player.cs
using Godot;
using System;
<<<<<<< Updated upstream
<<<<<<< Updated upstream

=======
using Godot.Collections;
>>>>>>> Stashed changes
=======
using Godot.Collections;
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
	private void SetupCamera()
	{
		if (PlayerID == Multiplayer.GetUniqueId())
		{
			_camera.Current = true;
			GD.Print($"Camera activated for player {PlayerID}");
		}
	}
	
=======
    public AnimationPlayer AnimPlayer;

    private BoneAttachment3D _rHandBoneAttachement;

    private RigidBody3D _equipped;


    //private GodotIKEffector _rHandEffector;
    //private GodotIKEffector _lHandEffector; 

    private BoneAttachment3D _rHandBoneAttachement;

    private RigidBody3D _equipped;


    //private GodotIKEffector _rHandEffector;
    //private GodotIKEffector _lHandEffector; 

    public override void _Ready()
    {
        Vector2 Resolution = GetViewport().GetVisibleRect().Size; //This needs to be elsewhere eventually. This will change if viewport size changes during gameplay\
        //Input.MouseMode = Input.MouseModeEnum.Confined;
        AnimPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        _rHandBoneAttachement = GetNodeOrNull<BoneAttachment3D>("BoneHandler/Skeleton3D/Bone Attachment Hand_R");
        if (_rHandBoneAttachement.GetChildCount() > 0 && _rHandBoneAttachement.GetChild(0) is RigidBody3D equippedBody)
        {
            EquipWeapon(equippedBody);
        }
    }

    public override void _Input(InputEvent @e)
    {
        if (@e.IsActionPressed("pause")) GetTree().Quit(); 

    }
>>>>>>> Stashed changes

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity += GetGravity() * (float)delta;

<<<<<<< Updated upstream
        if (_playerInput.jumping && IsOnFloor())
            velocity.Y = JumpVelocity;
=======
        if (!IsOnFloor()) _targetVelocity.Y -= FallAcceleration * (float)delta;
        
<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
=======

    public void EquipWeapon(RigidBody3D Equipped)
    {
        if (_equipped != null)
        {
            _equipped = Equipped;

        }

    }

    

<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes
}
