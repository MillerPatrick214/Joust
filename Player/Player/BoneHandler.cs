using Godot;
using System.Collections.Generic;

public partial class BoneHandler : Node3D
{
    [Export] public Skeleton3D IKTargetSkeleton;
    [Export] public Skeleton3D PhysicsSkeleton;
    [Export] public PhysicalBone3D HipBone;
    [Export] public Skeleton3D DisplaySkeleton;


    [Export] public float kP = 80f;
    [Export] public float kD = 12f;
    [Export] public float MaxTorque = 200f;
    [Export] public float kPHip = 400f;
    [Export] public float kDHip = 24f;
    [Export] public float HipMaxHoldForce = 400f;


    private int _hipID;
    private Transform3D pelvisAnchor;
    private PhysicalBoneSimulator3D _sim;
    private List<PhysicalBone3D> _bones = new();

    public override void _Ready()
    {
        _hipID = HipBone.GetBoneId();

        foreach (var c in PhysicsSkeleton.GetChildren())
        {
            if (c is PhysicalBoneSimulator3D pbs) _sim = pbs;
        }
        foreach (var cc in _sim.GetChildren())
        {
            if (cc is PhysicalBone3D pb) _bones.Add(pb);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;

        // applies quaternion correction forces to physics bones to move towards target skeleton
        foreach (PhysicalBone3D pb in _bones)
        {
            int pbID = pb.GetBoneId();

            Transform3D currentPose = PhysicsSkeleton.GlobalTransform * PhysicsSkeleton.GetBoneGlobalPose(pbID);
            Transform3D targetPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(pbID);

            const float SNAP = 0.40f;
            if ((targetPose.Origin - currentPose.Origin).LengthSquared() > SNAP * SNAP)
            {
                Transform3D snapped = currentPose; snapped.Origin = targetPose.Origin;
                PhysicsServer3D.BodySetState(pb.GetRid(), PhysicsServer3D.BodyState.Transform, snapped);
                PhysicsServer3D.BodySetState(pb.GetRid(), PhysicsServer3D.BodyState.LinearVelocity, Vector3.Zero);
                PhysicsServer3D.BodySetState(pb.GetRid(), PhysicsServer3D.BodyState.AngularVelocity, Vector3.Zero);
                continue;
            }

            if (pbID == _hipID)
            {
                Vector3 posErr = targetPose.Origin - currentPose.Origin;
                if (posErr.Length() > 0.005f)
                {
                    Vector3 f = posErr * kPHip + (-pb.LinearVelocity) * kDHip;
                    if (f.Length() > HipMaxHoldForce) f = f.Normalized() * HipMaxHoldForce;
                    // PhysicsServer3D.BodyApplyCentralImpulse(pb.GetRid(), f * d);
                    pb.LinearVelocity += f * d;
                }

            }

            Quaternion cQuat = new Quaternion(currentPose.Basis);
            Quaternion tQuat = new Quaternion(targetPose.Basis);
            Quaternion errQuat = (tQuat * cQuat.Inverse()).Normalized();
            Vector3 qAxis = errQuat.GetAxis();
            float qAngle = errQuat.GetAngle();
            if (Mathf.Abs(qAngle) > 0.01f)
            {
                Vector3 torque = qAxis * (qAngle * kP) - pb.AngularVelocity * kD;
                if (torque.Length() > MaxTorque) torque = torque.Normalized() * MaxTorque;
                // PhysicsServer3D.BodyApplyTorqueImpulse(pb.GetRid(), torque * d);
                pb.AngularVelocity += torque * d;
            }
        }
    }
}

