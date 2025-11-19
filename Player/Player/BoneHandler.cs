using Godot;
using System.Collections.Generic;


[Tool]
public partial class BoneHandler : Node3D
{
    [ExportGroup("Skeletons")]
    [Export] public Skeleton3D IKTargetSkeleton;
    [Export] public Skeleton3D PhysicsSkeleton;

    [ExportToolButton("Fix PhysicsSkeleton Transforms", Icon = "Skeleton3D")] public Callable FixSkeletonButton => Callable.From(FixPhysicsSkeleton);
    [ExportToolButton("Fix IK Target Transforms", Icon = "Marker3D")] public Callable FixIKButton => Callable.From(ResetIKTargets);

    [ExportGroup("Linear Controls")]
    [Export] public float LinearStiffness = 600;
    [Export] public float LinearDampening = 30;
    [Export] public float MaxLinearForce = 2500;

    [ExportGroup("Angular Controls")]
    [Export] public float AngularStiffness = 300;
    [Export] public float AngularDampening = 15f;
    [Export] public float MaxTorque = 1000;


    [ExportGroup("Stability")]
    [Export] public float MaxPositionError = 1f; // Clamp extreme errors

    private CharacterBody3D _player;
    private PhysicalBoneSimulator3D _sim;
    private List<PhysicalBone3D> _bones = new();
    private Godot.Collections.Dictionary<int, Transform3D> _boneCorrections = new();
    private Godot.Collections.Dictionary<PhysicalBone3D, Transform3D> _previousTargets = new();
    private Godot.Collections.Dictionary<int, Transform3D> _cachedModifiedPoses = new();
    private Godot.Collections.Dictionary<int, Marker3D> _ikTargets = new();  // bone ID : Marker3D target


    // Sets physical bones and their collision shapes transformations to match the IK skeleton
    public void FixPhysicsSkeleton()
    {
        GD.Print("[BoneHandler/FixPhysicsSkeleton] Starting...");

        if (_player == null) _player = GetParentOrNull<CharacterBody3D>();
        IKTargetSkeleton.GlobalTransform = _player.GlobalTransform;
        PhysicsSkeleton.GlobalTransform = _player.GlobalTransform * IKTargetSkeleton.Transform;  //Make the skeleton face the same way as the player!

        _bones.Clear();

        foreach (var c in PhysicsSkeleton.GetChildren())
        {
            if (c is PhysicalBoneSimulator3D pbs) _sim = pbs;
        }

        if (_sim is null)
        {
            GD.PrintErr($"[BoneHandler/FixPhysicsSkeleton] Can't find PhysicalBoneSimulator3D child of {PhysicsSkeleton.Name}. Exiting...");
            return;
        }

        for (int i = 0; i < PhysicsSkeleton.GetBoneCount(); i++)
        {
            string bName = PhysicsSkeleton.GetBoneName(i);
            int bID = IKTargetSkeleton.FindBone(bName);
            if (bID == -1)
            {
                GD.PrintErr($"[BoneHandler/FixPhysicsSkeleton] Bone '{bName}' not found in IKTargetSkeleton. Skipping.");
                continue;
            }
            Transform3D ikRest = IKTargetSkeleton.GetBoneRest(bID);
            PhysicsSkeleton.SetBoneRest(i, ikRest);
        }

        foreach (var cc in _sim.GetChildren())
        {
            if (cc is PhysicalBone3D pb)
            {
                _bones.Add(pb);
                int boneID = pb.GetBoneId();
                if (boneID < 0)
                {
                    GD.Print($"[BoneHandler/FixPhysicsSkeleton] {pb.Name} has invalid bone id. Skipping...");
                    continue;
                }

                // these add bloat to the .tscn file
                pb.AngularVelocity = Godot.Vector3.Zero;
                pb.LinearVelocity = Godot.Vector3.Zero;

                Transform3D targetIKPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(boneID);
                Transform3D targetInPhys = PhysicsSkeleton.GlobalTransform.AffineInverse() * targetIKPose;

                PhysicsSkeleton.SetBoneGlobalPose(boneID, targetInPhys);
                pb.GlobalTransform = targetIKPose;
            }
        }
        GD.PrintRich("[color=green][BoneHandler/FixPhysicsSkeleton] Ran successfully.[/color]");
    }

    // Sets the transforms of IK targets (hands and feet) to match IK target skeleton
    public void ResetIKTargets()
    {
        Godot.Collections.Array<string> ikTargets = new Godot.Collections.Array<string> { "Hand_R", "Hand_L", "Foot_R", "Foot_L" };

        foreach (string target in ikTargets)
        {
            var targetIK = IKTargetSkeleton.FindChild($"IK {target}", false, false);
            var ik3D = IKTargetSkeleton.FindChild($"{target} SkeletonIK3D", false, false);
            if (targetIK is Marker3D ik && ik3D is SkeletonIK3D ik3)
            {
                string targetName = ik3.TipBone;
                int targetID = IKTargetSkeleton.FindBone(targetName);
                if (targetID >= 0)
                {
                    Transform3D targetPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(targetID);
                    ik.GlobalTransform = targetPose;
                }
                else GD.PrintErr($"[BoneHandler/ResetIKTargets] Invalid SkeletonIK3D TargetNode {targetName} ID: {targetID}");
            }
            else GD.PrintErr($"[BoneHandler/ResetIKTargets] Missing 'IK {target}' and/or '{target} SkeletonIK3D' in IKTargetSkeleton");
        }
    }

