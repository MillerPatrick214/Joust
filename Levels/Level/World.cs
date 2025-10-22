using Godot;
using System;

public partial class World : Node3D
{
	private Node3D _players;
	public override void _Ready()
	{
		if (!Multiplayer.IsServer()) return;

		_players = GetNodeOrNull<Node3D>("Players");

		Multiplayer.PeerConnected += (id) => AddPlayer(id);
		Multiplayer.PeerDisconnected += (id) => RemovePlayer(id);

		foreach (long id in Multiplayer.GetPeers())
		{
			AddPlayer(id);
		}

		if (!OS.HasFeature("dedicated_server"))		//Acts as a host & a client. Test if necessary. Definitely not necessary when running headless dedicated server
        {
            AddPlayer(1);			
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _ExitTree()
    {
		if (!Multiplayer.IsServer()) return;
		Multiplayer.PeerConnected -= AddPlayer;
		Multiplayer.PeerDisconnected -= RemovePlayer;

    }


	public void AddPlayer(long id)
	{
		GD.Print($"[{Multiplayer.GetUniqueId()}] AddPlayer called for ID: {id}");
		
		// Check if player already exists
		if (_players.HasNode(id.ToString()))
		{
			GD.Print($"[{Multiplayer.GetUniqueId()}] Player {id} already exists!");
			return;
		}
		
		PackedScene playerScene = (PackedScene)GD.Load("res://Player/Player/Player.tscn");
		Player playerChar = playerScene.Instantiate<Player>();
		playerChar.Name = id.ToString();
		playerChar.PlayerID = (int)id;
		playerChar.Position = new Vector3(0, 5, 0);
		_players.AddChild(playerChar, true);
	}

	public void RemovePlayer(long id)
	{
		string playerName = id.ToString(); 
		if (!_players.HasNode(playerName)) return;
		_players.GetNode(playerName).QueueFree();
    }
}
