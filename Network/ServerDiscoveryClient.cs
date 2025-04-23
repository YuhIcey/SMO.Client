using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SMOClient.Models;
using Lidgren.Network;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SMOClient.Network;

public class ServerDiscoveryClient : IDisposable
{
    private readonly NetPeer _client;
    private const int DISCOVERY_PORT = 14192;
    private readonly string _serverConfigPath;
    private readonly string _serverExecutablePath;
    private readonly Dictionary<string, ServerInfo> _servers = new();
    private bool _isDiscovering;

    public event Action<ServerInfo>? ServerDiscovered;
    public event Action<ServerInfo>? ServerUpdated;
    public event Action<string>? ServerRemoved;

    private class ServerConfig
    {
        public string IpAddress { get; set; } = "";
        public int Port { get; set; }
        public int TickRate { get; set; }
        public int MaxPlayers { get; set; }
        public string AppIdentifier { get; set; } = "";
        public string ServerName { get; set; } = "";
    }

    private ServerConfig? _serverConfig;

    public ServerDiscoveryClient()
    {
        var config = new NetPeerConfiguration("ROBCO INDUSTRIES (TM) TERMLINK PROTOCOL");
        config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
        config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
        config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
        config.EnableMessageType(NetIncomingMessageType.StatusChanged);
        config.Port = 0; // Let the system assign a random port
        
        _client = new NetPeer(config);
        _client.Start();
        
        _servers = new Dictionary<string, ServerInfo>();
        _serverConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.json");
        _serverExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SMOServer.exe");
        
        LoadServerConfig();
        
        // Add hardcoded server
        var serverInfo = new ServerInfo
        {
            Id = "sierra-madre-1",
            Name = "Sierra Madre Online Server",
            IP = "26.238.240.168", // Using the IP from server.json
            Port = 14192,
            PlayerCount = 0,
            MaxPlayers = 32,
            GameMode = "Standard",
            Version = "1.0.0",
            Status = "ONLINE",
            Protocol = "ROBCO INDUSTRIES (TM) TERMLINK PROTOCOL",
            Latency = 0
        };
        
        var serverKey = $"{serverInfo.IP}:{serverInfo.Port}";
        _servers[serverKey] = serverInfo;
    }

    private void LoadServerConfig()
    {
        try
        {
            if (File.Exists(_serverConfigPath))
            {
                var jsonString = File.ReadAllText(_serverConfigPath);
                _serverConfig = JsonSerializer.Deserialize<ServerConfig>(jsonString);
                Debug.WriteLine($"[Discovery] Loaded server config from {_serverConfigPath}");
                Debug.WriteLine($"[Discovery] Server IP: {_serverConfig?.IpAddress}, Port: {_serverConfig?.Port}");
                
                // Add the server from config
                if (_serverConfig != null)
                {
                    AddServer(_serverConfig.IpAddress, _serverConfig.Port);
                }
            }
            else
            {
                Debug.WriteLine($"[Discovery] Server config not found at {_serverConfigPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Discovery] Error loading server config: {ex.Message}");
        }
    }

    public void Start()
    {
        if (!_client.Status.Equals(NetPeerStatus.Running))
        {
            _client.Start();
        }
    }

    public void Stop()
    {
        _client.Shutdown("Stopping discovery client");
    }

    public async Task DiscoverServersAsync()
    {
        // Since we have a hardcoded server, we don't need to do actual discovery
        foreach (var server in _servers.Values)
        {
            ServerDiscovered?.Invoke(server);
        }
        await Task.CompletedTask;
    }

    public void AddServer(string address, int port)
    {
        var key = $"{address}:{port}";
        if (!_servers.ContainsKey(key))
        {
            var server = new ServerInfo
            {
                Name = _serverConfig?.ServerName ?? "SMO Server",
                IP = address,
                Port = port,
                Status = "UNKNOWN",
                Protocol = "SMO_MP",
                Latency = -1,
                MaxPlayers = _serverConfig?.MaxPlayers ?? 32,
                PlayerCount = 0,
                Version = "0.1",
                LastUpdate = DateTime.Now
            };
            _servers[key] = server;
            ServerDiscovered?.Invoke(server);
            Debug.WriteLine($"[Discovery] Added server to list: {address}:{port}");
        }
    }

    public void RemoveServer(string address, int port)
    {
        var key = $"{address}:{port}";
        if (_servers.Remove(key))
        {
            ServerRemoved?.Invoke(key);
            Debug.WriteLine($"[Discovery] Removed server: {address}:{port}");
        }
    }

    public void ClearServers()
    {
        _servers.Clear();
        Debug.WriteLine("[Discovery] Cleared all servers");
    }

    public List<ServerInfo> GetServers()
    {
        return _servers.Values.ToList();
    }

    public void Dispose()
    {
        _client?.Shutdown("Client shutting down");
    }
} 