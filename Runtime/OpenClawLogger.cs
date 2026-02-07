/*
 * OpenClaw Unity Bridge
 * https://github.com/openclaw/unity-bridge
 * MIT License
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Captures Unity console logs for AI debugging assistance.
    /// </summary>
    public class OpenClawLogger : IDisposable
    {
        public struct LogEntry
        {
            public DateTime timestamp;
            public string message;
            public string stackTrace;
            public LogType type;
            
            public string ToJson()
            {
                return $"{{\"time\":\"{timestamp:HH:mm:ss.fff}\",\"type\":\"{type}\",\"msg\":{EscapeJson(message)},\"stack\":{EscapeJson(stackTrace)}}}";
            }
            
            private static string EscapeJson(string s)
            {
                if (string.IsNullOrEmpty(s)) return "null";
                return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
            }
        }
        
        private readonly Queue<LogEntry> _logs = new Queue<LogEntry>();
        private readonly object _lock = new object();
        private readonly int _maxEntries;
        private readonly OpenClawConfig.LogLevel _minLevel;
        private bool _isCapturing;
        
        public int Count
        {
            get { lock (_lock) return _logs.Count; }
        }
        
        public OpenClawLogger(int maxEntries = 1000, OpenClawConfig.LogLevel minLevel = OpenClawConfig.LogLevel.Warning)
        {
            _maxEntries = maxEntries;
            _minLevel = minLevel;
        }
        
        public void StartCapture()
        {
            if (_isCapturing) return;
            Application.logMessageReceived += OnLogReceived;
            _isCapturing = true;
            Debug.Log("[OpenClaw] Log capture started");
        }
        
        public void StopCapture()
        {
            if (!_isCapturing) return;
            Application.logMessageReceived -= OnLogReceived;
            _isCapturing = false;
        }
        
        private void OnLogReceived(string message, string stackTrace, LogType type)
        {
            // Filter by log level
            var level = type switch
            {
                LogType.Log => OpenClawConfig.LogLevel.Log,
                LogType.Warning => OpenClawConfig.LogLevel.Warning,
                LogType.Error => OpenClawConfig.LogLevel.Error,
                LogType.Exception => OpenClawConfig.LogLevel.Exception,
                LogType.Assert => OpenClawConfig.LogLevel.Error,
                _ => OpenClawConfig.LogLevel.Log
            };
            
            if (level < _minLevel) return;
            
            // Ignore our own logs
            if (message.StartsWith("[OpenClaw]")) return;
            
            lock (_lock)
            {
                if (_logs.Count >= _maxEntries)
                    _logs.Dequeue();
                    
                _logs.Enqueue(new LogEntry
                {
                    timestamp = DateTime.Now,
                    message = message,
                    stackTrace = stackTrace,
                    type = type
                });
            }
        }
        
        public List<LogEntry> GetLogs(int count = 100, LogType? filterType = null)
        {
            var result = new List<LogEntry>();
            lock (_lock)
            {
                var logs = _logs.ToArray();
                for (int i = logs.Length - 1; i >= 0 && result.Count < count; i--)
                {
                    if (filterType == null || logs[i].type == filterType)
                        result.Add(logs[i]);
                }
            }
            result.Reverse();
            return result;
        }
        
        public void Clear()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }
        
        public string GetLogsJson(int count = 100, LogType? filterType = null)
        {
            var logs = GetLogs(count, filterType);
            var entries = new List<string>();
            foreach (var log in logs)
            {
                entries.Add(log.ToJson());
            }
            return "[" + string.Join(",", entries) + "]";
        }
        
        public void Dispose()
        {
            StopCapture();
            Clear();
        }
    }
}
