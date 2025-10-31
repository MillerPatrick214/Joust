using Godot;
using System;

public partial class Weapon : Node
{
    [Export] public Vector3 RightHandPOS = new();   //SHOULD ALWAYS BE ASSUMED TO BE {0,0,0}
    [Export] public Marker3D LeftHandmark = new();

}
