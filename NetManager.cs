using Godot;
using System;

public partial class NetManager : Node
{
	[Export]
	int MaxClients = 10;

	private Button _joinBtn;
	private Button _hostBtn;

	public override void _Ready()
	{
		_joinBtn = GetNodeOrNull<Button>("VBoxContainer2/HBoxContainer/Join");
		_hostBtn = GetNodeOrNull<Button>("VBoxContainer2/HBoxContainer2/Host");

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
		LineEdit _hostPort = GetNodeOrNull<LineEdit>("VBoxContainer2/HBoxContainer2/Port");
		int Port = _hostPort.Text.ToInt();
		
        try
        {
            var peer = new ENetMultiplayerPeer();
            peer.CreateServer(Port, MaxClients);
            Multiplayer.MultiplayerPeer = peer;

            // The server is the authority by default (ID = 1).
            GD.Print($"Server started on {Port}. My ID: {Multiplayer.GetUniqueId()}");
            _hostBtn.Disabled = true;
        }
        catch (Exception e)
        {
            GD.PushWarning($"SERVER HOSTING FAILED: {e}");
        }
    }

	private void CreateClient()
		{
		LineEdit joinIP = GetNodeOrNull<LineEdit>("VBoxContainer2/HBoxContainer/IP");
		LineEdit joinPort = GetNodeOrNull<LineEdit>("VBoxContainer2/HBoxContainer/Port");

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
			}
		}
		
}
