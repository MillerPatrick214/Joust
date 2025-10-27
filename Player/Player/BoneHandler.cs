using Godot;

public partial class BoneHandler : Node3D
{
    [Export] public Skeleton3D IKTargetSkeleton;
    [Export] public Skeleton3D PhysicsSkeleton;
    [Export] public Skeleton3D DisplaySkeleton;


    [ExportGroup("Linear Controls")]
    [Export] public float LinearStiffness = 10000f;   // 100x stronger
    [Export] public float LinearDampening = 1000f;    // 100x stronger
    [Export] public float MaxLinearForce = 100000f;


    [ExportGroup("Angular Controls")]


    [Export] public float AngularStiffness = 50000f;  // 250x stronger
    [Export] public float AngularDampening = 5000f;   // 250x stronger
    [Export] public float MaxTorque = 100000f;

    private CharacterBody3D _player;
    private PhysicalBoneSimulator3D _sim;
    private Godot.Collections.Array<PhysicalBone3D> _bones = new();
    private Godot.Collections.Array<StringName> _boneNames = new();

    public override void _Ready()
    {
        _player = GetParent<CharacterBody3D>();

        // pull PhysicalBoneSimulator3D from parent PhysicsSkeleton Skeleton3D
        foreach (var c in PhysicsSkeleton.GetChildren())
        {
            if (c is PhysicalBoneSimulator3D pbs) _sim = pbs;
        }

        // loop thorugh PhysicalBoneSimulator3D and collect a list of child nodes
        foreach (var cc in _sim.GetChildren())
        {
            if (cc is PhysicalBone3D pb)
            {
                _bones.Add(pb);
            }
        }

        _sim.PhysicalBonesStartSimulation();

    }


    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;


        foreach (var pb in _bones) { 
            
            PhysicsSkeleton.GlobalTransform = IKTargetSkeleton.GlobalTransform;

            int pbID = pb.GetBoneId();

            // GD.PrintErr($"PhysicsSkeleton.GetBoneGlobalPose(pbID): {PhysicsSkeleton.GetBoneGlobalPose(pbID)}");

            Transform3D currentPose = pb.GlobalTransform;
            GD.Print($"PhysicsSkeleton currentPose: {currentPose}");
            Transform3D targetPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(pbID);
            GD.Print($"IKTargetSkeleton targetPose: {targetPose}");


            Vector3 posErr = targetPose.Origin - currentPose.Origin;


            Vector3 force = posErr * LinearStiffness + (-pb.LinearVelocity) * LinearDampening;
            //GD.Print($"force: {force}");
            if (force.Length() > MaxLinearForce) force = force.Normalized() * MaxLinearForce;
            Vector3 localForce = pb.GlobalTransform.Basis.Inverse() * force;
            PhysicsServer3D.BodyApplyCentralForce(pb.GetRid(), force);

            Quaternion currentQuat = new Quaternion(currentPose.Basis.Orthonormalized());
            Quaternion targetQuat = new Quaternion(targetPose.Basis.Orthonormalized());
        
            // Calculate rotation error quaternion
            Quaternion errorQuat = (targetQuat * currentQuat.Inverse()).Normalized();
            
            // Convert to axis-angle
            Vector3 axis = errorQuat.GetAxis();
            float angle = errorQuat.GetAngle();
            GD.Print($"axis: {axis}");
            GD.Print($"angle: {angle}");
            
            Vector3 torque = axis * angle * AngularStiffness - pb.AngularVelocity * AngularDampening;
            if (torque.Length() > MaxTorque) torque = torque.Normalized() * MaxTorque;
            PhysicsServer3D.BodyApplyTorque(pb.GetRid(), torque);
            
        }
    }
}

