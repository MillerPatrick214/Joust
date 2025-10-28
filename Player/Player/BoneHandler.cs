using System.Collections.Generic;
using System.Threading;
using Godot;

public partial class BoneHandler : Node3D
{
    [Export] public Skeleton3D IKTargetSkeleton;
    [Export] public Skeleton3D PhysicsSkeleton;
    [Export] public Skeleton3D DisplaySkeleton;


    [ExportGroup("Linear Controls")]
    [Export] public float LinearStiffness = 500f;   
    [Export] public float LinearDampening = 10f;    // 100x stronger
    [Export] public float MaxLinearForce = 500f;


    [ExportGroup("Angular Controls")]


    [Export] public float AngularStiffness = 300f;  // 250x stronger
    [Export] public float AngularDampening = 50f;   // 250x stronger
    [Export] public float MaxTorque = 500f;

    private CharacterBody3D _player;
    private PhysicalBoneSimulator3D _sim;
    private Godot.Collections.Array<PhysicalBone3D> _bones = new();

    private Godot.Collections.Dictionary<int, Transform3D> _boneCorrections= new();
    public override void _Ready()
    {
        PhysicsSkeleton.GlobalTransform = IKTargetSkeleton.GlobalTransform;
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
                int boneID = pb.GetBoneId();

                Transform3D physBonePose = PhysicsSkeleton.Transform * PhysicsSkeleton.GetBonePose(boneID);
                Transform3D correction = physBonePose.AffineInverse() * pb.GlobalTransform;
                _boneCorrections[boneID] = correction;

            }
        }

        _sim.PhysicalBonesStartSimulation();

    }

    private void MeasurePhysicsBoneCorrection(PhysicalBone3D pb, int id)
    {
        
        Transform3D physBonePose = PhysicsSkeleton.GlobalTransform * PhysicsSkeleton.GetBoneGlobalPose(id);
        Transform3D correction = physBonePose.AffineInverse() * pb.GlobalTransform; // C ~ constant
    }


    public override void _PhysicsProcess(double delta)
    {
        foreach (var pb in _bones)
        {
            int pbID = pb.GetBoneId();
            GD.Print($"pbID{pbID}\n name{pb.Name}");

            // GD.PrintErr($"PhysicsSkeleton.GetBoneGlobalPose(pbID): {PhysicsSkeleton.GetBoneGlobalPose(pbID)}");

            Transform3D currentPose = pb.GlobalTransform;
            //GD.Print($"PhysicsSkeleton currentPose: {currentPose}");
            Transform3D targetPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(pbID);
            //GD.Print($"IKTargetSkeleton targetPose: {targetPose}");

            Transform3D correctedTarget = targetPose * _boneCorrections[pbID];


            Vector3 posErr = correctedTarget.Origin - currentPose.Origin;


            Vector3 force = posErr * LinearStiffness + (-pb.LinearVelocity) * LinearDampening;
            //GD.Print($"force: {force}");
            if (force.Length() > MaxLinearForce) force = force.Normalized() * MaxLinearForce;
            PhysicsServer3D.BodyApplyCentralForce(pb.GetRid(), force);

            Quaternion currentQuat = new Quaternion(currentPose.Basis.Orthonormalized());
            Quaternion targetQuat = new Quaternion(correctedTarget.Basis.Orthonormalized());

            //     // Calculate rotation error quaternion
            Quaternion errorQuat = (targetQuat * currentQuat.Inverse()).Normalized();

            //     // Convert to axis-angle
            Vector3 axis = errorQuat.GetAxis();
            float angle = errorQuat.GetAngle();
            //GD.Print($"axis: {axis}");
            //GD.Print($"angle: {angle}");

            Vector3 torque = axis * angle * AngularStiffness - pb.AngularVelocity * AngularDampening;
            if (torque.Length() > MaxTorque) torque = torque.Normalized() * MaxTorque;
            PhysicsServer3D.BodyApplyTorque(pb.GetRid(), torque);
        }
    }    
}

