using Godot;


public partial class RightHandTarget : Marker3D
{
    //     [Export] public Camera3D Cam3D;
    //     [Export] public Node3D RightHandEffector;
    //     [Export] public Marker3D AttackStartPosition;

    //     [Export] public float RightHandSensitivity = 0.01f;
    //     [Export] public Vector2 AttackPlaneSize = new(1.0f, 1.0f);

    //     private Vector2 ScreenCenter => GetViewport().GetVisibleRect().Size * 0.5f;
    //     private Vector3 HandRestLocalPos;

    //     public override void _Ready()
    //     {
    //         HandRestLocalPos = Position;
    //     }

    //     public override void _PhysicsProcess(double delta)
    //     {
    //         if (Input.IsActionJustPressed("attack"))
    //         {
    //             // transition hand from rest position to attack position
    //             GlobalPosition = AttackStartPosition.GlobalPosition;
    //             RightHandEffector.GlobalPosition = GlobalPosition;
    //         }
    //         if (Input.IsActionPressed("attack"))
    //         {

    //             Vector3 p0 = AttackStartPosition.GlobalPosition;
    //             Vector2 pix = GetViewport().GetMousePosition() - ScreenCenter;

    //             float x = Mathf.Clamp(pix.X * RightHandSensitivity, -AttackPlaneSize.X / 2, AttackPlaneSize.X / 2);
    //             float y = Mathf.Clamp(-pix.Y * RightHandSensitivity, -AttackPlaneSize.Y / 2, AttackPlaneSize.Y / 2);

    //             Vector3 target = new Vector3(p0.X + x, p0.Y + y, p0.Z);
    //             GlobalPosition = target;
    //             RightHandEffector.GlobalPosition = GlobalPosition;

    //         }
    //         if (Input.IsActionJustReleased("attack"))
    //         {
    //             // maintain momentum/finish move and transition hand to rest position
    //             Position = HandRestLocalPos;
    //             RightHandEffector.GlobalPosition = GlobalPosition;
    //         }

    //     }

}
