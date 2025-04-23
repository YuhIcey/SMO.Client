using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using SMOClient.Models;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Net.Sockets;
using SMOClient.Network;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SMOClient.Utilities;
using System.Linq;
using System.Diagnostics;

namespace SMOClient;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private NetworkClient? _networkClient;
    private readonly SteamManager _steamManager;
    private readonly HttpClient _httpClient;
    private readonly DispatcherTimer _steamCallbackTimer;
    private readonly ObservableCollection<ServerInfo> _servers;
    private const int GAME_PORT = 14192;
    private const int DISCOVERY_PORT = 14192;
    private readonly DispatcherTimer _statusCheckTimer;
    private readonly ServerDiscoveryClient _discoveryClient;

    public MainWindow()
    {
        InitializeComponent();
        
        // Change colors to green theme
        this.Background = new SolidColorBrush(Color.FromRgb(15, 26, 15));  // Dark green-gray
        
        // Find all elements and update their colors
        var buttons = this.FindVisualChildren<Button>();
        var textBlocks = this.FindVisualChildren<TextBlock>();
        var borders = this.FindVisualChildren<Border>();
        var dataGrids = this.FindVisualChildren<DataGrid>();
        var dataGridColumnHeaders = this.FindVisualChildren<DataGridColumnHeader>();
        var dataGridRows = this.FindVisualChildren<DataGridRow>();

        foreach (var button in buttons)
        {
            button.Background = new SolidColorBrush(Color.FromRgb(47, 255, 47));  // Medium green
            button.Foreground = new SolidColorBrush(Color.FromRgb(15, 26, 15));   // Dark green-gray
            button.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 255, 80)); // Light green
        }

        foreach (var textBlock in textBlocks)
        {
            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(165, 255, 165)); // Very light green
        }

        foreach (var border in borders)
        {
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(47, 255, 47));  // Medium green
            border.Background = new SolidColorBrush(Color.FromRgb(31, 42, 31));    // Slightly lighter green-gray
        }

        foreach (var grid in dataGrids)
        {
            grid.Background = new SolidColorBrush(Color.FromRgb(31, 42, 31));     // Slightly lighter green-gray
            grid.BorderBrush = new SolidColorBrush(Color.FromRgb(47, 255, 47));   // Medium green
            grid.Foreground = new SolidColorBrush(Color.FromRgb(165, 255, 165));  // Very light green
            grid.HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(47, 255, 47));  // Medium green
            grid.VerticalGridLinesBrush = new SolidColorBrush(Color.FromRgb(47, 255, 47));    // Medium green
        }

        foreach (var header in dataGridColumnHeaders)
        {
            header.Background = new SolidColorBrush(Color.FromRgb(47, 255, 47));   // Medium green
            header.Foreground = new SolidColorBrush(Color.FromRgb(15, 26, 15));    // Dark green-gray
            header.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 255, 80));  // Light green
        }

        foreach (var row in dataGridRows)
        {
            row.Background = new SolidColorBrush(Color.FromRgb(31, 42, 31));     // Slightly lighter green-gray
            row.Foreground = new SolidColorBrush(Color.FromRgb(165, 255, 165));  // Very light green
        }

        // Change StatusText color
        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(165, 255, 165));  // Very light green

        _networkClient = new NetworkClient();
        _steamManager = SteamManager.Instance;
        if (!_steamManager.Initialize())
        {
            MessageBox.Show("Failed to initialize Steam. Please make sure Steam is running and you are logged in.", 
                "Steam Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        _httpClient = new HttpClient();
        
        _servers = new ObservableCollection<ServerInfo>();
        ServerGrid.ItemsSource = _servers;

        _steamCallbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _steamCallbackTimer.Tick += (s, e) => _steamManager.Update();
        _steamCallbackTimer.Start();

        _discoveryClient = new ServerDiscoveryClient();
        
        // Initialize status check timer
        _statusCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _statusCheckTimer.Tick += StatusCheckTimer_Tick;
        _statusCheckTimer.Start();

        UpdateSteamUserInfo();
        RefreshServerGrid();
    }

    private void UpdateSteamUserInfo()
    {
        if (_steamManager.IsInitialized)
        {
            SteamUsername.Text = _steamManager.PersonaName;
            var avatarImage = _steamManager.GetAvatarImage();
            if (avatarImage != null)
            {
                SteamAvatar.Source = avatarImage;
            }
        }
    }

    private async void RefreshServerGrid()
    {
        try
        {
            RefreshButton.IsEnabled = false;
            await _discoveryClient.DiscoverServersAsync();
            var servers = _discoveryClient.GetServers();
            
            if (servers.Count > 0)
            {
                var currentServer = servers[0]; // We only have one server for now
                
                // Update the UI
                if (ServerGrid.Items.Count > 0)
                {
                    var existingServer = (ServerInfo)ServerGrid.Items[0];
                    existingServer.Status = currentServer.Status;
                    existingServer.Latency = currentServer.Latency;
                    existingServer.PlayerCount = currentServer.PlayerCount;
                    
                    // Force UI refresh
                    ServerGrid.Items.Refresh();
                }
                else
                {
                    ServerGrid.ItemsSource = servers;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in status check: {ex.Message}");
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private async void StatusCheckTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            // Temporarily disable refresh button during check
            RefreshButton.IsEnabled = false;

            await _discoveryClient.DiscoverServersAsync();
            var servers = _discoveryClient.GetServers();
            
            if (servers.Count > 0)
            {
                var currentServer = servers[0]; // We only have one server for now
                
                // Update the UI
                if (ServerGrid.Items.Count > 0)
                {
                    var existingServer = (ServerInfo)ServerGrid.Items[0];
                    existingServer.Status = currentServer.Status;
                    existingServer.Latency = currentServer.Latency;
                    existingServer.PlayerCount = currentServer.PlayerCount;
                    
                    // Force UI refresh
                    ServerGrid.Items.Refresh();
                }
                else
                {
                    ServerGrid.ItemsSource = servers;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in status check: {ex.Message}");
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshServerGrid();
    }

    private void ServerGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedServer = ServerGrid.SelectedItem as ServerInfo;
        ConnectButton.IsEnabled = selectedServer != null;
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedServer = ServerGrid.SelectedItem as ServerInfo;
        if (selectedServer != null)
        {
            Console.WriteLine($"[MainWindow] Selected server: {selectedServer.Name}");
            Console.WriteLine($"[MainWindow] IP: '{selectedServer.IP}', Port: {selectedServer.Port}");
            
            try
            {
                // Dispose of any existing connection
                _networkClient?.Dispose();

                // Create new network client
                _networkClient = new NetworkClient();
                _networkClient.OnConnectionStatusChanged += status =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        switch (status)
                        {
                            case ConnectionStatus.Connected:
                                StatusText.Text = $"Connected to {selectedServer.Name}";
                                Console.WriteLine($"[MainWindow] Connected to {selectedServer.Name}");
                                break;
                            case ConnectionStatus.Disconnected:
                                StatusText.Text = "Disconnected from server";
                                Console.WriteLine("[MainWindow] Disconnected from server");
                                break;
                            case ConnectionStatus.Failed:
                                StatusText.Text = "Failed to connect to server";
                                Console.WriteLine("[MainWindow] Failed to connect to server");
                                break;
                        }
                    });
                };

                // Connect to server
                Console.WriteLine("[MainWindow] Attempting to connect...");
                StatusText.Text = $"Connecting to {selectedServer.Name}...";
                Console.WriteLine("[MainWindow] Connection initiated");
                
                try
                {
                    await _networkClient.Connect(selectedServer.IP, selectedServer.Port);
                }
                catch (TimeoutException)
                {
                    StatusText.Text = "Connection attempt timed out";
                    Console.WriteLine("[MainWindow] Connection attempt timed out");
                    MessageBox.Show($"Failed to connect to server: Connection timed out", 
                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Connection error";
                Console.WriteLine($"[MainWindow] Connection error: {ex.Message}");
                Console.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error connecting to server: {ex.Message}",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void TermsButton_Click(object sender, RoutedEventArgs e)
    {
        var tosWindow = new TermsOfServiceWindow();
        tosWindow.Owner = this;
        tosWindow.ShowDialog();
    }

    protected override void OnClosed(EventArgs e)
    {
        _networkClient?.Dispose();
        _steamCallbackTimer.Stop();
        _steamManager.Shutdown();
        _httpClient.Dispose();
        _statusCheckTimer.Stop();
        _discoveryClient.Dispose();
        base.OnClosed(e);
    }
}