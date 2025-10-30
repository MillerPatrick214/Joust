using Godot;
using System;

public partial class Fall : PlayerState
{
	// Called when the node enters the scene tree for the first time.
	Vector3 FallVelocity = Vector3.Zero; 

	public override void Enter(String previousState)
	{
		player.AnimPlayer.Stop();
		FallVelocity.Y = 0; 
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void PhysicsUpdate(double delta)
	{
		
		FallVelocity.Y -= (float)(gravity * delta); //casting var as a float
		
		if (player.IsOnFloor()){
			EmitSignal(SignalName.Finished, IDLE);
		}

		Vector3 direction = Vector3.Zero;
		Vector3 velocity = player.Velocity;
		Vector3 forward = player.GlobalTransform.Basis.Z.Normalized(); //foward direction? This shit is confusing me honestly. The reason we need to do this is so we're always moving relative to the camera 
		Vector3 right = player.GlobalTransform.Basis.X.Normalized(); //Right?
				
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

		velocity.X = direction.X * player.AirManueverSpeed;
        velocity.Z = direction.Z * player.AirManueverSpeed;
		
		

		player.Velocity = velocity + FallVelocity;
		player.MoveAndSlide();	
	}
}
