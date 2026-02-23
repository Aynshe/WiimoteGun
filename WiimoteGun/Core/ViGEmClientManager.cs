using System;
using Nefarius.ViGEm.Client;
using WiimoteGun.Common;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN: Singleton manager for ViGEmBus client.
    /// FR: Gestionnaire Singleton pour le client ViGEmBus.
    /// </summary>
    public class ViGEmClientManager : IDisposable
    {
        private static ViGEmClientManager _instance;
        private static readonly object _lock = new object();
        private ViGEmClient _client;

        public ViGEmClient Client => _client;
        public bool IsAvailable => _client != null;

        private ViGEmClientManager()
        {
            try
            {
                _client = new ViGEmClient();
                SimpleLogger.Instance.Info("[ViGEmClientManager] ViGEmBus Client initialized.");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ViGEmClientManager] Failed to initialize ViGEmBus Client: {ex.Message}");
            }
        }

        public static ViGEmClientManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new ViGEmClientManager();
                    return _instance;
                }
            }
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
                SimpleLogger.Instance.Info("[ViGEmClientManager] ViGEmBus Client disposed.");
            }
        }
    }
}
