using Godot;

public partial class HeldItemHandler : Node3D
{
    [ExportGroup("References")]
    [Export] public PhysicalBone3D RightHandBone;
    [Export] public PhysicalBone3D LeftHandBone;
    [Export] public Marker3D RightHandIKTarget;
    [Export] public Marker3D LeftHandIKTarget;
    [Export] public PackedScene DefaultWeaponScene;  // Optional default item to auto-equip at start

    [ExportSubgroup("Hand Positions")]
    [Export] public Marker3D OneHandedReadyMarker;
    [Export] public Marker3D TwoHandedReadyMarker;
    [Export] public Marker3D OneHandedRestMarker;
    [Export] public Marker3D TwoHandedRestMarker;

    [ExportGroup("Hand Movement Values")]
    [Export] public float HandSensitivity = 0.75f;                                 // Global sensitivity scaler for cursor to hand mapping
    [Export] public Vector2 OneHandedAttackPlaneOffset = new Vector2(0.5f, 0.5f);  // Base offset for the attack plane, measured in meters along camera-right (X) and camera-down (Y)
    [Export] public Vector2 TwoHandedAttackPlaneOffset = new Vector2(0.5f, 0.5f);
    [Export] public Vector2 OneHandedAttackPlaneSize = new Vector2(1.0f, 1.0f);    // max meters (X, Y) from center attack position
    [Export] public Vector2 TwoHandedAttackPlaneSize = new Vector2(1.0f, 1.0f);
    [Export] public float OneHandedSlashAngle = 15f;                               // Slash roll angle (degrees) applied around blade/forward direction during movement
    [Export] public float TwoHandedSlashAngle = 15f;

    [Export] public float OneHandedOutwardSpeedFactor = 0.1f;  // How much swing speed pushes the blade “outward” (tilt component) for visual slash feel
    [Export] public float TwoHandedOutwardSpeedFactor = 0.1f;
    [Export] public float OneHandedSwingOutwardAmount = 0.1f;  // Max outward contribution from speed tilt. Acts like a cap/scale on the outward blend
    [Export] public float TwoHandedSwingOutwardAmount = 0.1f;


    private Node _worldScene;
    private HeldItem3D _held;               // currently held item instance (custom RigidBody3D)
    private HeldItem3D.ItemKind _itemKind;  // cached type of held item
    private Marker3D _rHandGrip;            // item-defined right-hand grip marker (world transform source)
    private Generic6DofJoint3D _joint;      // joint linking hand bone to held item
    private Marker3D _lHandGrip;            // item-defined left-hand grip (for two-handed items)
    private bool _isTwoHanded;
    private Viewport _viewport;
    private Rect2 _visibleRect;
    private Vector2 _screenCenter;
    private Camera3D _camera;
    private Vector3 _prevHandWorld;

    public override void _Ready()
    {
        _worldScene = GetTree().CurrentScene;

        // Cache viewport size/center/camera. Recalculate on resize.
        CalcScreenData();
        GetViewport().SizeChanged += CalcScreenData;

        // Optionally equip default item after we’re in-tree.
        if (DefaultWeaponScene != null) CallDeferred(nameof(_EquipDefault));
    }

    private void CalcScreenData()
    {
        // Capture viewport geometry and main camera for ray queries.
        _viewport = GetViewport();
        _visibleRect = _viewport.GetVisibleRect();
        _screenCenter = _visibleRect.Position + _visibleRect.Size * 0.5f;
        _camera = _viewport.GetCamera3D();
    }

    private void _EquipDefault() => Equip(DefaultWeaponScene);

    public void Equip(PackedScene heldItemScene)
    {
        // If already holding something, toss it before equipping the new item.
        if (_held != null) Toss(Vector3.Zero, Vector3.Zero);

        // Instantiate and validate the item root type (must be HeldItem3D).
        var item = heldItemScene.Instantiate();
        if (item is not HeldItem3D heldItem)
        {
            GD.PrintErr($"HeldItemHandler: Weapon scene must have HeldItem3D root! Got {item.GetType()}");
            item.QueueFree();
            return;
        }

        _held = heldItem;

        // Add to world so its _Ready() resolves internal refs (grips etc.).
        _worldScene.AddChild(_held);

        // Finalize setup on the next idle/deferred tick to ensure item is ready.
        CallDeferred(nameof(_FinalizeEquip));
    }

