using Godot;
using Godot.Collections;



[Tool]
public partial class BoneHandler : Node3D
{
    [ExportGroup("Skeletons")]
    [Export] public Skeleton3D IKTargetSkeleton;
    [Export] public Skeleton3D PhysicsSkeleton;

    [ExportToolButton("Fix PhysicsSkeleton Transforms", Icon = "Skeleton3D")] public Callable FixSkeletonButton => Callable.From(FixPhysicsSkeleton);
    [ExportToolButton("Fix IK Target Transforms", Icon = "Marker3D")] public Callable FixIKButton => Callable.From(ResetIKTargets);

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
    private Godot.Collections.Dictionary<int, Transform3D> _cachedModifiedPoses = new();
    private Godot.Collections.Dictionary<int, Marker3D> _ikTargets = new();  // bone ID : Marker3D target


    // Sets physical bones and their collision shapes transformations to match the IK skeleton
    public void FixPhysicsSkeleton()
    {
        Dictionary<int, float> collShapeDict = new();
        GenCollShapes(collShapeDict); 
        
        GD.Print("[BoneHandler/FixPhysicsSkeleton] Starting...");

        PhysicsSkeleton.GlobalTransform = IKTargetSkeleton.GlobalTransform;
        Transform3D playerTransform = _player.GlobalTransform;
        PhysicsSkeleton.GlobalTransform = playerTransform * IKTargetSkeleton.Transform; //Make the skeleton face the same way as the player!

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

                PhysicsSkeleton.SetBoneRest(boneID, IKTargetSkeleton.GetBonePose(boneID));

                Transform3D targetIKPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(boneID);
                Transform3D targetInPhys = PhysicsSkeleton.GlobalTransform.AffineInverse() * targetIKPose;
                
                PhysicsSkeleton.SetBoneGlobalPose(boneID, targetInPhys);
                pb.GlobalTransform = targetIKPose;

                CollisionShape3D collShape = pb.GetChildOrNull<CollisionShape3D>(0);
                if (collShape is null)
                {
                    GD.Print($"[BoneHandler/FixPhysicsSkeleton] {pb.Name} has no CollisionShape3D resource. Skipping...");
                    continue;
                }

                collShape.Scale = new Godot.Vector3(1.0f, 1.0f, 1.0f);

                if (collShape.Shape == null)
                {
                    GD.Print($"[BoneHandler/FixPhysicsSkeleton] {pb.Name} CollisionShape3D has no Shape resource. Skipping...");
                    continue;
                }

                switch (collShape.Shape)
                {
                    case CapsuleShape3D cap:
                        if (collShapeDict.ContainsKey(boneID))
                        {
                            cap.Height = collShapeDict[boneID];
                        }
                        collShape.Position = new Vector3(0, cap.Height * 0.5f, 0);
                        break;

                    case CylinderShape3D cyl:
                        if (collShapeDict.ContainsKey(boneID))
                        {
                            cyl.Height = collShapeDict[boneID];
                        }
                        collShape.Position = new Vector3(0, cyl.Height * 0.5f, 0);
                        break;

                    case BoxShape3D box:
                        if (collShapeDict.ContainsKey(boneID))
                        {
                            box.Size = new Vector3(collShapeDict[boneID], collShapeDict[boneID], collShapeDict[boneID]); //Note, this only makes a cube at the moment. May need work. 
                        }
                        collShape.Position = new Vector3(0, box.Size.Y * 0.5f, 0);
                        break;
                    default:
                        GD.Print($"[BoneHandler] {pb.Name} uses {collShape.Shape.GetClass()} (no height property). Skipping...");
                        break;
                }
            }
        }
        GD.PrintRich("[color=green][BoneHandler/FixPhysicsSkeleton] Ran successfully.[/color]");
    }

    private void GenCollShapes(Dictionary<int, float> CollShapeLengths, int? ParentID = null, int selfID = 0, int recursiontrackdebug = 0)
    {
        int[] boneChildren = PhysicsSkeleton.GetBoneChildren(selfID);
        foreach (int childID in boneChildren)
        {
            string indent = "";

            for (int i = 0; i < recursiontrackdebug; i++)
            {
                indent = indent + " ";
            }
            GD.Print($"{indent}{childID}");

            if (ParentID != null)
            {
                ParentID = (int)ParentID;
                CollShapeLengths[selfID] = PhysicsSkeleton.GetBonePosePosition(selfID+1).Y;
                
                //GD.Print($"PhysicsSkeleton.GetBonePosePosition(selfID).Y: {PhysicsSkeleton.GetBonePosePosition(selfID).Y}");
            }

            if (!PhysicsSkeleton.GetBoneChildren(childID).IsEmpty())
            {
                GenCollShapes(CollShapeLengths, selfID, childID, recursiontrackdebug + 1); // selfid passed to parentID, childId passed to selfId
            }
        }
    }
    

    // Sets the transforms of IK targets (hands and feet) to match IK target skeleton
    public void ResetIKTargets()
    {
        Godot.Collections.Array<string> ikTargets = new Godot.Collections.Array<string> { "Hand_R", "Hand_L", "Foot_R", "Foot_L" };

        foreach (string target in ikTargets)
        {
            var targetIK = IKTargetSkeleton.FindChild($"IK {target}", false, false);
            var ik3DSkeleton = IKTargetSkeleton.FindChild($"{target} SkeletonIK3D", false, false);
            if (targetIK is Marker3D ik && ik3DSkeleton is SkeletonIK3D ik3D)
            {
                string targetName = ik3D.TipBone;
                int targetID = IKTargetSkeleton.FindBone(targetName);
                if (targetID > 0)
                {
                    Transform3D targetPose = IKTargetSkeleton.GlobalTransform * IKTargetSkeleton.GetBoneGlobalPose(targetID);
                    ik.GlobalTransform = targetPose;
                }
                else GD.PrintErr($"[BoneHandler/ResetIKTargets] Invalid SkeletonIK3D TargetNode {targetName} ID: {targetID}");
            }
            else GD.PrintErr($"[BoneHandler/ResetIKTargets] Missing 'IK {target}' and/or '{target} SkeletonIK3D' in IKTargetSkeleton");
        }
    }

    public override void _Ready()
    {
        PhysicsSkeleton.GlobalTransform = IKTargetSkeleton.GlobalTransform;

        _bones.Clear();
        _boneCorrections.Clear();

        IKTargetSkeleton.SkeletonUpdated += OnSkeletonUpdated;
        _player = GetParentOrNull<CharacterBody3D>();

        OnSkeletonUpdated(); // Run once to init bone cache

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

    public Transform3D GetBoneCachedPose(int boneID)
    {
        if (_cachedModifiedPoses.TryGetValue(boneID, out Transform3D pose)) return pose;
        else
        {
            GD.PrintErr($"[BoneHandler.cs] Failed to get cached bone pose for boneID: {boneID} ({PhysicsSkeleton.GetBoneName(boneID)}). Returning default transform...");
            return new Transform3D();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        DriveBones(delta);
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
    public void DriveBones(double delta)
    {

        foreach (var pb in _bones)
        {
            int pbID = pb.GetBoneId();
            float mass = pb.Mass;
            if (mass <= 0) mass = 1f;

            if (!_cachedModifiedPoses.TryGetValue(pbID, out Transform3D modP)) return;

            // Get target pose
            Transform3D targetPose = IKTargetSkeleton.GlobalTransform * modP;
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

                pb.LinearVelocity += force * (float)delta;
            }

            // === ANGULAR (Rotation) Control ===
            // Quaternion currentQuat = new Quaternion(currentPose.Basis.Orthonormalized());
            // Quaternion targetQuat = new Quaternion(correctedTarget.Basis.Orthonormalized());

            // // Normalize quaternions
            // currentQuat = currentQuat.Normalized();
            // targetQuat = targetQuat.Normalized();

            // === ANGULAR (Rotation) Control ===
            Basis rotationDifference = correctedTarget.Basis * currentPose.Basis.Inverse();

            // Convert to axis-angle (Euler can have gimbal lock issues, but tutorial uses it)
            Vector3 angularDisplacement = rotationDifference.GetEuler();

            // Normalize angles to [-PI, PI] range
            angularDisplacement.X = NormalizeAngle(angularDisplacement.X);
            angularDisplacement.Y = NormalizeAngle(angularDisplacement.Y);
            angularDisplacement.Z = NormalizeAngle(angularDisplacement.Z);

            // Hooke's Law for rotation: T = kθ - cω
            Vector3 torque = (AngularStiffness * angularDisplacement) - (AngularDampening * pb.AngularVelocity);

            // Clamp
            if (torque.Length() > MaxTorque)
            {
                torque = torque.Normalized() * MaxTorque;
            }

            pb.AngularVelocity += torque * (float)delta;
        }
    }



    private float NormalizeAngle(float angle)
    {
        while (angle > Mathf.Pi) angle -= Mathf.Tau;
        while (angle < -Mathf.Pi) angle += Mathf.Tau;
        return angle;
    }
}

