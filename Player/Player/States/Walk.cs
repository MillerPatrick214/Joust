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
		Vector3 velocity = player.Velocity;

		Vector3 direction = new Vector3(player.PlayerInput.inputDir.X, 0, player.PlayerInput.inputDir.Y);

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

		player.Velocity = velocity * player.GlobalTransform.Basis.Inverse();
		
		player.MoveAndSlide();	

		if (!player.IsOnFloor()) {
			EmitSignal(SignalName.Finished, FALL);
		}

		if(direction == Vector3.Zero) {
			EmitSignal(SignalName.Finished, IDLE);
		}
	}
}
