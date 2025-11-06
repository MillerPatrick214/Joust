using Godot;
using System;

public partial class StepHandler : Node3D
{
    public RayCast3D RightRay;
    public RayCast3D LeftRay;


    public Vector3 StepDownTargetR = Vector3.Zero;
    public Vector3 StepDownTargetL = Vector3.Zero;
    public override void _Ready()
    {
        RightRay = GetNodeOrNull<RayCast3D>("RightRay");
        LeftRay = GetNodeOrNull<RayCast3D>("LeftRay");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        StepDownTargetR = RightRay.GetCollisionPoint();
        StepDownTargetL = LeftRay.GetCollisionPoint();
    }


}
