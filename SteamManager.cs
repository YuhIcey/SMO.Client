using Steamworks;
using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace SMOClient
{
    public class SteamManager
    {
        private static SteamManager? _instance;
        public static SteamManager Instance => _instance ??= new SteamManager();

        private bool _initialized;
        public bool IsInitialized => _initialized;

        public ulong SteamID { get; private set; }
        public string PersonaName { get; private set; } = string.Empty;

        private SteamManager() { }

        public bool Initialize()
        {
            if (_initialized) return true;

            try
            {
                if (!SteamAPI.Init())
                {
                    throw new Exception("Steam API failed to initialize. Please make sure Steam is running and you are logged in.");
                }

                SteamID = SteamUser.GetSteamID().m_SteamID;
                PersonaName = SteamFriends.GetPersonaName();
                _initialized = true;

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Steam initialization error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        public void Shutdown()
        {
            if (!_initialized) return;

            SteamAPI.Shutdown();
            _initialized = false;
        }

        public void Update()
        {
            if (!_initialized) return;
            SteamAPI.RunCallbacks();
        }

        public bool ValidateUser()
        {
            if (!_initialized) return false;
            return SteamUser.BLoggedOn();
        }

        public ImageSource? GetAvatarImage()
        {
            if (!_initialized) return null;

            try
            {
                int avatarHandle = SteamFriends.GetLargeFriendAvatar(new CSteamID(SteamID));
                uint width = 0, height = 0;
                if (!SteamUtils.GetImageSize(avatarHandle, out width, out height))
                    return null;

                byte[] buffer = new byte[4 * width * height];
                if (!SteamUtils.GetImageRGBA(avatarHandle, buffer, (int)(4 * width * height)))
                    return null;

                // Convert RGBA to BGRA (WPF format)
                for (int i = 0; i < buffer.Length; i += 4)
                {
                    byte r = buffer[i];
                    buffer[i] = buffer[i + 2];
                    buffer[i + 2] = r;
                }

                var bitmap = new WriteableBitmap((int)width, (int)height, 96, 96, PixelFormats.Bgra32, null);
                bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, (int)width, (int)height), buffer, (int)(4 * width), 0);
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
} 