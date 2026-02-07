# 🐾 OpenClaw Unity Plugin

Connect Unity to [OpenClaw](https://github.com/openclaw/openclaw) AI assistant via HTTP. Works in **Editor mode** without hitting Play!

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black?logo=unity)](https://unity.com)
[![OpenClaw](https://img.shields.io/badge/OpenClaw-2026.2.3+-green)](https://github.com/openclaw/openclaw)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## ✨ Key Features

- 🎮 **Works in Editor & Play Mode** - No need to hit Play to use AI tools
- 🔌 **Auto-Connect** - Connects when Unity starts, maintains connection across mode changes
- 📋 **Console Integration** - Capture and query Unity logs for debugging
- 🎬 **Scene Management** - List, load, and inspect scenes
- 🔧 **Component Editing** - Add, remove, and modify component properties
- 📸 **Debug Tools** - Screenshots, hierarchy view, and more
- 🔒 **Security Controls** - Configure what operations are allowed

## Requirements

| Component | Version |
|-----------|---------|
| **Unity** | 2021.3+ |
| **OpenClaw** | 2026.2.3+ |

## Installation

### Option 1: Git URL (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL...`
3. Enter:
   ```
   https://github.com/TomLeeLive/openclaw-unity-plugin.git
   ```

### Option 2: Local Package

1. Clone this repository
2. In Unity: `Window > Package Manager` → `+` → `Add package from disk...`
3. Select the `package.json` file

## Quick Start

### 1. Enable Unity Plugin in OpenClaw

```bash
openclaw plugins list | grep unity
# Should show: Unity MCP │ unity │ loaded

openclaw unity status
# Shows connection status
```

### 2. Configure in Unity

1. Open `Window > OpenClaw Bridge`
2. Set Gateway URL: `http://localhost:18789` (default)
3. Connection is automatic when Unity starts
4. Status shows green when connected

### 3. Chat with OpenClaw

Ask OpenClaw to inspect your scene, create objects, or debug issues - all without entering Play mode!

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Unity Editor                             │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           OpenClawEditorBridge                          │ │
│  │           [InitializeOnLoad]                            │ │
│  │                                                          │ │
│  │  • EditorApplication.delayCall → safe init              │ │
│  │  • EditorApplication.update → connection polling        │ │
│  │  • Maintains connection in Edit Mode                    │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │         OpenClawConnectionManager                       │ │
│  │         (Singleton - shared across modes)               │ │
│  │                                                          │ │
│  │  • HTTP polling for commands                            │ │
│  │  • Main thread execution queue                          │ │
│  │  • Automatic reconnection                               │ │
│  │  • Session management & heartbeat                       │ │
│  └──────────────────────┬─────────────────────────────────┘ │
│                         │                                    │
│                         ▼                                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │           OpenClawBridge                                │ │
│  │           (MonoBehaviour - Play Mode)                   │ │
│  │                                                          │ │
│  │  • Additional Update() for gameplay responsiveness      │ │
│  │  • Game-specific event hooks                            │ │
│  │  • Status overlay in Game view                          │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                   OpenClaw Gateway                            │
│                   http://localhost:18789                      │
│                                                               │
│  Endpoints:                                                   │
│  • POST /unity/register  - Register Unity session             │
│  • POST /unity/heartbeat - Keep session alive                 │
│  • GET  /unity/poll      - Poll for commands (long-polling)   │
│  • POST /unity/result    - Send tool execution results        │
└──────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Description |
|-----------|-------------|
| `OpenClawEditorBridge` | Editor-time initializer using `[InitializeOnLoad]` |
| `OpenClawConnectionManager` | Unified singleton handling all HTTP communication |
| `OpenClawBridge` | MonoBehaviour for Play mode responsiveness |
| `OpenClawTools` | 30+ Unity tools exposed to AI |
| `OpenClawWindow` | Editor window for configuration & status |

### Connection Flow

1. **Unity Starts** → `[InitializeOnLoad]` triggers
2. **Delayed Init** → Wait 2 seconds for editor stability
3. **Auto-Connect** → Connect to OpenClaw Gateway
4. **Polling Loop** → `EditorApplication.update` polls for commands
5. **Mode Change** → Connection maintained across Edit/Play transitions
6. **Reconnect** → Automatic recovery on connection loss

## Available Tools

### Console
| Tool | Description |
|------|-------------|
| `console.getLogs` | Get Unity console logs (with type filter) |
| `console.clear` | Clear captured logs |

### Scene
| Tool | Description |
|------|-------------|
| `scene.list` | List all scenes in build settings |
| `scene.getActive` | Get active scene info |
| `scene.getData` | Get scene hierarchy data |
| `scene.load` | Load a scene by name |

### GameObject
| Tool | Description |
|------|-------------|
| `gameobject.find` | Find by name, tag, or component type |
| `gameobject.create` | Create GameObject or primitive |
| `gameobject.destroy` | Destroy a GameObject |
| `gameobject.getData` | Get detailed object data |
| `gameobject.setActive` | Enable/disable object |
| `gameobject.setParent` | Change parent |

### Transform
| Tool | Description |
|------|-------------|
| `transform.setPosition` | Set world position |
| `transform.setRotation` | Set rotation (Euler) |
| `transform.setScale` | Set local scale |

### Component
| Tool | Description |
|------|-------------|
| `component.add` | Add component to object |
| `component.remove` | Remove component |
| `component.get` | Get component data |
| `component.set` | Set field/property value |
| `component.list` | List available types |

### Application
| Tool | Description |
|------|-------------|
| `app.getState` | Get play mode, FPS, etc. |
| `app.play` | Enter play mode (Editor) |
| `app.pause` | Toggle pause (Editor) |
| `app.stop` | Exit play mode (Editor) |

### Debug
| Tool | Description |
|------|-------------|
| `debug.log` | Write to console |
| `debug.screenshot` | Capture screenshot |
| `debug.hierarchy` | Text hierarchy view |

## Configuration

Create via `Assets > Create > OpenClaw > Config` and place in `Resources` folder.

| Setting | Description | Default |
|---------|-------------|---------|
| `gatewayUrl` | OpenClaw gateway URL | `http://localhost:18789` |
| `apiToken` | Optional API token | (empty) |
| `autoConnect` | Connect on start | `true` |
| `showStatusOverlay` | Show status in Game view | `true` |
| `captureConsoleLogs` | Capture logs for AI | `true` |
| `allowCodeExecution` | Allow code execution | `true` |
| `allowFileAccess` | Allow file operations | `true` |
| `allowSceneModification` | Allow scene changes | `true` |

## Example Usage

```
You: What GameObjects are in my scene?

OpenClaw: [Executes debug.hierarchy]

Your scene has:
▶ Main Camera [Camera, AudioListener]
▶ Directional Light [Light]
▶ Player [PlayerController, Rigidbody]
  ▶ Model [MeshRenderer]
▶ Environment
  ▶ Ground [MeshCollider]
```

```
You: Create 5 spheres in a circle

OpenClaw: [Executes gameobject.create 5 times]

Done! Created 5 spheres at radius 3 from origin.
```

## Troubleshooting

### Bridge won't connect
1. Check Gateway status: `openclaw gateway status`
2. Verify URL: default is `http://localhost:18789`
3. Check `Window > OpenClaw Bridge` for errors

### Connection shows "Reconnecting"
- Gateway may have restarted - auto-recovers
- Click "Force Reconnect" in Advanced section

### Tools work but parameters ignored
- Known issue with parameter parsing
- Will be fixed in future update

## ⚠️ Important Notes

- **Development Only**: Disable `allowCodeExecution` in production builds
- **Unity 6**: Some versions may have UPM stability issues

## License

MIT License - See [LICENSE](LICENSE)

---

Made with 🐾 by the OpenClaw community
