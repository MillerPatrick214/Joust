using Godot;
using System;
using System.Collections.Generic;

public partial class State : Node
{   
    [Signal]
    public delegate void FinishedEventHandler(String targetStatePath);

    [Signal]
    public delegate void AniStateEventHandler(String Ani);

    public virtual void Enter(string previousStatePath) {

    }
    public virtual void Exit() { 

    }
    public virtual void HandleInput(InputEvent evt) { 

    }
    public virtual void Update(double delta) { 

    }
    public virtual void PhysicsUpdate(double delta) { 

    }

    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
}
