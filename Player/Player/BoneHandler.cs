using System.Collections.Generic;
using Godot;

public partial class BoneHandler : Node3D
{
    [Export] public Skeleton3D IKTargetSkeleton;
    [Export] public Skeleton3D PhysicsSkeleton;
    [Export] public Skeleton3D DisplaySkeleton;
    
    [ExportGroup("Linear Controls")]
    [Export] public float LinearStiffness = 500f;
    [Export] public float LinearDampening = 10f;
    [Export] public float MaxLinearForce = 5000f;
    
    [ExportGroup("Angular Controls")]
    [Export] public float AngularStiffness = 300f;
    [Export] public float AngularDampening = 50f;
    [Export] public float MaxTorque = 500f;
    
    [ExportGroup("Stability")]
    [Export] public float MaxPositionError = 0.5f; // Clamp extreme errors
    [Export] public float MaxAngularError = Mathf.Pi; // Clamp to 180 degrees
    
    private CharacterBody3D _player;
    private PhysicalBoneSimulator3D _sim;
    private Godot.Collections.Array<PhysicalBone3D> _bones = new();
    private Godot.Collections.Dictionary<int, Transform3D> _boneCorrections = new();
    private Godot.Collections.Dictionary<PhysicalBone3D, Transform3D> _previousTargets = new();
    
    public override void _Ready()
    {
        PhysicsSkeleton.GlobalTransform = IKTargetSkeleton.GlobalTransform;
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
        float dt = (float)delta;
        if (dt <= 0) return;
        
        foreach (var pb in _bones)
        {
            int pbID = pb.GetBoneId();
            
            // Get current state
            Transform3D currentPose = pb.GlobalTransform;
            float mass = pb.Mass;
            if (mass <= 0) mass = 1f; // Safety check
            
            // Get target pose
            Transform3D targetPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(pbID);
            Transform3D correctedTarget = targetPose * _boneCorrections[pbID];
            
            // === LINEAR (Position) Control ===
            Vector3 posError = correctedTarget.Origin - currentPose.Origin;
            
            // Clamp extreme position errors for stability
            if (posError.Length() > MaxPositionError)
            {
                posError = posError.Normalized() * MaxPositionError;
            }
            
            // Estimate target velocity from position change
            Vector3 targetVelocity = (correctedTarget.Origin - _previousTargets[pb].Origin) / dt;
            Vector3 velocityError = targetVelocity - pb.LinearVelocity;
            
            // Spring-damper force (scaled by mass)
            Vector3 force = (posError * LinearStiffness + velocityError * LinearDampening) * mass;
            
            if (force.Length() > MaxLinearForce)
            {
                force = force.Normalized() * MaxLinearForce;
            }
            
            PhysicsServer3D.BodyApplyCentralForce(pb.GetRid(), force);
            
            // === ANGULAR (Rotation) Control ===
            Quaternion currentQuat = new Quaternion(currentPose.Basis.Orthonormalized());
            Quaternion targetQuat = new Quaternion(correctedTarget.Basis.Orthonormalized());
            
            // Calculate shortest rotation from current to target
            Quaternion errorQuat = (targetQuat * currentQuat.Inverse()).Normalized();
            
            // Handle quaternion double-cover (ensure shortest path)
            if (errorQuat.W < 0)
            {
                errorQuat = new Quaternion(-errorQuat.X, -errorQuat.Y, -errorQuat.Z, -errorQuat.W);
            }
            
            Vector3 axis = errorQuat.GetAxis();
            float angle = errorQuat.GetAngle();
            
            // Clamp extreme angular errors
            if (angle > MaxAngularError)
            {
                angle = MaxAngularError;
            }
            
            // Calculate angular error vector
            Vector3 angularError = axis * angle;
            
            // Estimate target angular velocity
            Quaternion previousTargetQuat = new Quaternion(_previousTargets[pb].Basis.Orthonormalized());
            Quaternion targetRotDelta = (targetQuat * previousTargetQuat.Inverse()).Normalized();
            Vector3 targetAngularVelocity = targetRotDelta.GetAxis() * targetRotDelta.GetAngle() / dt;
            
            Vector3 angularVelocityError = targetAngularVelocity - pb.AngularVelocity;
            
            // Torque = spring force + damping force (scaled by inertia approximation)
            Vector3 torque = (angularError * AngularStiffness + angularVelocityError * AngularDampening) * mass;
            
            if (torque.Length() > MaxTorque)
            {
                torque = torque.Normalized() * MaxTorque;
            }
            
            PhysicsServer3D.BodyApplyTorque(pb.GetRid(), torque);
            
            // Store current target for next frame
            _previousTargets[pb] = correctedTarget;
        }
    }
}