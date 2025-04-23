using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace SMOClient.Models
{
    public class ServerInfo : INotifyPropertyChanged
    {
        private string _name = "";
        private string _ip = "";
        private int _port;
        private int _playerCount;
        private int _maxPlayers;
        private string _protocol = "ROBCO-NET/1.0";
        private string _version = "1.0.0";
        private int _latency;
        private string _status = "ONLINE";
        private string _appIdentifier = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        [JsonProperty("protocol")]
        public string Protocol
        {
            get => _protocol;
            set => SetField(ref _protocol, value);
        }

        [JsonProperty("type")]
        public string Type { get; set; } = "DISCOVERY_RESPONSE";

        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        [JsonProperty("playerCount")]
        public int PlayerCount
        {
            get => _playerCount;
            set => SetField(ref _playerCount, value);
        }

        [JsonProperty("maxPlayers")]
        public int MaxPlayers
        {
            get => _maxPlayers;
            set => SetField(ref _maxPlayers, value);
        }

        [JsonProperty("gameMode")]
        public string GameMode { get; set; } = "";

        [JsonProperty("version")]
        public string Version
        {
            get => _version;
            set => SetField(ref _version, value);
        }

        [JsonProperty("status")]
        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        // Additional client-side properties
        public string IP
        {
            get => _ip;
            set => SetField(ref _ip, value);
        }

        public int Port
        {
            get => _port;
            set => SetField(ref _port, value);
        }

        public DateTime LastUpdate { get; set; }

        public int Latency
        {
            get => _latency;
            set => SetField(ref _latency, value);
        }

        [JsonProperty("appIdentifier")]
        public string AppIdentifier
        {
            get => _appIdentifier;
            set => SetField(ref _appIdentifier, value);
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 