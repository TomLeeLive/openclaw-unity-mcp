/*
 * OpenClaw Unity Plugin
 * https://github.com/TomLeeLive/openclaw-unity-plugin
 * MIT License
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenClaw.Unity
{
    /// <summary>
    /// Tool implementations for OpenClaw AI to interact with Unity.
    /// </summary>
    public class OpenClawTools
    {
        private readonly OpenClawBridge _bridge;
        private readonly Dictionary<string, Func<Dictionary<string, object>, object>> _tools;
        
        public OpenClawTools(OpenClawBridge bridge)
        {
            _bridge = bridge;
            _tools = new Dictionary<string, Func<Dictionary<string, object>, object>>
            {
                // Console
                { "console.getLogs", ConsoleGetLogs },
                { "console.clear", ConsoleClear },
                
                // Scene
                { "scene.list", SceneList },
                { "scene.getActive", SceneGetActive },
                { "scene.getData", SceneGetData },
                { "scene.load", SceneLoad },
                
                // GameObject
                { "gameobject.find", GameObjectFind },
                { "gameobject.create", GameObjectCreate },
                { "gameobject.destroy", GameObjectDestroy },
                { "gameobject.getData", GameObjectGetData },
                { "gameobject.setActive", GameObjectSetActive },
                { "gameobject.setParent", GameObjectSetParent },
                
                // Transform
                { "transform.setPosition", TransformSetPosition },
                { "transform.setRotation", TransformSetRotation },
                { "transform.setScale", TransformSetScale },
                
                // Component
                { "component.add", ComponentAdd },
                { "component.remove", ComponentRemove },
                { "component.get", ComponentGet },
                { "component.set", ComponentSet },
                { "component.list", ComponentList },
                
                // Script
                { "script.execute", ScriptExecute },
                { "script.read", ScriptRead },
                { "script.list", ScriptList },
                
                // Application
                { "app.getState", AppGetState },
                { "app.play", AppPlay },
                { "app.pause", AppPause },
                { "app.stop", AppStop },
                
                // Debug
                { "debug.log", DebugLog },
                { "debug.screenshot", DebugScreenshot },
                { "debug.hierarchy", DebugHierarchy },
            };
        }
        
        public List<Dictionary<string, object>> GetToolList()
        {
            return _tools.Keys.Select(name => new Dictionary<string, object>
            {
                { "name", name },
                { "description", GetToolDescription(name) }
            }).ToList();
        }
        
        public object Execute(string toolName, string parametersJson)
        {
            if (!_tools.TryGetValue(toolName, out var tool))
            {
                throw new ArgumentException($"Unknown tool: {toolName}");
            }
            
            var parameters = ParseJson(parametersJson);
            return tool(parameters);
        }
        
        #region Console Tools
        
        private object ConsoleGetLogs(Dictionary<string, object> p)
        {
            var count = GetInt(p, "count", 100);
            var type = GetString(p, "type", null);
            
            LogType? filterType = null;
            if (!string.IsNullOrEmpty(type))
            {
                filterType = type.ToLower() switch
                {
                    "error" => LogType.Error,
                    "warning" => LogType.Warning,
                    "log" => LogType.Log,
                    "exception" => LogType.Exception,
                    _ => null
                };
            }
            
            return _bridge.Logger?.GetLogsJson(count, filterType) ?? "[]";
        }
        
        private object ConsoleClear(Dictionary<string, object> p)
        {
            _bridge.Logger?.Clear();
            return new { success = true };
        }
        
        #endregion
        
        #region Scene Tools
        
        private object SceneList(Dictionary<string, object> p)
        {
            var scenes = new List<Dictionary<string, object>>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                scenes.Add(new Dictionary<string, object>
                {
                    { "index", i },
                    { "path", path },
                    { "name", System.IO.Path.GetFileNameWithoutExtension(path) }
                });
            }
            return scenes;
        }
        
        private object SceneGetActive(Dictionary<string, object> p)
        {
            var scene = SceneManager.GetActiveScene();
            return new Dictionary<string, object>
            {
                { "name", scene.name },
                { "path", scene.path },
                { "buildIndex", scene.buildIndex },
                { "isLoaded", scene.isLoaded },
                { "rootCount", scene.rootCount }
            };
        }
        
        private object SceneGetData(Dictionary<string, object> p)
        {
            var sceneName = GetString(p, "name", null);
            Scene scene;
            
            if (string.IsNullOrEmpty(sceneName))
            {
                scene = SceneManager.GetActiveScene();
            }
            else
            {
                scene = SceneManager.GetSceneByName(sceneName);
            }
            
            var rootObjects = scene.GetRootGameObjects();
            var objects = new List<Dictionary<string, object>>();
            
            foreach (var go in rootObjects)
            {
                objects.Add(GetGameObjectData(go, GetInt(p, "depth", 2)));
            }
            
            return new Dictionary<string, object>
            {
                { "name", scene.name },
                { "rootObjects", objects }
            };
        }
        
        private object SceneLoad(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var mode = GetString(p, "mode", "Single");
            
            var loadMode = mode.ToLower() == "additive" 
                ? LoadSceneMode.Additive 
                : LoadSceneMode.Single;
            
            SceneManager.LoadScene(name, loadMode);
            return new { success = true, scene = name };
        }
        
        #endregion
        
        #region GameObject Tools
        
        private object GameObjectFind(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var tag = GetString(p, "tag", null);
            var type = GetString(p, "type", null);
            
            GameObject[] results;
            
            if (!string.IsNullOrEmpty(name))
            {
                var go = GameObject.Find(name);
                results = go != null ? new[] { go } : new GameObject[0];
            }
            else if (!string.IsNullOrEmpty(tag))
            {
                results = GameObject.FindGameObjectsWithTag(tag);
            }
            else if (!string.IsNullOrEmpty(type))
            {
                var componentType = FindType(type);
                if (componentType != null)
                {
                    var components = UnityEngine.Object.FindObjectsByType(componentType, FindObjectsSortMode.None);
                    results = components.Select(c => (c as Component)?.gameObject).Where(g => g != null).ToArray();
                }
                else
                {
                    results = new GameObject[0];
                }
            }
            else
            {
                results = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            }
            
            var depth = GetInt(p, "depth", 1);
            return results.Take(100).Select(go => GetGameObjectData(go, depth)).ToList();
        }
        
        private object GameObjectCreate(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", "New GameObject");
            var primitive = GetString(p, "primitive", null);
            
            GameObject go;
            
            if (!string.IsNullOrEmpty(primitive))
            {
                var type = primitive.ToLower() switch
                {
                    "cube" => PrimitiveType.Cube,
                    "sphere" => PrimitiveType.Sphere,
                    "capsule" => PrimitiveType.Capsule,
                    "cylinder" => PrimitiveType.Cylinder,
                    "plane" => PrimitiveType.Plane,
                    "quad" => PrimitiveType.Quad,
                    _ => PrimitiveType.Cube
                };
                go = GameObject.CreatePrimitive(type);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
            }
            
            // Set position if provided
            if (p.ContainsKey("position"))
            {
                go.transform.position = ParseVector3(p["position"]);
            }
            
            return GetGameObjectData(go, 1);
        }
        
        private object GameObjectDestroy(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                UnityEngine.Object.Destroy(go);
                return new { success = true, destroyed = name };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object GameObjectGetData(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go == null)
            {
                return new { error = $"GameObject '{name}' not found" };
            }
            
            return GetGameObjectData(go, GetInt(p, "depth", 3));
        }
        
        private object GameObjectSetActive(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var active = GetBool(p, "active", true);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                go.SetActive(active);
                return new { success = true, name = name, active = active };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object GameObjectSetParent(Dictionary<string, object> p)
        {
            var childName = GetString(p, "child", null);
            var parentName = GetString(p, "parent", null);
            
            var child = GameObject.Find(childName);
            var parent = string.IsNullOrEmpty(parentName) ? null : GameObject.Find(parentName);
            
            if (child != null)
            {
                child.transform.SetParent(parent?.transform);
                return new { success = true };
            }
            
            return new { success = false, error = $"Child '{childName}' not found" };
        }
        
        private Dictionary<string, object> GetGameObjectData(GameObject go, int depth)
        {
            var data = new Dictionary<string, object>
            {
                { "name", go.name },
                { "tag", go.tag },
                { "layer", LayerMask.LayerToName(go.layer) },
                { "active", go.activeSelf },
                { "position", Vec3ToDict(go.transform.position) },
                { "rotation", Vec3ToDict(go.transform.eulerAngles) },
                { "scale", Vec3ToDict(go.transform.localScale) },
                { "components", go.GetComponents<Component>().Select(c => c?.GetType().Name).Where(n => n != null).ToList() }
            };
            
            if (depth > 0 && go.transform.childCount > 0)
            {
                var children = new List<Dictionary<string, object>>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    children.Add(GetGameObjectData(go.transform.GetChild(i).gameObject, depth - 1));
                }
                data["children"] = children;
            }
            
            return data;
        }
        
        #endregion
        
        #region Transform Tools
        
        private object TransformSetPosition(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                go.transform.position = new Vector3(
                    GetFloat(p, "x", go.transform.position.x),
                    GetFloat(p, "y", go.transform.position.y),
                    GetFloat(p, "z", go.transform.position.z)
                );
                return new { success = true, position = Vec3ToDict(go.transform.position) };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object TransformSetRotation(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                go.transform.eulerAngles = new Vector3(
                    GetFloat(p, "x", go.transform.eulerAngles.x),
                    GetFloat(p, "y", go.transform.eulerAngles.y),
                    GetFloat(p, "z", go.transform.eulerAngles.z)
                );
                return new { success = true, rotation = Vec3ToDict(go.transform.eulerAngles) };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        private object TransformSetScale(Dictionary<string, object> p)
        {
            var name = GetString(p, "name", null);
            var go = GameObject.Find(name);
            
            if (go != null)
            {
                go.transform.localScale = new Vector3(
                    GetFloat(p, "x", go.transform.localScale.x),
                    GetFloat(p, "y", go.transform.localScale.y),
                    GetFloat(p, "z", go.transform.localScale.z)
                );
                return new { success = true, scale = Vec3ToDict(go.transform.localScale) };
            }
            
            return new { success = false, error = $"GameObject '{name}' not found" };
        }
        
        #endregion
        
        #region Component Tools
        
        private object ComponentAdd(Dictionary<string, object> p)
        {
            var goName = GetString(p, "gameObject", null);
            var typeName = GetString(p, "type", null);
            
            var go = GameObject.Find(goName);
            if (go == null)
            {
                return new { success = false, error = $"GameObject '{goName}' not found" };
            }
            
            var type = FindType(typeName);
            if (type == null)
            {
                return new { success = false, error = $"Type '{typeName}' not found" };
            }
            
            var component = go.AddComponent(type);
            return new { success = true, component = type.Name };
        }
        
        private object ComponentRemove(Dictionary<string, object> p)
        {
            var goName = GetString(p, "gameObject", null);
            var typeName = GetString(p, "type", null);
            
            var go = GameObject.Find(goName);
            if (go == null)
            {
                return new { success = false, error = $"GameObject '{goName}' not found" };
            }
            
            var type = FindType(typeName);
            if (type == null)
            {
                return new { success = false, error = $"Type '{typeName}' not found" };
            }
            
            var component = go.GetComponent(type);
            if (component != null)
            {
                UnityEngine.Object.Destroy(component);
                return new { success = true };
            }
            
            return new { success = false, error = $"Component '{typeName}' not found on '{goName}'" };
        }
        
        private object ComponentGet(Dictionary<string, object> p)
        {
            var goName = GetString(p, "gameObject", null);
            var typeName = GetString(p, "type", null);
            
            var go = GameObject.Find(goName);
            if (go == null)
            {
                return new { error = $"GameObject '{goName}' not found" };
            }
            
            var type = FindType(typeName);
            if (type == null)
            {
                return new { error = $"Type '{typeName}' not found" };
            }
            
            var component = go.GetComponent(type);
            if (component == null)
            {
                return new { error = $"Component '{typeName}' not found on '{goName}'" };
            }
            
            return GetComponentData(component);
        }
        
        private object ComponentSet(Dictionary<string, object> p)
        {
            var goName = GetString(p, "gameObject", null);
            var typeName = GetString(p, "type", null);
            var field = GetString(p, "field", null);
            var value = p.ContainsKey("value") ? p["value"] : null;
            
            var go = GameObject.Find(goName);
            if (go == null)
            {
                return new { success = false, error = $"GameObject '{goName}' not found" };
            }
            
            var type = FindType(typeName);
            var component = go.GetComponent(type);
            if (component == null)
            {
                return new { success = false, error = $"Component '{typeName}' not found" };
            }
            
            try
            {
                var fieldInfo = type.GetField(field, BindingFlags.Public | BindingFlags.Instance);
                var propInfo = type.GetProperty(field, BindingFlags.Public | BindingFlags.Instance);
                
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(component, ConvertValue(value, fieldInfo.FieldType));
                    return new { success = true };
                }
                else if (propInfo != null && propInfo.CanWrite)
                {
                    propInfo.SetValue(component, ConvertValue(value, propInfo.PropertyType));
                    return new { success = true };
                }
                
                return new { success = false, error = $"Field/Property '{field}' not found or not writable" };
            }
            catch (Exception e)
            {
                return new { success = false, error = e.Message };
            }
        }
        
        private object ComponentList(Dictionary<string, object> p)
        {
            var prefix = GetString(p, "prefix", "").ToLower();
            var types = new List<string>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(Component).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            if (string.IsNullOrEmpty(prefix) || type.Name.ToLower().Contains(prefix))
                            {
                                types.Add(type.FullName);
                            }
                        }
                    }
                }
                catch { }
            }
            
            return types.OrderBy(t => t).Take(100).ToList();
        }
        
        private Dictionary<string, object> GetComponentData(Component component)
        {
            var type = component.GetType();
            var data = new Dictionary<string, object>
            {
                { "type", type.Name },
                { "fullType", type.FullName }
            };
            
            var fields = new Dictionary<string, object>();
            
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    fields[field.Name] = SerializeValue(field.GetValue(component));
                }
                catch { }
            }
            
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        fields[prop.Name] = SerializeValue(prop.GetValue(component));
                    }
                    catch { }
                }
            }
            
            data["fields"] = fields;
            return data;
        }
        
        #endregion
        
        #region Script Tools
        
        private object ScriptExecute(Dictionary<string, object> p)
        {
            if (!OpenClawConfig.Instance.allowCodeExecution)
            {
                return new { success = false, error = "Code execution is disabled in config" };
            }
            
            var code = GetString(p, "code", null);
            if (string.IsNullOrEmpty(code))
            {
                return new { success = false, error = "No code provided" };
            }
            
            // Note: Full C# execution requires Roslyn or similar
            // For now, we support simple eval-style operations
            try
            {
                // Try to interpret as a simple command
                if (code.StartsWith("Debug.Log("))
                {
                    var msg = code.Substring(10, code.Length - 12);
                    Debug.Log(msg);
                    return new { success = true, output = msg };
                }
                
                return new { 
                    success = false, 
                    error = "Dynamic code execution requires Roslyn. Use tools instead.",
                    suggestion = "Use gameobject.*, component.*, or other tools for Unity operations."
                };
            }
            catch (Exception e)
            {
                return new { success = false, error = e.Message };
            }
        }
        
        private object ScriptRead(Dictionary<string, object> p)
        {
            if (!OpenClawConfig.Instance.allowFileAccess)
            {
                return new { success = false, error = "File access is disabled in config" };
            }
            
            var path = GetString(p, "path", null);
            
            #if UNITY_EDITOR
            if (System.IO.File.Exists(path))
            {
                var content = System.IO.File.ReadAllText(path);
                return new { success = true, content = content };
            }
            #endif
            
            return new { success = false, error = $"File not found: {path}" };
        }
        
        private object ScriptList(Dictionary<string, object> p)
        {
            var scripts = new List<string>();
            
            #if UNITY_EDITOR
            var folder = GetString(p, "folder", "Assets/Scripts");
            if (System.IO.Directory.Exists(folder))
            {
                scripts = System.IO.Directory.GetFiles(folder, "*.cs", System.IO.SearchOption.AllDirectories)
                    .Select(f => f.Replace("\\", "/"))
                    .ToList();
            }
            #endif
            
            return scripts;
        }
        
        #endregion
        
        #region Application Tools
        
        private object AppGetState(Dictionary<string, object> p)
        {
            return new Dictionary<string, object>
            {
                { "isPlaying", Application.isPlaying },
                { "isPaused", false }, // EditorApplication.isPaused in Editor
                { "platform", Application.platform.ToString() },
                { "unityVersion", Application.unityVersion },
                { "productName", Application.productName },
                { "dataPath", Application.dataPath },
                { "fps", 1f / Time.deltaTime },
                { "time", Time.time }
            };
        }
        
        private object AppPlay(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = true;
            return new { success = true };
            #else
            return new { success = false, error = "Can only control play mode in Editor" };
            #endif
        }
        
        private object AppPause(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = !UnityEditor.EditorApplication.isPaused;
            return new { success = true, isPaused = UnityEditor.EditorApplication.isPaused };
            #else
            return new { success = false, error = "Can only control pause in Editor" };
            #endif
        }
        
        private object AppStop(Dictionary<string, object> p)
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            return new { success = true };
            #else
            return new { success = false, error = "Can only control play mode in Editor" };
            #endif
        }
        
        #endregion
        
        #region Debug Tools
        
        private object DebugLog(Dictionary<string, object> p)
        {
            var message = GetString(p, "message", "");
            var level = GetString(p, "level", "log");
            
            switch (level.ToLower())
            {
                case "error":
                    Debug.LogError($"[OpenClaw] {message}");
                    break;
                case "warning":
                    Debug.LogWarning($"[OpenClaw] {message}");
                    break;
                default:
                    Debug.Log($"[OpenClaw] {message}");
                    break;
            }
            
            return new { success = true };
        }
        
        private object DebugScreenshot(Dictionary<string, object> p)
        {
            var filename = GetString(p, "filename", $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            var path = System.IO.Path.Combine(Application.persistentDataPath, filename);
            
            try
            {
                // Use CaptureScreenshotAsTexture for immediate capture (requires Play mode)
                if (!Application.isPlaying)
                {
                    return new { success = false, error = "Screenshot requires Play mode. Please enter Play mode first." };
                }
                
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture != null)
                {
                    var bytes = texture.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, bytes);
                    UnityEngine.Object.Destroy(texture);
                    return new { success = true, path = path };
                }
                else
                {
                    return new { success = false, error = "Failed to capture screenshot texture" };
                }
            }
            catch (System.Exception e)
            {
                return new { success = false, error = e.Message };
            }
        }
        
        private object DebugHierarchy(Dictionary<string, object> p)
        {
            var depth = GetInt(p, "depth", 3);
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            
            var sb = new StringBuilder();
            foreach (var go in rootObjects)
            {
                AppendHierarchy(sb, go, 0, depth);
            }
            
            return sb.ToString();
        }
        
        private void AppendHierarchy(StringBuilder sb, GameObject go, int indent, int maxDepth)
        {
            sb.Append(new string(' ', indent * 2));
            sb.Append(go.activeSelf ? "▶ " : "▷ ");
            sb.Append(go.name);
            
            var components = go.GetComponents<Component>()
                .Where(c => c != null && c.GetType() != typeof(Transform))
                .Select(c => c.GetType().Name);
            
            if (components.Any())
            {
                sb.Append($" [{string.Join(", ", components)}]");
            }
            
            sb.AppendLine();
            
            if (indent < maxDepth)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    AppendHierarchy(sb, go.transform.GetChild(i).gameObject, indent + 1, maxDepth);
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        private string GetToolDescription(string name)
        {
            return name switch
            {
                "console.getLogs" => "Get Unity console logs with optional type filter",
                "console.clear" => "Clear captured logs",
                "scene.list" => "List all scenes in build settings",
                "scene.getActive" => "Get active scene info",
                "scene.getData" => "Get scene hierarchy data",
                "scene.load" => "Load a scene by name",
                "gameobject.find" => "Find GameObjects by name, tag, or component type",
                "gameobject.create" => "Create a new GameObject or primitive",
                "gameobject.destroy" => "Destroy a GameObject",
                "gameobject.getData" => "Get detailed GameObject data",
                "gameobject.setActive" => "Enable/disable a GameObject",
                "gameobject.setParent" => "Change GameObject parent",
                "transform.setPosition" => "Set position",
                "transform.setRotation" => "Set rotation (Euler angles)",
                "transform.setScale" => "Set local scale",
                "component.add" => "Add component to GameObject",
                "component.remove" => "Remove component from GameObject",
                "component.get" => "Get component data/fields",
                "component.set" => "Set component field value",
                "component.list" => "List available component types",
                "script.execute" => "Execute C# code (requires Roslyn)",
                "script.read" => "Read script file contents",
                "script.list" => "List script files in project",
                "app.getState" => "Get application state (playing, fps, etc)",
                "app.play" => "Enter play mode (Editor only)",
                "app.pause" => "Toggle pause (Editor only)",
                "app.stop" => "Exit play mode (Editor only)",
                "debug.log" => "Write to Unity console",
                "debug.screenshot" => "Capture screenshot",
                "debug.hierarchy" => "Get text hierarchy view",
                _ => name
            };
        }
        
        private Dictionary<string, object> ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new Dictionary<string, object>();
            
            // Simple JSON parser for flat objects
            var result = new Dictionary<string, object>();
            json = json.Trim();
            
            if (!json.StartsWith("{")) return result;
            
            json = json.Substring(1, json.Length - 2); // Remove { }
            
            // Very basic parsing - real implementation should use JsonUtility or Newtonsoft
            var parts = json.Split(',');
            foreach (var part in parts)
            {
                var kv = part.Split(new[] { ':' }, 2);
                if (kv.Length == 2)
                {
                    var key = kv[0].Trim().Trim('"');
                    var value = kv[1].Trim();
                    
                    if (value.StartsWith("\""))
                        result[key] = value.Trim('"');
                    else if (value == "true")
                        result[key] = true;
                    else if (value == "false")
                        result[key] = false;
                    else if (value == "null")
                        result[key] = null;
                    else if (float.TryParse(value, out var f))
                        result[key] = f;
                    else
                        result[key] = value;
                }
            }
            
            return result;
        }
        
        private string GetString(Dictionary<string, object> p, string key, string defaultValue)
        {
            return p.TryGetValue(key, out var v) && v != null ? v.ToString() : defaultValue;
        }
        
        private int GetInt(Dictionary<string, object> p, string key, int defaultValue)
        {
            if (p.TryGetValue(key, out var v))
            {
                if (v is int i) return i;
                if (v is float f) return (int)f;
                if (int.TryParse(v?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }
        
        private float GetFloat(Dictionary<string, object> p, string key, float defaultValue)
        {
            if (p.TryGetValue(key, out var v))
            {
                if (v is float f) return f;
                if (v is int i) return i;
                if (float.TryParse(v?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }
        
        private bool GetBool(Dictionary<string, object> p, string key, bool defaultValue)
        {
            if (p.TryGetValue(key, out var v))
            {
                if (v is bool b) return b;
                if (bool.TryParse(v?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }
        
        private Vector3 ParseVector3(object value)
        {
            if (value is Dictionary<string, object> dict)
            {
                return new Vector3(
                    GetFloat(dict, "x", 0),
                    GetFloat(dict, "y", 0),
                    GetFloat(dict, "z", 0)
                );
            }
            return Vector3.zero;
        }
        
        private Dictionary<string, float> Vec3ToDict(Vector3 v)
        {
            return new Dictionary<string, float> { { "x", v.x }, { "y", v.y }, { "z", v.z } };
        }
        
        private Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            
            // Try direct match first
            var type = Type.GetType(typeName);
            if (type != null) return type;
            
            // Search in all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null) return type;
                
                // Try with UnityEngine prefix
                type = assembly.GetType($"UnityEngine.{typeName}");
                if (type != null) return type;
            }
            
            return null;
        }
        
        private object SerializeValue(object value)
        {
            if (value == null) return null;
            
            var type = value.GetType();
            
            if (type.IsPrimitive || type == typeof(string))
                return value;
            
            if (value is Vector3 v3)
                return Vec3ToDict(v3);
            
            if (value is Vector2 v2)
                return new Dictionary<string, float> { { "x", v2.x }, { "y", v2.y } };
            
            if (value is Quaternion q)
                return new Dictionary<string, float> { { "x", q.x }, { "y", q.y }, { "z", q.z }, { "w", q.w } };
            
            if (value is Color c)
                return new Dictionary<string, float> { { "r", c.r }, { "g", c.g }, { "b", c.b }, { "a", c.a } };
            
            if (value is UnityEngine.Object obj)
                return obj.name;
            
            return value.ToString();
        }
        
        private object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            
            if (targetType == typeof(string))
                return value.ToString();
            
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);
            
            if (targetType == typeof(Vector3) && value is Dictionary<string, object> dict)
                return ParseVector3(dict);
            
            return value;
        }
        
        #endregion
    }
}
