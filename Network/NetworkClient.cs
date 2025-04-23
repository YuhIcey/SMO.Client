using System;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using System.Net;
using System.Diagnostics;

namespace SMOClient.Network;

public class NetworkClient : IDisposable
{
    private NetClient _client;
    private bool _isDisposed;
    private bool _isConnected;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _receiveTask;
    private GamePlatform _platform;

    public event Action<ConnectionStatus>? OnConnectionStatusChanged;
    public event Action<NetIncomingMessage>? OnPacketReceived;

    public NetworkClient(GamePlatform platform = GamePlatform.Steam)
    {
        _platform = platform;
        var config = new NetPeerConfiguration("ROBCO INDUSTRIES (TM) TERMLINK PROTOCOL");
        config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
        config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
        config.EnableMessageType(NetIncomingMessageType.StatusChanged);
        config.EnableMessageType(NetIncomingMessageType.Data);
        config.EnableMessageType(NetIncomingMessageType.Receipt);
        config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
        config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
        config.EnableMessageType(NetIncomingMessageType.DebugMessage);
        config.EnableMessageType(NetIncomingMessageType.WarningMessage);
        config.EnableMessageType(NetIncomingMessageType.ErrorMessage);
        
        // Configure connection settings
        config.PingInterval = 1.0f;           // Send ping every second
        config.ConnectionTimeout = 30.0f;      // Wait 30 seconds before timing out
        config.MaximumHandshakeAttempts = 5;  // Try harder to establish connection
        config.ResendHandshakeInterval = 1.0f; // Retry handshake every second
        
        _client = new NetClient(config);
        _client.Start();
        
        _cancellationTokenSource = new CancellationTokenSource();
        _receiveTask = Task.Run(ReceiveLoop, _cancellationTokenSource.Token);
    }

