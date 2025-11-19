// Player.cs
using Godot;

public partial class Player : CharacterBody3D
{
    public bool CurrFootIsRight = true;
    public bool IsStepping = false;

    [ExportGroup("Required Child Node References")]
    [Export] public Skeleton3D IKSkeleton;
    [Export] public Marker3D LFootIKTarget;
    [Export] public Marker3D RFootIKTarget;
    [Export] public Marker3D ChestTarget;
    [Export] public StepHandler StepHandler;
    [Export] public BoneHandler BoneHandler;
    [Export] public HeldItemHandler HeldItemHandler;
    [Export] public PlayerInput PlayerInput;


    [ExportGroup("Procedural Walking Parameters")]
    [Export] public float FootLerpSpeed = 10;
    [Export] public float MaxDistanceBeforeStep = 1.5f;
    [Export] public float MaxBodyTilt = .25f; //Represents how far the HeadTarget can move to in meters
    [Export(PropertyHint.Range, "1.0f, 5.0f")] public float VelocityImpactOnStrideFactor;

    [Export(PropertyHint.Range, "0.0f,1.0f,")] public float StepUpHeight; //Between 0.0 & 1.0m
    [Export(PropertyHint.Range, "1.0f, 20.0f,")] public float StepUpSpeed;

    public Vector3 _stepDownTarget;
    private Vector3 _footStartPos;

    [Export] public float Speed { get; set; } = 10f;
    [Export] public float FallAcceleration { get; set; } = 9.8f;
    [Export] public float AirManueverSpeed { get; set; } = 5f;

    Vector3 _rFootPos;
    Vector3 _lFootPos;

    int rFootBoneId;
    int lFootBoneId;

    private float _stepProgress;
    private bool _isStepUp;

    [Export] public RigidBody3D HeldItem;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector2 _mouse;

    public AnimationPlayer AnimPlayer;
    private BoneAttachment3D _rHandBoneAttachement;

    [ExportGroup("Camera Settings")]
    [Export(PropertyHint.None, "suffix:\u00ba")] float YRotationMinimum = -70;
    [Export(PropertyHint.None, "suffix:\u00ba")] float YRotationMaximum = 70;
    [Export] float lookAroundSpeed = 10f;
    [Export] Camera3D Camera;
    float yawDeg;
    float pitchDeg;


    //Multiplayer -----------------------
    [Export]
    public int PlayerID
    {
        get => _playerId;
        set
        {
            _playerId = value;
            if (GetNodeOrNull<PlayerInput>("PlayerInput") is Node PlayerInputNode)
            {
                PlayerInputNode.SetMultiplayerAuthority(value);
            }

            else
            {
                GD.PrintErr("PlayerInput node not found!");
            }
        }
    }
    private int _playerId = 1;
    // -----------------------------------------------------


    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        Vector2 Resolution = GetViewport().GetVisibleRect().Size; //This needs to be elsewhere eventually. Will need a "GetViewport().SizeChanged +="
        //Input.MouseMode = Input.MouseModeEnum.Confined;
        AnimPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        rFootBoneId = IKSkeleton.FindBone("Foot.R");
        lFootBoneId = IKSkeleton.FindBone("Foot.L");
        yawDeg = -RotationDegrees.Y; //See note in _Input about how Mouse/player rotation axis (x & y) are flipped


        GD.Print($"Player._Ready() - PlayerID: {PlayerID}, MyUniqueID: {Multiplayer.GetUniqueId()}, Match: {PlayerID == Multiplayer.GetUniqueId()}");
        SetupCamera();

