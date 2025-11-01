using Godot;
using System;
using Godot.Collections;

public partial class Player : CharacterBody3D
{
    public bool CurrFootIsRight = true;
    public bool IsStepping = false;
    public bool IsStepDown = false;

    [Export] public float FootLerpSpeed = 10;
    
    [Export] public float MaxDistanceBeforeStep = 1.5f;
    [Export] public Skeleton3D IKSkeleton;
    [Export] public Marker3D LFootIKTarget;
    [Export] public Marker3D RFootIKTarget;
    public Vector3 StepDownTarget;

    [Export] public StepHandler StepHandler;

    [Export] public float Speed { get; set; } = 10f;
    [Export] public float FallAcceleration { get; set; } = 9.8f;
    [Export] public float AirManueverSpeed { get; set; } = 5f;

    [Export] public RigidBody3D HeldItem;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector2 _mouse;

    public AnimationPlayer AnimPlayer;
    private BoneAttachment3D _rHandBoneAttachement;
    private RigidBody3D _equipped;


    public override void _Ready()
    {
        Vector2 Resolution = GetViewport().GetVisibleRect().Size; //This needs to be elsewhere eventually. This will change if viewport size changes during gameplay\
        //Input.MouseMode = Input.MouseModeEnum.Confined;
        AnimPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
    }

    public override void _Input(InputEvent @e)
    {
        if (@e.IsActionPressed("pause")) GetTree().Quit(); 

    }

    public override void _PhysicsProcess(double delta)
    {
        HandleSteps();
        FootMove(delta);
        

    }

    public void EquipWeapon(RigidBody3D Equipped)
    {
        if (_equipped != null)
        {
            _equipped = Equipped;

        }

    }

    private void HandleSteps()
    {
        if (IsStepping) return;
        int rFootBoneId = IKSkeleton.FindBone("Foot.R");
        int lFootBoneId = IKSkeleton.FindBone("Foot.L");
        int hipsBoneId = IKSkeleton.FindBone("Hips");

        float rFootDistance = IKSkeleton.GetBoneGlobalPose(rFootBoneId)[0].DistanceTo(IKSkeleton.GetBoneGlobalPose(hipsBoneId)[0]);
        float lFootDistance = IKSkeleton.GetBoneGlobalPose(lFootBoneId)[0].DistanceTo(IKSkeleton.GetBoneGlobalPose(hipsBoneId)[0]);
        if (rFootDistance > MaxDistanceBeforeStep || lFootDistance > MaxDistanceBeforeStep)
        {
            IsStepping = true;
            GD.Print("IsStepping");
        }


        if (lFootDistance > rFootDistance)
        {
            CurrFootIsRight = false;
            StepDownTarget = StepHandler.StepDownTargetL;
        }
        else
        {
            CurrFootIsRight = true;
            StepDownTarget = StepHandler.StepDownTargetR;
        }
    }
    

    private void FootMove(double delta)
    {
        if (!IsStepping) return;

        Vector3 StepUpPos = (CurrFootIsRight) ? StepHandler.StepUpPosR.GlobalPosition : StepHandler.StepUpPosL.GlobalPosition;
        Vector3 StepDownPos = StepDownTarget;
        Marker3D CurrFootIKTarget = (CurrFootIsRight) ? RFootIKTarget : LFootIKTarget;

        if (IsStepDown == false)
        {
            CurrFootIKTarget.GlobalPosition = CurrFootIKTarget.GlobalPosition.Lerp(StepUpPos, FootLerpSpeed * 2 * (float)delta);
            if (CurrFootIKTarget.GlobalPosition.DistanceSquaredTo(StepUpPos) > .1) IsStepDown = true;
        }

        else if (IsStepDown == true)
        {
            {
                CurrFootIKTarget.GlobalPosition = CurrFootIKTarget.GlobalPosition.Lerp(StepDownPos, FootLerpSpeed * (float)delta);
                if (CurrFootIKTarget.GlobalPosition.DistanceSquaredTo(StepUpPos) > .01)
                {
                    IsStepDown = false;
                    IsStepping = false;
                }
            }
        }
    }
}
