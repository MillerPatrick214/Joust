using System.ComponentModel;
using Godot;

public partial class BoneHandler : Node3D
{
    [Export] public Skeleton3D IKTargetSkeleton;
    [Export] public Skeleton3D PhysicsSkeleton;
    [Export] public Skeleton3D DisplaySkeleton;


    [ExportGroup("Linear Controls")]
    [Export] public float LinearStiffness = 1200f;  //FIXME Need proper suffix for these values!
    [Export] public float LinearDampening = 40f;
    [Export] public float MaxLinearForce = 9999f;


    [ExportGroup("Angular Controls")]
    [Export] public float AngularStiffness = 4000f;  //FIXME Need proper suffix for these values!
    [Export] public float AngularDampening = 80f;
    [Export] public float MaxTorque = 9999f;

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

    public override void _Process(double delta)
    {
        IKTargetSkeleton.GlobalTransform = _player.GlobalTransform;
    }


    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // applies quaternion correction forces to physics bones to move towards target skeleton
        foreach (PhysicalBone3D pb in _bones)
        {
            int pbID = pb.GetBoneId();

            Transform3D currentPose = PhysicsSkeleton.GlobalTransform * PhysicsSkeleton.GetBoneGlobalPose(pbID);
            Transform3D targetPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(pbID);

            const float SNAP = 1.0f;
            if ((targetPose.Origin - currentPose.Origin).LengthSquared() > SNAP * SNAP)
            {
                Transform3D snapped = currentPose;
                snapped.Origin = targetPose.Origin;
                PhysicsServer3D.BodySetState(pb.GetRid(), PhysicsServer3D.BodyState.Transform, snapped);
                PhysicsServer3D.BodySetState(pb.GetRid(), PhysicsServer3D.BodyState.LinearVelocity, Vector3.Zero);
                PhysicsServer3D.BodySetState(pb.GetRid(), PhysicsServer3D.BodyState.AngularVelocity, Vector3.Zero);
                continue;
            }

            Vector3 posErr = targetPose.Origin - currentPose.Origin;

            if (posErr.Length() > 0.0005f)
            {
                Vector3 force = posErr * LinearStiffness + (-pb.LinearVelocity) * LinearDampening;
                GD.Print($"force: {force}");
                if (force.Length() > MaxLinearForce) force = force.Normalized() * MaxLinearForce;
                // PhysicsServer3D.BodyApplyCentralImpulse(pb.GetRid(), force * dt);
                pb.LinearVelocity += force * dt;
            }

            Quaternion cQuat = new Quaternion(currentPose.Basis);
            Quaternion tQuat = new Quaternion(targetPose.Basis);
            Quaternion errQuat = (tQuat * cQuat.Inverse()).Normalized();
            Vector3 qAxis = errQuat.GetAxis();
            float qAngle = errQuat.GetAngle();

            if (Mathf.Abs(qAngle) > 0.0001f)
            {
                Vector3 torque = (qAxis * qAngle * AngularStiffness) + (-pb.AngularVelocity) * AngularDampening;
                if (torque.Length() > MaxTorque) torque = torque.Normalized() * MaxTorque;
                // PhysicsServer3D.BodyApplyTorqueImpulse(pb.GetRid(), torque * dt);
                pb.AngularVelocity += torque * dt;
            }
        }
    }
}

