using Godot;
public partial class Player : CharacterBody3D
{
    public bool CurrFootIsRight = true;
    public bool IsStepping = false;
    public bool IsStepDown = false;

    [Export] public float FootLerpSpeed = 10;

    [Export] public float MaxDistanceBeforeStep = 1.5f;
    [Export] public Skeleton3D IKSkeleton;
    [Export] public Marker3D LFootIKTarget;
    private Vector3 _lFootIKDefaultPosition;
    [Export] public Marker3D RFootIKTarget;
    private Vector3 _rFootIKDefaultPosition;
    [Export] public Marker3D ChestTarget;

    [Export] public float MaxBodyTilt = .25f; //Represents how far the HeadTarget can move to in meters
    public Vector3 StepDownTarget;

    [Export] public StepHandler StepHandler;
    [Export] public BoneHandler BoneHandler;
    [Export] public HeldItemHandler HeldItemHandler;


    [Export] public float Speed { get; set; } = 10f;
    [Export] public float FallAcceleration { get; set; } = 9.8f;
    [Export] public float AirManueverSpeed { get; set; } = 5f;

    int rFootBoneId;
    int lFootBoneId;
    int hipsBoneId;

    [Export] public RigidBody3D HeldItem;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector2 _mouse;

    public AnimationPlayer AnimPlayer;
    private BoneAttachment3D _rHandBoneAttachement;


    public override void _Ready()
    {
        Vector2 Resolution = GetViewport().GetVisibleRect().Size; //This needs to be elsewhere eventually. This will change if viewport size changes during gameplay\
        //Input.MouseMode = Input.MouseModeEnum.Confined;
        AnimPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        rFootBoneId = IKSkeleton.FindBone("Foot.R");
        lFootBoneId = IKSkeleton.FindBone("Foot.L");
        hipsBoneId = IKSkeleton.FindBone("Hips");

        _lFootIKDefaultPosition = LFootIKTarget.Position;
        _rFootIKDefaultPosition = RFootIKTarget.Position;
    }

    public override void _Input(InputEvent @e)
    {
        if (@e.IsActionPressed("pause")) GetTree().Quit();

    }

    public override void _PhysicsProcess(double delta)
    {
        // HandleSteps();
        // FootMove(delta);

        float yVelRotation = Mathf.Atan2(-Velocity.X, -Velocity.Z);
        if (Velocity != Vector3.Zero)
        {
            StepHandler.LeftRay.Rotation = new Vector3(Mathf.DegToRad(35), yVelRotation, 0);
            StepHandler.RightRay.Rotation = new Vector3(Mathf.DegToRad(35), yVelRotation, 0);
        }
        else
        {
            StepHandler.LeftRay.Rotation = Vector3.Zero;
            StepHandler.RightRay.Rotation = Vector3.Zero;
        }

        Vector3 chestTargetNewPosition = new Vector3(-Velocity.Normalized().X * MaxBodyTilt, ChestTarget.Position.Y, -Velocity.Normalized().Z * MaxBodyTilt); //Shifts the chest target in accordance w/ velocity. 0 vel is default position
        ChestTarget.Position = ChestTarget.Position.Lerp(chestTargetNewPosition, 10 * (float)delta);

    }

    //     private void HandleSteps()
    //     {
    //         if (IsStepping) return;

    //         Vector3 rFootPos = IKSkeleton.GlobalTransform * BoneHandler.GetBoneCachedPose(rFootBoneId).Origin;
    //         Vector3 lFootPos = IKSkeleton.GlobalTransform * BoneHandler.GetBoneCachedPose(lFootBoneId).Origin;

    //         Vector3 avgPos = (rFootPos + lFootPos) / 2;

    //         float rFootDistance = rFootPos.DistanceTo(RFootIKTarget.GlobalPosition);
    //         float lFootDistance = lFootPos.DistanceTo(LFootIKTarget.GlobalPosition);

    //         float avgDistanceCenter = new Vector3(avgPos.X, (Velocity.Normalized().Length() * .2f), avgPos.Z).DistanceTo(new Vector3(GlobalPosition.X, (Velocity.Normalized().Length() * .2f), GlobalPosition.Z));

    //         if (IsStepping)
    //         {
    //             // if (rFootDistance > MaxDistanceBeforeStep || lFootDistance > MaxDistanceBeforeStep)
    //             // {
    //             //     Velocity = Vector3.Zero;
    //             // }
    //             return;
    //         }

    //         if (avgDistanceCenter > MaxDistanceBeforeStep /*|| Velocity == Vector3.Zero && (rFootDistance > 0.1f || lFootDistance > 0.1f)*/)
    //         {
    //             IsStepping = true;
    //             CurrFootIsRight = lFootDistance > rFootDistance ? false : true;
    //             StepDownTarget = CurrFootIsRight ? StepHandler.StepDownTargetR : StepHandler.StepDownTargetL;
    //         }

    //         if (Velocity == Vector3.Zero && rFootDistance > MaxDistanceBeforeStep * .2 || lFootDistance > MaxDistanceBeforeStep * .2) //when we have a velocity of 0, we are more strict about maxstepdistance to acheive a more natural looking pose
    //         {
    //             IsStepping = true;
    //             CurrFootIsRight = lFootDistance > rFootDistance ? false : true;
    //             StepDownTarget = CurrFootIsRight ? StepHandler.StepDownTargetR : StepHandler.StepDownTargetL;

    //         }

    //         /* if (rFootDistance > MaxDistanceBeforeStep || lFootDistance > MaxDistanceBeforeStep)
    //         {
    //             IsStepping = true;
    //             CurrFootIsRight = lFootDistance > rFootDistance ? false : true;
    //             StepDownTarget = CurrFootIsRight ? StepHandler.StepDownTargetR : StepHandler.StepDownTargetL;
    //         } */
    //     }

    //     private void FootMove(double delta)
    //     {
    //         if (!IsStepping) return;

    //         Vector3 StepUpPos = CurrFootIsRight ? StepHandler.StepUpPosR.GlobalPosition : StepHandler.StepUpPosL.GlobalPosition;
    //         Marker3D CurrFootIKTarget = CurrFootIsRight ? RFootIKTarget : LFootIKTarget;

    //         if (!IsStepDown)
    //         {
    //             CurrFootIKTarget.GlobalPosition = CurrFootIKTarget.GlobalPosition.Lerp(StepUpPos, FootLerpSpeed * (float)delta);
    //             if (CurrFootIKTarget.GlobalPosition.DistanceSquaredTo(StepUpPos) < 0.01f) IsStepDown = true;
    //         }
    //         else
    //         {
    //             CurrFootIKTarget.GlobalPosition = CurrFootIKTarget.GlobalPosition.Lerp(StepDownTarget, FootLerpSpeed * (float)delta);
    //             if (CurrFootIKTarget.GlobalPosition.DistanceSquaredTo(StepDownTarget) < 0.01f)
    //             {
    //                 IsStepDown = false;
    //                 IsStepping = false;
    //             }
    //         }
    //     }
}
