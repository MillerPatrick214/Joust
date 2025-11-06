using Godot;
using System;

public partial class Idle : PlayerState
{
	// Called when the node enters the scene tree for the first time.
	Vector3 ZeroVect = Vector3.Zero;

	public override void Enter(String previousState) { //NOTE -- I have previous state in here but there is no CURRENT functionality. I expect we'll add transitions later on. 
													   //Animation change goes here 
		player.Velocity = ZeroVect;
		//player.AnimPlayer.Play("Armature|Idle");
		//GD.Print($"Entered Idle movement state.");
	}

	public override void PhysicsUpdate(double delta) {
		if (!player.IsOnFloor()) {
			EmitSignal(SignalName.Finished, FALL);
		}

		if (Input.IsActionPressed("move_forward") || Input.IsActionPressed("move_left") || Input.IsActionPressed("move_right") || Input.IsActionPressed("move_back") ) {
			EmitSignal(SignalName.Finished, WALK);
		}

		if (Input.IsActionJustPressed("jump")) {
			EmitSignal(SignalName.Finished, JUMPING);
		}
		
		player.MoveAndSlide();
	}
}