    private void StopIK()
    {
        Godot.Collections.Array<string> ikTargets = new Godot.Collections.Array<string> { "Hand_R", "Hand_L", "Foot_R", "Foot_L" };

        foreach (string target in ikTargets)
        {
            var targetIK = IKTargetSkeleton.FindChild($"IK {target}", false, false);
            var ik3D = IKTargetSkeleton.FindChild($"{target} SkeletonIK3D", false, false);
            if (targetIK is Marker3D ik && ik3D is SkeletonIK3D ik3) ik3.Stop();
        }
    }


    public override void _Ready()
    {
        PhysicsSkeleton.GlobalTransform = IKTargetSkeleton.GlobalTransform;
        _bones.Clear();
        _boneCorrections.Clear();
        IKTargetSkeleton.SkeletonUpdated += OnSkeletonUpdated;
        _player = GetParentOrNull<CharacterBody3D>();

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
                if (!correction.IsFinite())
                {
                    GD.PrintErr($"[BoneHandler.cs/_Ready] Invalid bone correction for bone {pb.Name} (ID: {boneID})");
                    correction = Transform3D.Identity; // Use identity as fallback
                }

                _boneCorrections[boneID] = correction;
                _previousTargets[pb] = pb.GlobalTransform;
            }
        }

        OnSkeletonUpdated(); // Run once to init bone cache
        _sim.PhysicalBonesStartSimulation();

    }

    public Transform3D GetBoneCachedPose(int boneID)
    {
        if (_cachedModifiedPoses.TryGetValue(boneID, out Transform3D pose)) return pose;
        else
        {
            GD.PrintErr($"[BoneHandler.cs] Failed to get cached bone pose for boneID: {boneID} ({PhysicsSkeleton.GetBoneName(boneID)}). Returning default transform...");
            return new Transform3D();
        }
    }
    private void OnSkeletonUpdated()
    {
        // Capture all modified bone poses while they're still applied
        // This happens right after all IK/modifiers have run
        foreach (var pb in _bones)
        {
            int boneId = pb.GetBoneId();

            // Get the MODIFIED global pose (includes IK)
            _cachedModifiedPoses[boneId] = IKTargetSkeleton.GetBoneGlobalPose(boneId);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        DriveBones(delta);
    }

    private void DriveBones(double delta)
    {
        foreach (var pb in _bones)
        {
            int pbID = pb.GetBoneId();
            if (pb.Mass <= 0) pb.Mass = 1f;

            if (!_cachedModifiedPoses.TryGetValue(pbID, out Transform3D modP)) continue;

            // Get target pose
            Transform3D targetPose = IKTargetSkeleton.GlobalTransform * modP;
            Transform3D correctedTarget = targetPose * _boneCorrections[pbID];

            // Get current pose
            Transform3D currentPose = pb.GlobalTransform;

            // ══════════════════════════════════════════════════════════════
            // LINEAR CONTROL - PD with Feed-Forward (Moving Target)
            // ══════════════════════════════════════════════════════════════
            // Makes position error decay exponentially: de/dt = -k_p × e
            //
            //   e = x_target - x_current          (position error)
            //   de/dt = v_target - v_current      (derivative of error)
            //
            // de/dt = -k_p × e, solve for v_current:
            //   v_target - v_current = -k_p × e
            //   v_current = v_target + k_p × e    (desired velocity)
            //
            // Discrete implementation:
            //   k_p = Stiffness/Dampening, k_d = Dampening
            //   v_cmd = v_target + k_p × e
            //   Δv = k_d × (v_cmd - v_current) × Δt
            //   v_new = v_current + Δv
            // ══════════════════════════════════════════════════════════════
            // 1. Estimate target velocity (feed-forward)
            Vector3 targetVelocity = Vector3.Zero;
            if (_previousTargets.TryGetValue(pb, out Transform3D prevTarget))
            {
                // v_target = Δx / Δt = (x_current - x_previous) / dt
                targetVelocity = (correctedTarget.Origin - prevTarget.Origin) / (float)delta;
            }
            _previousTargets[pb] = correctedTarget;

            // 2. Position error: e = x_target - x_current
            Vector3 positionError = correctedTarget.Origin - currentPose.Origin;
            float dSq = positionError.LengthSquared();

            if (dSq > MaxPositionError * MaxPositionError)  // Teleport if too far (prevents explosive forces)
            {
                pb.GlobalPosition = correctedTarget.Origin;
                pb.LinearVelocity = Vector3.Zero;
            }
            else if (dSq > 0.0001f)
            {
                // 3. Commanded velocity: v_cmd = v_target + k_p × e
                float k_p = LinearStiffness / LinearDampening;
                Vector3 velocityCmd = targetVelocity + k_p * positionError;
                // 4. Velocity correction with damping
                Vector3 velocityError = velocityCmd - pb.LinearVelocity;  // v_error = v_cmd - v_current
                Vector3 velocityChange = LinearDampening * velocityError * (float)delta;  // Δv = k_d × v_error × Δt

                float maxVelChange = MaxLinearForce * (float)delta;
                if (velocityChange.LengthSquared() > maxVelChange * maxVelChange)
                {
                    GD.Print("[BoneHandler.cs/DriveBones] Max Linear Force exceeded. Normalizing to max force.");
                    velocityChange = velocityChange.Normalized() * maxVelChange;
                }

                pb.LinearVelocity += velocityChange;
            }

            // ══════════════════════════════════════════════════════════════
            // ANGULAR CONTROL - PD with Feed-Forward (Moving Target)
            // ══════════════════════════════════════════════════════════════
            // Makes rotation error decay exponentially
            //
            // Math (analogous to linear):
            //   e_rot = rotation from current to target    (angular error)
            //   ω_target = angular velocity of target      (feed-forward)
            //   ω_cmd = ω_target + k_p × e_rot            (desired angular velocity)
            //   Δω = k_d × (ω_cmd - ω_current) × Δt      (angular velocity change)
            //
            // Where:
            //   k_p = AngularStiffness / AngularDampening
            //   k_d = AngularDampening
            // ══════════════════════════════════════════════════════════════
            // 1. Estimate target angular velocity (feed-forward term)
            //    ω_target ≈ ΔR / Δt (change in rotation over time)
            Vector3 targetAngularVelocity = Vector3.Zero;
            if (_previousTargets.TryGetValue(pb, out Transform3D prevRotTarget))
            {
                Quaternion prevRot = prevRotTarget.Basis.GetRotationQuaternion().Normalized();
                Quaternion currentTargetRot = correctedTarget.Basis.GetRotationQuaternion().Normalized();

                Quaternion rotDelta = currentTargetRot * prevRot.Inverse();
                if (rotDelta.W < 0f) rotDelta = -rotDelta;

                float angDelta = rotDelta.GetAngle();
                if (angDelta > 0.001f && angDelta < Mathf.Pi)
                {
                    Vector3 axisDelta = rotDelta.GetAxis();
                    if (axisDelta.IsFinite() && axisDelta.LengthSquared() > 0.0001f)
                    {
                        axisDelta = axisDelta.Normalized();
                        if (angDelta > Mathf.Pi) angDelta -= Mathf.Tau;  // Normalize angle to [-π, π]

                        targetAngularVelocity = axisDelta * angDelta / (float)delta;  // ω = θ / Δt (angular displacement over time)
                    }
                }
            }

            // 2. Rotation error: Convert rotation difference to axis-angle representation
            Basis currentBasis = currentPose.Basis.Orthonormalized();
            Basis targetBasis = correctedTarget.Basis.Orthonormalized();

            Quaternion currentRot = currentBasis.GetRotationQuaternion().Normalized();
            Quaternion targetRot = targetBasis.GetRotationQuaternion().Normalized();

            if (!currentRot.IsFinite() || !targetRot.IsFinite())
            {
                GD.PrintErr($"Invalid rotation quaternion for bone {pb.Name}");
                continue;
            }

            Quaternion errRot = targetRot * currentRot.Inverse();
            if (errRot.W < 0f) errRot = -errRot;

            float angle = errRot.GetAngle();

            if (angle > 0.001f && angle < Mathf.Pi)
            {
                Vector3 axis = errRot.GetAxis();

                if (!axis.IsFinite() || axis.LengthSquared() < 0.0001f)
                {
                    continue;
                }
                axis = axis.Normalized();
                if (angle > Mathf.Pi) angle -= Mathf.Tau;  // Normalize angle to [-π, π]

                Vector3 angularErr = axis * angle;  // Angular error vector: e_rot = axis × angle

                float k_p_angular = AngularStiffness / AngularDampening;
                Vector3 angularVelocityCmd = targetAngularVelocity + k_p_angular * angularErr;  // ω_cmd = ω_target + k_p × e_rot

                // 3. Angular velocity correction with damping: Δω = k_d × (ω_cmd - ω_current) × Δt
                Vector3 angularVelocityError = angularVelocityCmd - pb.AngularVelocity;
                Vector3 angularVelocityChange = AngularDampening * angularVelocityError * (float)delta;

                float maxAngVelChange = MaxTorque * (float)delta;
                if (angularVelocityChange.LengthSquared() > maxAngVelChange * maxAngVelChange)
                {
                    angularVelocityChange = angularVelocityChange.Normalized() * maxAngVelChange;
                }

                pb.AngularVelocity += angularVelocityChange;
            }
        }
    }
}

