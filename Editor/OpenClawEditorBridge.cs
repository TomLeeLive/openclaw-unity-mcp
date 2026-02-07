/*
 * OpenClaw Unity MCP - Editor Connection
 * Works in Edit mode without Play button using EditorApplication.update
 * https://github.com/TomLeeLive/openclaw-unity-mcp
 * MIT License
 */

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace OpenClaw.Unity.Editor
{
    /// <summary>
    /// Editor-time connection handler that uses EditorApplication.update for polling.
    /// This ensures MCP stays connected even when not in Play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class OpenClawEditorBridge
    {
        private static bool _initialized;
        private static bool _subscribed;
        private static OpenClawConfig _config;
        private static OpenClawLogger _logger;
        private static float _initDelay = 2f; // Wait 2 seconds after editor loads
        private static double _startTime;
        
        /// <summary>
        /// Static constructor runs when Unity Editor loads.
        /// Keep this minimal to avoid UPM crashes.
        /// </summary>
        static OpenClawEditorBridge()
        {
            // Use delayCall instead of update to be even safer
            EditorApplication.delayCall += () =>
            {
                _startTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += DeferredInitialize;
            };
        }
        
        /// <summary>
        /// Deferred initialization - waits for editor to stabilize.
        /// </summary>
        private static void DeferredInitialize()
        {
            // Wait for editor to stabilize
            if (EditorApplication.timeSinceStartup - _startTime < _initDelay)
                return;
            
            // Remove this callback
            EditorApplication.update -= DeferredInitialize;
            
            // Now do real initialization
            Initialize();
        }
        
        /// <summary>
        /// Initialize the editor bridge.
        /// </summary>
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            // Subscribe to editor events
            if (!_subscribed)
            {
                _subscribed = true;
                EditorApplication.update += OnEditorUpdate;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                EditorApplication.quitting += OnQuitting;
            }
            
            // Load config
            _config = OpenClawConfig.Instance;
            
            if (_config == null)
            {
                // No config, skip initialization
                return;
            }
            
            // Initialize logger if enabled
            if (_config.captureConsoleLogs)
            {
                try
                {
                    _logger = new OpenClawLogger(_config.maxLogEntries, _config.minLogLevel);
                    _logger.StartCapture();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[OpenClaw] Failed to start log capture: {e.Message}");
                }
            }
            
            // Initialize the unified connection manager
            try
            {
                OpenClawConnectionManager.Instance.Initialize(_config, _logger);
                // Delay the log message to avoid UPM pipe issues
                EditorApplication.delayCall += () => Debug.Log("[OpenClaw Editor] Initialized");
            }
            catch (Exception e)
            {
                EditorApplication.delayCall += () => Debug.LogWarning($"[OpenClaw] Init failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get the connection manager instance.
        /// </summary>
        public static OpenClawConnectionManager ConnectionManager => OpenClawConnectionManager.Instance;
        
        /// <summary>
        /// Check if connected.
        /// </summary>
        public static bool IsConnected => _initialized && OpenClawConnectionManager.Instance.IsConnected;
        
        /// <summary>
        /// Get session ID.
        /// </summary>
        public static string SessionId => OpenClawConnectionManager.Instance?.SessionId;
        
        /// <summary>
        /// Get connection state.
        /// </summary>
        public static OpenClawConnectionManager.ConnectionState State => 
            _initialized ? OpenClawConnectionManager.Instance.State : OpenClawConnectionManager.ConnectionState.Disconnected;
        
        /// <summary>
        /// Connect to the gateway.
        /// </summary>
        public static void Connect()
        {
            if (!_initialized) Initialize();
            OpenClawConnectionManager.Instance.ConnectAsync();
        }
        
        /// <summary>
        /// Disconnect from the gateway.
        /// </summary>
        public static void Disconnect()
        {
            OpenClawConnectionManager.Instance?.Disconnect();
        }
        
        /// <summary>
        /// EditorApplication.update callback - runs every editor frame.
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (!_initialized) return;
            
            try
            {
                // Update the connection manager
                OpenClawConnectionManager.Instance?.Update();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OpenClaw] Update error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Handle play mode state changes.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!_initialized) return;
            
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    // Verify connection is still active
                    if (!IsConnected && _config != null && _config.autoConnect)
                    {
                        Connect();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Handle editor quitting.
        /// </summary>
        private static void OnQuitting()
        {
            _logger?.Dispose();
            OpenClawConnectionManager.Instance?.Dispose();
        }
        
        /// <summary>
        /// Manually trigger reconnection.
        /// </summary>
        public static void Reconnect()
        {
            Disconnect();
            EditorApplication.delayCall += () => Connect();
        }
        
        /// <summary>
        /// Test connection to the gateway.
        /// </summary>
        public static void TestConnection(Action<bool, string> callback)
        {
            if (_config == null)
            {
                callback?.Invoke(false, "No config found");
                return;
            }
            
            var url = _config.gatewayUrl.TrimEnd('/') + "/api/health";
            
            var request = UnityEngine.Networking.UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
            
            operation.completed += _ =>
            {
                bool success = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success;
                string message = success ? request.downloadHandler.text : request.error;
                callback?.Invoke(success, message);
                request.Dispose();
            };
        }
    }
}
#endif