    private void _FinalizeEquip()
    {
        // Cache item-defined grip markers and type flags.
        _rHandGrip = _held.RightGrip;
        if (_rHandGrip == null) GD.PrintErr($"Marker3D RightGrip in HeldItem3D {_held.Name} missing!");
        _lHandGrip = _held.LeftGrip;
        _itemKind = _held.Kind;
        _isTwoHanded = _held.IsTwoHanded;

        // Position the item so its right-hand grip aligns with the current right hand bone.
        // (world_hand) = (item_world) * (grip_local)  ⇒ item_world = hand_world * inverse(grip_local)
        Transform3D gripLocal = _rHandGrip.Transform;
        _held.GlobalTransform = RightHandBone.GlobalTransform * gripLocal.AffineInverse();

        // Match initial linear/angular velocity to the hand.
        _held.LinearVelocity = RightHandBone.LinearVelocity;
        _held.AngularVelocity = RightHandBone.AngularVelocity;

        // Create and place a 6DoF joint at the hand bone; connect hand (A) ↔ item (B).
        _joint = new Generic6DofJoint3D();
        _joint.GlobalTransform = RightHandBone.GlobalTransform;
        _worldScene.AddChild(_joint);

        _joint.NodeA = RightHandBone.GetPath();
        _joint.NodeB = _held.GetPath();
        _LockAllDofs(_joint);

        // For two-handed items, snap the left-hand IK target to the item’s left grip transform.
        if (_isTwoHanded)
        {
            if (_lHandGrip == null) GD.PrintErr($"Marker3D LeftGrip in HeldItem3D {_held.Name} missing!");
            LeftHandIKTarget.GlobalTransform = _lHandGrip.GlobalTransform;
        }
    }

    private void _LockAllDofs(Generic6DofJoint3D j)
    {
        // Fully lock translation and rotation on all three axes.
        // X axis
        j.SetParamX(Generic6DofJoint3D.Param.LinearLowerLimit, 0f);
        j.SetParamX(Generic6DofJoint3D.Param.LinearUpperLimit, 0f);
        j.SetParamX(Generic6DofJoint3D.Param.AngularLowerLimit, 0f);
        j.SetParamX(Generic6DofJoint3D.Param.AngularUpperLimit, 0f);
        // Y axis
        j.SetParamY(Generic6DofJoint3D.Param.LinearLowerLimit, 0f);
        j.SetParamY(Generic6DofJoint3D.Param.LinearUpperLimit, 0f);
        j.SetParamY(Generic6DofJoint3D.Param.AngularLowerLimit, 0f);
        j.SetParamY(Generic6DofJoint3D.Param.AngularUpperLimit, 0f);
        // Z axis
        j.SetParamZ(Generic6DofJoint3D.Param.LinearLowerLimit, 0f);
        j.SetParamZ(Generic6DofJoint3D.Param.AngularLowerLimit, 0f);
        j.SetParamZ(Generic6DofJoint3D.Param.LinearUpperLimit, 0f);
        j.SetParamZ(Generic6DofJoint3D.Param.AngularUpperLimit, 0f);
    }

    public void Toss(Vector3 tossImpulse, Vector3 tossTorqueImpulse)
    {
        // Remove joint first to detach physics link.
        _joint.Free();
        _joint = null;

        // Give the item the hand’s current velocities prior to impulses for continuity.
        _held.LinearVelocity = RightHandBone.LinearVelocity;
        _held.AngularVelocity = RightHandBone.AngularVelocity;

        // Impulse toss (linear + torque), then clear held state.
        _held.ApplyCentralImpulse(tossImpulse);
        _held.ApplyTorqueImpulse(tossTorqueImpulse);

        _isTwoHanded = false;
        _held.QueueFree();
        _held = null;
    }

