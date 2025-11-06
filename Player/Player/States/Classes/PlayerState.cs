public partial class PlayerState : State
{
	public const string IDLE = "Idle";
	public const string WALK = "Walk";
	public const string JUMPING = "Jumping";
	public const string FALL = "Fall";



	public Player player;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Owner.Ready += assignPlayerOwner;
	}
	
	private void assignPlayerOwner()
    {
        player = Owner as Player;
    }

}