        ProcessMode = ProcessModeEnum.Always;  // TEMP: so unpause works after pause
    }

    private void SetupCamera()
    {
        if (PlayerID == Multiplayer.GetUniqueId())
        {
            Camera.Current = true;
            GD.Print($"Camera activated for player {PlayerID}");
        }
    }

    public override void _Input(InputEvent @e)
    {
        if (@e.IsActionPressed("pause"))
        {
            GetTree().Paused = !GetTree().Paused;
            if (Input.MouseMode != Input.MouseModeEnum.Visible) Input.MouseMode = Input.MouseModeEnum.Visible;
            else Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        if (@e is InputEventMouseMotion mouseMotion)
        { //mouseMotion is a local variable here
            yawDeg += mouseMotion.Relative.X * (lookAroundSpeed / 100);
            pitchDeg -= mouseMotion.Relative.Y * (lookAroundSpeed / 100);
            GD.Print(pitchDeg);

            pitchDeg = Mathf.Clamp(pitchDeg, YRotationMinimum, YRotationMaximum);

            Vector3 char_rot = new Godot.Vector3(RotationDegrees.X, -yawDeg, RotationDegrees.Z);
            Vector3 cam_rot = new Godot.Vector3(pitchDeg, Camera.RotationDegrees.Y, Camera.RotationDegrees.Z);

            RotationDegrees = char_rot;
            Camera.RotationDegrees = cam_rot;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _rFootPos = IKSkeleton.GlobalTransform * BoneHandler.GetBoneCachedPose(rFootBoneId).Origin;
        _lFootPos = IKSkeleton.GlobalTransform * BoneHandler.GetBoneCachedPose(lFootBoneId).Origin;

        float yVelRotation = Mathf.Atan2(-Velocity.X, -Velocity.Z);

        if (Velocity != Vector3.Zero)
        {
            StepHandler.LeftRay.GlobalRotation = new Vector3(Mathf.DegToRad(35), yVelRotation, 0);
            StepHandler.RightRay.GlobalRotation = new Vector3(Mathf.DegToRad(35), yVelRotation, 0);
        }
        else
        {
            StepHandler.LeftRay.Rotation = new Vector3(0, 0, Mathf.DegToRad(-5));
            StepHandler.RightRay.Rotation = new Vector3(0, 0, Mathf.DegToRad(5));
        }

        HandleSteps(delta);
        FootMove(delta);

        Vector3 avgLocalFtPos = (BoneHandler.GetBoneCachedPose(rFootBoneId).Origin + BoneHandler.GetBoneCachedPose(lFootBoneId).Origin) / 2; // taking avg local foot pos to shift head in that direction along w/ velocity.
        Vector3 LocalVelocity = GlobalTransform.Basis.Inverse() * Velocity;
        Vector3 chestTargetNewPosition = new Vector3((avgLocalFtPos.X + LocalVelocity.Normalized().X) * MaxBodyTilt, ChestTarget.Position.Y, (avgLocalFtPos.Z + LocalVelocity.Normalized().Z) * MaxBodyTilt); //Shifts the chest target in accordance w/ velocity. 0 vel is default position
        ChestTarget.Position = ChestTarget.Position.Lerp(chestTargetNewPosition, 10 * (float)delta);

    }


    private void HandleSteps(double delta)
    {
        if (IsStepping) return;
        //GD.Print("HandleSteps Entered");

        Vector3 desiredR = StepHandler.StepDownTargetR + (Velocity * VelocityImpactOnStrideFactor * (float)delta);
        Vector3 desiredL = StepHandler.StepDownTargetL + (Velocity * VelocityImpactOnStrideFactor * (float)delta);

        float rErr = _rFootPos.DistanceTo(desiredR);
        float lErr = _lFootPos.DistanceTo(desiredL);


        if (rErr > MaxDistanceBeforeStep || lErr > MaxDistanceBeforeStep)
        {
            IsStepping = true;
            CurrFootIsRight = (rErr >= lErr); // move the foot with the larger error

            _stepDownTarget = CurrFootIsRight ? desiredR
                                                : desiredL;

            _footStartPos = CurrFootIsRight ? RFootIKTarget.GlobalPosition
                                                : LFootIKTarget.GlobalPosition;

            _stepProgress = 0;
            _isStepUp = true;
        }
    }

    private void FootMove(double delta) //PROBLEM HERE IS THAT RAISING FOOT OBVIOUSLY INCREASES DISTANCE & THEREFORE CHANGES HEIGHT WHICH THEREFORE INCREASES DISTANCE AND THEREFORE... ETC ETC
    {
        if (!IsStepping) return;
        //GD.Print("FootMove Entered");

        Marker3D CurrFootIKTarget = CurrFootIsRight ? RFootIKTarget : LFootIKTarget;

        _stepProgress = Mathf.Clamp(_stepProgress + StepUpSpeed * (float)delta, 0, 100);

        float footHeightOffset = StepUpHeight * (1 - _stepProgress);       //Using cosine as, since we are lerping to the height offset, we shoot up from 0 to the .5 max (or a bit after tbh) immediately and then head down. The foot doesn't draw the same arc as the Cosine here.

        CurrFootIKTarget.GlobalPosition = CurrFootIKTarget.GlobalPosition.Lerp(_stepDownTarget + new Vector3(0, footHeightOffset, 0), (FootLerpSpeed + Velocity.Length()) * (float)delta);
        CurrFootIKTarget.Rotation = new Vector3(CurrFootIKTarget.Rotation.X, Rotation.Y, CurrFootIKTarget.Rotation.Z); //Prevents ankles from rolling

        if (CurrFootIKTarget.GlobalPosition.DistanceSquaredTo(_stepDownTarget) < 0.01f)
        {
            IsStepping = false;
            _stepProgress = 0f;
            CurrFootIKTarget.GlobalPosition = _stepDownTarget;
        }
    }
}


/*
    Might be able to break the necessesity for tuning player speed, lerp speed etc? It might be easier to

    Implement delta*vel ofset?
    hip rot?
    different step distance for directions
    accel/deccel
    Velocity only during step other just move hips? Torso? (Maybe)
    Ray cast from hips down?
    Sine wave interp for foot up/down movement?

    I feel like max distance from IK target is an erroneous way to time steps. Ideally, we would want IK target & foot to be synced 24/7 as this means we haven't exceeded the limits of our IK skeleton. need to think about this more.
*/
