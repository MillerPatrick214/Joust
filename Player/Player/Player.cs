using Godot;
using System;
using Godot.Collections;
using System.IO;

public partial class Player : CharacterBody3D
{
    Vector2 MouseCoords = Vector2.Zero;
    Vector2 input_dir;
    public override void _Ready()
    {
        Vector2 Resolution = GetViewport().GetVisibleRect().Size; //This needs to be elsewhere eventually. This will change if viewport size changes during gameplay\
        
    }

    public override void _Process(double delta)
    {
        MouseCoords = GetViewport().GetMousePosition();

    }





}