    public override void _PhysicsProcess(double dt)
    {
        // Maintain left-hand IK on the item’s left grip for two-handed items.
        if (_held != null && _isTwoHanded && _lHandGrip != null)
        {
            if (LeftHandIKTarget.IsInsideTree() && _lHandGrip.IsInsideTree())
                LeftHandIKTarget.GlobalTransform = _lHandGrip.GlobalTransform;
        }

        if (Input.IsActionJustPressed("use"))
        {
            // Enter confined mouse mode and recenter cursor.
            Input.MouseMode = Input.MouseModeEnum.Confined;
            Input.WarpMouse(_screenCenter);

            // TODO: start soft-locking look direction to nearest target(s).

            // Snap right-hand IK to appropriate ready pose on press.
            // Unarmed
            if (_held == null)
            {
                RightHandIKTarget.GlobalTransform = OneHandedReadyMarker.GlobalTransform;
            }
            // 1 or 2 handed weapon
            else if (_itemKind == HeldItem3D.ItemKind.Weapon)
            {
                if (!_isTwoHanded) RightHandIKTarget.GlobalTransform = OneHandedReadyMarker.GlobalTransform;
                else RightHandIKTarget.GlobalTransform = TwoHandedReadyMarker.GlobalTransform;
            }
        }
        else if (Input.IsActionJustReleased("use"))
        {
            // Return to captured mode; snap back to rest.
            Input.MouseMode = Input.MouseModeEnum.Captured;

            // Unarmed
            if (_held == null)
            {
                RightHandIKTarget.GlobalTransform = OneHandedRestMarker.GlobalTransform;
            }
            // 1 or 2 handed weapon
            else if (_itemKind == HeldItem3D.ItemKind.Weapon)
            {
                if (!_isTwoHanded) RightHandIKTarget.GlobalTransform = OneHandedRestMarker.GlobalTransform;
                else RightHandIKTarget.GlobalTransform = TwoHandedRestMarker.GlobalTransform;
            }
        }
        else if (Input.IsActionPressed("use"))
        {
            // While holding the button, continuously update hand position/orientation.
            UseHand(dt);
        }
    }

