using Godot;


public partial class HeldItemHandler : Node3D
{
    [ExportGroup("References")]
    [Export] public PhysicalBone3D RightHandBone;
    [Export] public PhysicalBone3D LeftHandBone;
    [Export] public Marker3D RightHandIKTarget;
    [Export] public Marker3D LeftHandIKTarget;
    [Export] public PackedScene DefaultWeaponScene; // equip on start

    private Node _worldScene;
    private RigidBody3D _held;
    private Marker3D _rHandGrip;
    private Generic6DofJoint3D _joint;
    private Marker3D _lHandGrip;
    private bool _isTwoHanded;


    public override void _Ready()
    {
        _worldScene = GetTree().CurrentScene;
        // if (DefaultWeaponScene != null) CallDeferred(nameof(_EquipDefault));
    }

    private void _EquipDefault() => Equip(DefaultWeaponScene);

    public void Equip(PackedScene heldItemScene)
    {
        // drop item if holding one
        if (_held != null) Toss(Vector3.Zero, Vector3.Zero);

        // Spawn the held item RigidBody3D
        var item = heldItemScene.Instantiate();
        if (item is not RigidBody3D heldItem)
        {
            GD.PrintErr($"HeldItemHandler: Weapon scene must have RigidBody3D root! Got {item.GetType()}");
            item.QueueFree();
            return;
        }

        _held = heldItem;
        _worldScene.CallDeferred(Node.MethodName.AddChild, _held);

        CallDeferred(nameof(_FinalizeEquip));

    }

    private void _FinalizeEquip()
    {
        // find hand grip for placing item in
        _rHandGrip = _held.GetNodeOrNull<Marker3D>("MarkerHand_R");
        if (_rHandGrip == null)
        {
            GD.PrintErr("HeldItemHandler: Weapon must have MarkerHand_R Marker3D node!");
            _held.QueueFree();
            _held = null;
            return;
        }

        // Calculate weapon position so that right hand grip marker ends up at hand position
        Transform3D gripLocalToWeapon = _held.GlobalTransform.AffineInverse() * _rHandGrip.GlobalTransform;
        _held.GlobalTransform = RightHandBone.GlobalTransform * gripLocalToWeapon.AffineInverse();

        // set the item's velocities to match the hand before creating joint
        _held.LinearVelocity = RightHandBone.LinearVelocity;
        _held.AngularVelocity = RightHandBone.AngularVelocity;

        // create joint
        _joint = new Generic6DofJoint3D();
        _joint.GlobalTransform = RightHandBone.GlobalTransform;
        _worldScene.AddChild(_joint);

        _joint.NodeA = RightHandBone.GetPath();
        _joint.NodeB = _held.GetPath();

        _LockAllDofs(_joint);

        _lHandGrip = _held.GetNodeOrNull<Marker3D>("MarkerHand_L");
        if (_lHandGrip != null)
        {
            LeftHandIKTarget.GlobalTransform = _lHandGrip.GlobalTransform;
            _isTwoHanded = true;
        }
    }

    private void _LockAllDofs(Generic6DofJoint3D j)
    {
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
        _isTwoHanded = false;

        _held.LinearVelocity = RightHandBone.LinearVelocity;
        _held.AngularVelocity = RightHandBone.AngularVelocity;

        _held.ApplyCentralImpulse(tossImpulse);
        _held.ApplyTorqueImpulse(tossTorqueImpulse);

        _held = null;
    }

    public override void _PhysicsProcess(double dt)
    {
        if (_held == null || !_isTwoHanded || _lHandGrip == null) return;
        if (!LeftHandIKTarget.IsInsideTree() || !_lHandGrip.IsInsideTree()) return;

        LeftHandIKTarget.GlobalTransform = _lHandGrip.GlobalTransform;
    }
}
