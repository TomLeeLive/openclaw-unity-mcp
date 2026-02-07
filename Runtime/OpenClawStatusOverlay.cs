/*
 * OpenClaw Unity Plugin
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using UnityEngine;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Displays connection status overlay in Game view.
    /// </summary>
    public class OpenClawStatusOverlay : MonoBehaviour
    {
        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;
        private bool _initialized;
        
        private void InitStyles()
        {
            if (_initialized) return;
            
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 4, 4)
            };
            
            _initialized = true;
        }
        
        private void OnGUI()
        {
            if (!OpenClawConfig.Instance.showStatusOverlay) return;
            
            InitStyles();
            
            var bridge = OpenClawBridge.Instance;
            if (bridge == null) return;
            
            var statusColor = bridge.State switch
            {
                OpenClawBridge.ConnectionState.Connected => Color.green,
                OpenClawBridge.ConnectionState.Connecting => Color.yellow,
                OpenClawBridge.ConnectionState.Error => Color.red,
                _ => Color.gray
            };
            
            var statusText = bridge.State switch
            {
                OpenClawBridge.ConnectionState.Connected => "🟢 OpenClaw Connected",
                OpenClawBridge.ConnectionState.Connecting => "🟡 Connecting...",
                OpenClawBridge.ConnectionState.Error => "🔴 Error",
                _ => "⚪ Disconnected"
            };
            
            var width = 180f;
            var height = 24f;
            var padding = 10f;
            
            var rect = new Rect(
                Screen.width - width - padding,
                padding,
                width,
                height
            );
            
            var oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.color = oldColor;
            
            _labelStyle.normal.textColor = statusColor;
            GUI.Label(rect, statusText, _labelStyle);
        }
    }
}
