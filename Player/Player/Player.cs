using Godot;
using System;
using Godot.Collections;
public partial class Player : CharacterBody3D
{

    [Export] public float Speed { get; set; } = 10f;
    [Export] public float FallAcceleration { get; set; } = 9.8f;
    [Export] public float AirManueverSpeed { get; set; } = 5f;

    [Export] public RigidBody3D HeldItem;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector2 _mouse;

    public AnimationPlayer AnimPlayer;

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

    public override void _PhysicsProcess(double delta)
    {
        // Ground and Vertical Movement
        var direction = Vector3.Zero;

        _targetVelocity.X = direction.X * Speed;
        _targetVelocity.Z = direction.Z * Speed;

        if (!IsOnFloor()) _targetVelocity.Y -= FallAcceleration * (float)delta;
        

        Velocity = _targetVelocity;
        MoveAndSlide();
    }

    public void EquipWeapon(RigidBody3D Equipped)
    {
        if (_equipped != null)
        {
            _equipped = Equipped;

        }

    }

    

}