    private void UseHand(double dt)
    {
        // Build camera ray from current mouse position (viewport space → world).
        Vector2 mouse = _viewport.GetMousePosition();
        Vector3 ro = _camera.ProjectRayOrigin(mouse);
        Vector3 rd = _camera.ProjectRayNormal(mouse);

        // Camera forward (−Z in Godot’s camera convention) defines the plane normal.
        Vector3 fwd = -_camera.GlobalTransform.Basis.Z;

        // Choose plane center/limits based on item kind and handedness.
        Vector3 center;
        Vector2 planeSize;
        Vector2 planeOffset;

        if (_itemKind == HeldItem3D.ItemKind.Weapon)
        {
            // Attack plane origin: ready marker (1H or 2H). Size/offset from inspector.
            center = _isTwoHanded ? TwoHandedReadyMarker.GlobalTransform.Origin : OneHandedReadyMarker.GlobalTransform.Origin;
            planeSize = _isTwoHanded ? TwoHandedAttackPlaneSize : OneHandedAttackPlaneSize;
            planeOffset = _isTwoHanded ? TwoHandedAttackPlaneOffset : OneHandedAttackPlaneOffset;
        }
        else
        {
            // TODO: unarmed “smack” uses one-handed values for now; consider a separate set.
            center = OneHandedReadyMarker.GlobalTransform.Origin;
            planeSize = OneHandedAttackPlaneSize;
            planeOffset = OneHandedAttackPlaneOffset;
        }

        // Intersect camera ray with the (infinite) hand plane through 'center'.
        Vector3? hit = new Plane(fwd, center).IntersectsRay(ro, rd);
        if (hit.HasValue)
        {
            // Camera-aligned basis vectors for plane-space projection:
            Basis b = _camera.GlobalTransform.Basis;
            Vector3 right = b.X;
            Vector3 down = -b.Y;

            // Pre-apply a base offset in plane space so the allowed region is shifted.
            Vector3 offsetCenter = center + (right * planeOffset.X) + (down * planeOffset.Y);

            // Vector from plane center to the raw hit point.
            Vector3 v = hit.Value - center;

            // Project raw hit into plane coordinates (meters), scaled by sensitivity.
            float x = v.Dot(right) * HandSensitivity;
            float y = v.Dot(down) * HandSensitivity;

            // Clamp within rectangular bounds around the offset center.
            x = Mathf.Clamp(x + planeOffset.X, -planeSize.X / 2, planeSize.X / 2);
            y = Mathf.Clamp(y + planeOffset.Y, -planeSize.Y / 2, planeSize.Y / 2);

            // Rebuild the clamped world position on the plane.
            Vector3 targetPos = offsetCenter + (right * x) + (down * y);

            // === Wrist rotation during movement (slash feel) ===
            // Derive swing direction from IK target motion and build a basis that tilts outward with speed.
            Vector3 curr = RightHandIKTarget.GlobalTransform.Origin;
            Vector3 velocity = (curr - _prevHandWorld) / (float)dt;
            _prevHandWorld = curr;

            if (velocity.Length() > 0.001f)
            {
                Vector3 swingDir = velocity.Normalized();

                // "Outward" is perpendicular to swing in the camera’s (right, up) plane.
                Vector3 up = _camera.GlobalTransform.Basis.Y;
                Vector3 outward = swingDir.Cross(fwd).Normalized();

                // Parameterize rotation based on item mode.
                float aoaDeg;             // “angle of attack” roll (deg)
                float speedFactor;        // scales how much velocity contributes
                float SwingOutwardAmount; // cap/scale for outward blend
                if (_itemKind == HeldItem3D.ItemKind.Weapon)
                {
                    aoaDeg = _isTwoHanded ? TwoHandedSlashAngle : OneHandedSlashAngle;
                    speedFactor = _isTwoHanded ? TwoHandedOutwardSpeedFactor : OneHandedOutwardSpeedFactor;
                    SwingOutwardAmount = _isTwoHanded ? TwoHandedSwingOutwardAmount : OneHandedSwingOutwardAmount;
                }
                else
                {
                    // TODO: add wrist rotation style for unarmed “smack”.
                    aoaDeg = OneHandedSlashAngle;
                    speedFactor = 0f;
                    SwingOutwardAmount = 0f;
                }

                // Blend swing direction with outward tilt based on speed.
                float speedFactorVel = Mathf.Clamp(velocity.Length() * speedFactor, 0f, 1f);
                Vector3 bladeForward = (swingDir + outward * speedFactorVel * SwingOutwardAmount).Normalized();

                // Build an orthonormal frame: newRight = forward × up; newUp = right × forward.
                Vector3 newRight = bladeForward.Cross(up).Normalized();
                Vector3 newUp = newRight.Cross(bladeForward).Normalized();

                // Roll around the right axis by aoaDeg to expose the edge.
                Quaternion aoaRotation = new Quaternion(newRight, Mathf.DegToRad(aoaDeg));

                // Compose final orientation: basis with forward pointing along −Z.
                Basis targetBasis = new Basis(newRight, newUp, -bladeForward);
                targetBasis = new Basis(aoaRotation) * targetBasis;

                // Smoothly slerp current wrist orientation toward target basis.
                var curQ = new Quaternion(RightHandIKTarget.GlobalTransform.Basis);
                var dstQ = new Quaternion(targetBasis);
                float alpha = 1f - Mathf.Exp(-15f * (float)dt);
                Quaternion newQ = curQ.Slerp(dstQ, alpha);

                // Write position + orientation.
                RightHandIKTarget.GlobalTransform = new Transform3D(new Basis(newQ), targetPos);
            }
        }
    }
}
