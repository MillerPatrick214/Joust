using Godot;
using System;

public partial class NetManager : Node
{

	/*
	Should not be an autoload as every player scene added to game should carry one to manage net status
	*/

	[Export]
	private int _maxCLients = 10;
	private Control _ui;
	private Button _joinBtn;
	private Button _hostBtn;
	private Node _level;

	public override void _Ready()
	{
		_ui = GetNodeOrNull<Control>("UI");
		_joinBtn = GetNodeOrNull<Button>("UI/VBoxContainer2/HBoxContainer/Join");
		_hostBtn = GetNodeOrNull<Button>("UI/VBoxContainer2/HBoxContainer2/Host");
		_level = GetNodeOrNull<Node>("Level");

		if (_joinBtn == null || _hostBtn == null)
        {
            GD.PushError("Join/Host button paths are wrong. Double-check your scene tree paths.");
            return;
        }

		_joinBtn.Pressed += CreateClient;
		_hostBtn.Pressed += StartServer;

		Multiplayer.PeerConnected += id => GD.Print($"Peer connected: {id}");
        Multiplayer.PeerDisconnected += id => GD.Print($"Peer disconnected: {id}");
        Multiplayer.ConnectedToServer += () => {
            GD.Print($"Connected to server. My ID: {Multiplayer.GetUniqueId()}");
            // Send a test RPC as soon as we connect.
            Rpc(nameof(Ping), Multiplayer.GetUniqueId());
        };
		
        Multiplayer.ConnectionFailed += () => GD.PushWarning("Connection failed.");
		Multiplayer.ServerDisconnected += () => GD.PushWarning("Disconnected from server.");
    }

	[Rpc] // makes this callable over the network
	private void Ping(int fromId)
	{
		GD.Print($"[RPC] Ping from {fromId} -> I am {Multiplayer.GetUniqueId()}");
	}


    private void StartServer()
	{
		LineEdit hostPort = GetNodeOrNull<LineEdit>("UI/VBoxContainer2/HBoxContainer2/Port");
		int Port = hostPort.Text.ToInt();

		try
		{
			var peer = new ENetMultiplayerPeer();
			peer.CreateServer(Port, _maxCLients);
			Multiplayer.MultiplayerPeer = peer;

			// The server is the authority by default (ID = 1).
			GD.Print($"Server started on {Port}. My ID: {Multiplayer.GetUniqueId()}");
			_hostBtn.Disabled = true;
		}
		catch (Exception e)
		{
			GD.PushWarning($"SERVER HOSTING FAILED: {e}");
			return;
		}
		StartGame();
    }

	private void CreateClient()
	{
		LineEdit joinIP = GetNodeOrNull<LineEdit>("UI/VBoxContainer2/HBoxContainer/IP");
		LineEdit joinPort = GetNodeOrNull<LineEdit>("UI/VBoxContainer2/HBoxContainer/Port");

		string serverAddress = joinIP.Text;
		int port = joinPort.Text.ToInt();

		try
		{
			var peer = new ENetMultiplayerPeer();
			peer.CreateClient(serverAddress, port);
			Multiplayer.MultiplayerPeer = peer;

			GD.Print($"Connecting to {serverAddress}:{port}…");
			_joinBtn.Disabled = true;
		}
		catch (Exception e)
		{
			GD.PushWarning($"CLIENT CONNECT FAILED: {e}");
			_joinBtn.Disabled = false;
			return;
		}
		StartGame();
	}

	private void StartGame()
	{
		_ui.Hide();

		if (Multiplayer.IsServer())
		{
			CallDeferred(nameof(ChangeLevel), GD.Load("res://Levels/Level/World.tscn"));
		}
	}
	
	private void ChangeLevel(PackedScene scene)
    {
		foreach (Node currLevel in _level.GetChildren())
		{
			_level.RemoveChild(currLevel);
			currLevel.QueueFree();
		}
		_level.AddChild(scene.Instantiate());
    }
	
}