    public async Task Connect(string ip, int port)
    {
        try
        {
            Debug.WriteLine($"[Network] Connecting to {ip}:{port}...");
            
            string username;
            string platformId;
            
            // Get platform-specific info
            switch (_platform)
            {
                case GamePlatform.Steam:
                    if (!SteamManager.Instance.IsInitialized)
                    {
                        if (!SteamManager.Instance.Initialize())
                        {
                            throw new Exception("Steam must be running to connect with Steam.");
                        }
                    }
                    username = SteamManager.Instance.PersonaName;
                    platformId = SteamManager.Instance.SteamID.ToString();
                    break;

                case GamePlatform.GOG:
                    // Generate a consistent but anonymous player ID
                    var machineHash = Math.Abs(Environment.MachineName.GetHashCode()) % 100000;
                    username = $"Player_{machineHash:D5}";
                    platformId = $"GOG_{machineHash:X8}";
                    break;

                default:
                    throw new Exception("Unsupported platform");
            }
            
            // Send initial connection request with platform info
            var hail = _client.CreateMessage();
            hail.Write("ROBCO INDUSTRIES (TM) TERMLINK PROTOCOL");
            hail.Write("CONNECT_REQUEST");
            hail.Write(_platform.ToString());
            hail.Write(username);
            hail.Write(platformId);
            
            Debug.WriteLine($"[Network] Connecting with {_platform} username: {username}");
            Debug.WriteLine($"[Network] {_platform} ID: {platformId}");
            
            var serverEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _client.Connect(serverEndpoint, hail);
            
            // Wait for connection response
            var startTime = DateTime.Now;
            var connectionTimeout = TimeSpan.FromSeconds(30);
            
            while ((DateTime.Now - startTime) < connectionTimeout)
            {
                if (_isConnected)
                {
                    Debug.WriteLine("[Network] Successfully connected to server!");
                    return;
                }
                await Task.Delay(100);
            }
            
            throw new TimeoutException("[Network] Connection attempt timed out after 30 seconds");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Network] Connection error: {ex.Message}");
            _isConnected = false;
            OnConnectionStatusChanged?.Invoke(ConnectionStatus.Failed);
            throw;
        }
    }

    public void Disconnect()
    {
        if (!_isConnected)
            return;

        _client.Disconnect("Client disconnecting");
        _isConnected = false;
        OnConnectionStatusChanged?.Invoke(ConnectionStatus.Disconnected);
    }

    public void SendPacket(byte[] data, byte deliveryMethod = 0)
    {
        if (!_isConnected)
            return;

        var msg = _client.CreateMessage();
        msg.Write(data);
        _client.SendMessage(msg, GetDeliveryMethod(deliveryMethod));
    }

    private NetDeliveryMethod GetDeliveryMethod(byte method)
    {
        return method switch
        {
            0 => NetDeliveryMethod.ReliableOrdered,
            1 => NetDeliveryMethod.ReliableUnordered,
            2 => NetDeliveryMethod.UnreliableSequenced,
            _ => NetDeliveryMethod.ReliableOrdered
        };
    }

    private void ReceiveLoop()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            NetIncomingMessage? msg;
            while ((msg = _client.ReadMessage()) != null)
            {
                try
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.StatusChanged:
                            var status = (NetConnectionStatus)msg.ReadByte();
                            var reason = msg.ReadString();
                            Debug.WriteLine($"[Network] Status changed to: {status}, Reason: {reason}");
                            
                            switch (status)
                            {
                                case NetConnectionStatus.Connected:
                                    _isConnected = true;
                                    OnConnectionStatusChanged?.Invoke(ConnectionStatus.Connected);
                                    
                                    // Send initialization sequence with platform info
                                    var initMsg = _client.CreateMessage();
                                    string initUsername;
                                    string initPlatformId;
                                    
                                    if (_platform == GamePlatform.Steam)
                                    {
                                        initUsername = SteamManager.Instance.PersonaName;
                                        initPlatformId = SteamManager.Instance.SteamID.ToString();
                                    }
                                    else
                                    {
                                        // Use same anonymous player ID as above
                                        var machineHash = Math.Abs(Environment.MachineName.GetHashCode()) % 100000;
                                        initUsername = $"Player_{machineHash:D5}";
                                        initPlatformId = $"GOG_{machineHash:X8}";
                                    }
                                    
                                    initMsg.Write("ROBCO INDUSTRIES (TM) TERMLINK PROTOCOL");
                                    initMsg.Write("INITIALIZING NETWORK PROTOCOLS");
                                    initMsg.Write(_platform.ToString());
                                    initMsg.Write(initUsername);
                                    initMsg.Write(initPlatformId);
                                    _client.SendMessage(initMsg, _client.ServerConnection, NetDeliveryMethod.ReliableOrdered);
                                    break;
                                    
                                case NetConnectionStatus.Disconnected:
                                case NetConnectionStatus.Disconnecting:
                                    _isConnected = false;
                                    Debug.WriteLine($"[Network] Disconnected from server. Reason: {reason}");
                                    OnConnectionStatusChanged?.Invoke(ConnectionStatus.Disconnected);
                                    break;
                                    
                                case NetConnectionStatus.RespondedAwaitingApproval:
                                    Debug.WriteLine("[Network] Waiting for server approval...");
                                    break;
                                    
                                case NetConnectionStatus.RespondedConnect:
                                    Debug.WriteLine("[Network] Server responded to connection request...");
                                    break;

                                case NetConnectionStatus.InitiatedConnect:
                                    Debug.WriteLine("[Network] Connection initiated...");
                                    break;

                                case NetConnectionStatus.ReceivedInitiation:
                                    Debug.WriteLine("[Network] Received connection initiation...");
                                    break;
                            }
                            break;

                        case NetIncomingMessageType.Data:
                            OnPacketReceived?.Invoke(msg);
                            break;

                        case NetIncomingMessageType.UnconnectedData:
                            var header = msg.ReadString();
                            if (header == "ROBCO INDUSTRIES (TM) TERMLINK PROTOCOL")
                            {
                                var messageType = msg.ReadString();
                                Debug.WriteLine($"[Network] Received unconnected message: {messageType}");
                            }
                            break;

                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Debug.WriteLine($"[Network] {msg.MessageType}: {msg.ReadString()}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Network] Error processing message: {ex.Message}\nStack trace: {ex.StackTrace}");
                }
                finally
                {
                    _client.Recycle(msg);
                }
            }
            Thread.Sleep(1);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                Task.WaitAll(_receiveTask);
                _cancellationTokenSource.Dispose();

                if (_isConnected)
                    Disconnect();

                _client.Shutdown("Client shutting down");
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NetworkClient()
    {
        Dispose(false);
    }
}

public enum ConnectionStatus
{
    Connected,
    Disconnected,
    Failed
} 