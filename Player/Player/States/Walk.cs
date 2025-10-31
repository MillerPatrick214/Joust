using Godot;
using System;

public partial class Walk : PlayerState
{
	// Called when the node enters the scene tree for the first time.
	public override void Enter(String previousState) {
		if (player == null)
		{
   	 		GD.PrintErr("player is null in walking state. Check your state machine.");
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void PhysicsUpdate(double delta)
	{
		
		Vector3 direction = Vector3.Zero;
		Vector3 velocity = player.Velocity;
		Vector3 forward = -player.GlobalTransform.Basis.Z.Normalized(); 
		Vector3 right = -player.GlobalTransform.Basis.X.Normalized(); 

		if (Input.IsActionJustPressed("jump")) {
       	 	EmitSignal(SignalName.Finished, JUMPING);
    	}
				
		if (Input.IsActionPressed("move_left")) {
			direction -= right;
		}
		if (Input.IsActionPressed("move_right")) {
			direction += right;
		}
		if (Input.IsActionPressed("move_forward")) {
			direction -= forward;
			//animation.Play("Back Camera"); 
			
		}
		if (Input.IsActionPressed("move_back")) {
			direction += forward;
			//animation.Play("Front Camera");
		}


		if (direction != Vector3.Zero) {
			direction = direction.Normalized();
		}
		
		if (Input.IsActionPressed("sprint")) {

			velocity.X = direction.X * player.Speed * 2;
        	velocity.Z = direction.Z * player.Speed * 2;
		}
		else {
			velocity.X = direction.X * player.Speed;
        	velocity.Z = direction.Z * player.Speed;
		}
		
		velocity.Y = player.Velocity.Y;
		

		player.Velocity = velocity;
		player.MoveAndSlide();	

		if (!player.IsOnFloor()) {
			EmitSignal(SignalName.Finished, FALL);
		}

		if(direction == Vector3.Zero) {
			EmitSignal(SignalName.Finished, IDLE);
		}
	}
}
