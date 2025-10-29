using Godot;
using System;
using Godot.Collections;

public partial class Player : CharacterBody3D
{

    [Export] public float Speed { get; set; } = 15f;
    [Export] public float FallAcceleration { get; set; } = 9.8f;

    [Export] public RigidBody3D HeldItem;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector2 _mouse;

    private AnimationPlayer _animPlayer;

    public override void _Ready()
    {
        Vector2 Resolution = GetViewport().GetVisibleRect().Size; //This needs to be elsewhere eventually. This will change if viewport size changes during gameplay\
        //Input.MouseMode = Input.MouseModeEnum.Confined;
        _animPlayer = GetNodeOrNull<AnimationPlayer>("Armature/Skeleton3D/AnimationPlayer");
        _animPlayer.Play("Armature|Run");
        _animPlayer.Active = true;
    }

    public override void _Input(InputEvent @e)
    {
        if (@e.IsActionPressed("pause")) GetTree().Quit(); 

    }

    public override void _PhysicsProcess(double delta)
    {
        // Ground and Vertical Movement
        var direction = Vector3.Zero;

        if (Input.IsActionPressed("move_right")) direction.X += 1.0f;
        if (Input.IsActionPressed("move_left")) direction.X -= 1.0f;
        if (Input.IsActionPressed("move_back")) direction.Z += 1.0f;
        if (Input.IsActionPressed("move_forward")) direction.Z -= 1.0f;

        _targetVelocity.X = direction.X * Speed;
        _targetVelocity.Z = direction.Z * Speed;

        if (!IsOnFloor()) _targetVelocity.Y -= FallAcceleration * (float)delta;

        Velocity = _targetVelocity;
        MoveAndSlide();
    }

}
