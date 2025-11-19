using Godot;

[GlobalClass]
public partial class HeldItem3D : RigidBody3D
{
    public enum ItemKind { None, Weapon, Mug }

    [ExportGroup("Identity")]
    [Export] public ItemKind Kind { get; set; }
    [Export] public bool IsTwoHanded { get; set; }

    [ExportGroup("Grips")]
    [Export] public Marker3D RightGrip { get; set; }
    [Export] public Marker3D LeftGrip { get; set; }

}
