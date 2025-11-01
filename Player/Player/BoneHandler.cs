using System.Collections.Generic;
using Godot;

public partial class BoneHandler : Node3D
{
    [Export] public Skeleton3D IKTargetSkeleton;
    [Export] public Skeleton3D PhysicsSkeleton;
    [Export] public Skeleton3D DisplaySkeleton;

    [ExportGroup("Linear Controls")]
    [Export] public float LinearStiffness = 1200f;
    [Export] public float LinearDampening = 40f;
    [Export] public float MaxLinearForce = 9999f;

    [ExportGroup("Angular Controls")]
    [Export] public float AngularStiffness = 1750.0f;
    [Export] public float AngularDampening = 30.0f;
    [Export] public float MaxTorque = 2000.0f;


    [ExportGroup("Stability")]
    [Export] public float MaxPositionError = 1f; // Clamp extreme errors
    [Export] public float MaxAngularError = Mathf.Pi; // Clamp to 180 degrees
    //[Export(PropertyHint.None, "suffix:degrees")] public double MinimumAngleForForce = 2.0f;

    private CharacterBody3D _player;
    private PhysicalBoneSimulator3D _sim;
    private Godot.Collections.Array<PhysicalBone3D> _bones = new();
    private Godot.Collections.Dictionary<int, Transform3D> _boneCorrections = new();
    private Godot.Collections.Dictionary<PhysicalBone3D, Transform3D> _previousTargets = new();


    public override void _Ready()
    {
        PhysicsSkeleton.GlobalTransform = IKTargetSkeleton.GlobalTransform;
        IKTargetSkeleton.SkeletonUpdated += DriveBones;
        _player = GetParent<CharacterBody3D>();

        foreach (var c in PhysicsSkeleton.GetChildren())
        {
            if (c is PhysicalBoneSimulator3D pbs) _sim = pbs;
        }

        foreach (var cc in _sim.GetChildren())
        {
            if (cc is PhysicalBone3D pb)
            {
                _bones.Add(pb);
                int boneID = pb.GetBoneId();

                // Calculate initial offset between bone pose and physical bone
                Transform3D physBonePose = PhysicsSkeleton.GlobalTransform * PhysicsSkeleton.GetBoneGlobalPose(boneID);
                Transform3D correction = physBonePose.AffineInverse() * pb.GlobalTransform;
                _boneCorrections[boneID] = correction;

                // Initialize previous target
                _previousTargets[pb] = pb.GlobalTransform;
            }
        }

        _sim.PhysicalBonesStartSimulation();
        
    }

    public override void _PhysicsProcess(double delta)
    {
        IKTargetSkeleton.Advance(delta);

    }
    
    public void DriveBones()
    {
        
        foreach (var pb in _bones)
        {
            int pbID = pb.GetBoneId();
            float mass = pb.Mass;
            if (mass <= 0) mass = 1f;

            // Get target pose
            Transform3D targetPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(pbID);
            Transform3D correctedTarget = targetPose * _boneCorrections[pbID];

            // Get current pose
            Transform3D currentPose = pb.GlobalTransform;

            // === LINEAR (Position) Control ===
            Vector3 displacement = correctedTarget.Origin - currentPose.Origin;

            // Teleport if too far (prevents explosive forces)
            if (displacement.LengthSquared() > MaxPositionError * MaxPositionError)
            {
                pb.GlobalPosition = correctedTarget.Origin;
                pb.LinearVelocity = Vector3.Zero;
            }
            else
            {
                // Hooke's Law: F = kx - cv
                Vector3 force = (LinearStiffness * displacement) - (LinearDampening * pb.LinearVelocity);

                // Scale by mass (F = ma, so we need force proportional to mass)
                force *= mass;

                // Clamp
                if (force.Length() > MaxLinearForce)
                {
                    force = force.Normalized() * MaxLinearForce;
                }

                PhysicsServer3D.BodyApplyCentralForce(pb.GetRid(), force);
            }

            // === ANGULAR (Rotation) Control ===
            Quaternion currentQuat = new Quaternion(currentPose.Basis.Orthonormalized());
            Quaternion targetQuat = new Quaternion(correctedTarget.Basis.Orthonormalized());
            
            // Normalize quaternions
            currentQuat = currentQuat.Normalized();
            targetQuat = targetQuat.Normalized();
            
            // Calculate rotation error (shortest path from current to target)
            Quaternion errorQuat = targetQuat * currentQuat.Inverse();
            errorQuat = errorQuat.Normalized();
            
            // Handle quaternion double-cover (ensure shortest rotation path)
            if (errorQuat.W < 0)
            {
                errorQuat = new Quaternion(-errorQuat.X, -errorQuat.Y, -errorQuat.Z, -errorQuat.W);
            }
            
            // Convert quaternion error to axis-angle representation
            // This gives us the angular displacement vector
            Vector3 angularDisplacement = Vector3.Zero;
            float angle = errorQuat.GetAngle();
            
            // Only calculate if angle is significant (avoid numerical issues at small angles)
            if (angle > 0.001f)
            {
                Vector3 axis = errorQuat.GetAxis();
                angularDisplacement = axis * angle;
            }
            
            // Hooke's Law for rotation: T = kθ - cω
            // k = spring stiffness, θ = angular displacement
            // c = damping coefficient, ω = angular velocity
            Vector3 torque = (AngularStiffness * angularDisplacement) - (AngularDampening * pb.AngularVelocity);

            // Clamp
            if (torque.Length() > MaxTorque)
            {
                torque = torque.Normalized() * MaxTorque;
            }

        }
    }



    private float NormalizeAngle(float angle)
    {
        while (angle > Mathf.Pi) angle -= Mathf.Tau;
        while (angle < -Mathf.Pi) angle += Mathf.Tau;
        return angle;
    }
}

