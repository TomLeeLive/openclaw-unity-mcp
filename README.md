# 🐾 OpenClaw Unity Bridge

Connect Unity to [OpenClaw](https://github.com/openclaw/openclaw) AI assistant. Execute code, inspect GameObjects, manage scenes, and debug with natural language.

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black?logo=unity)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![OpenClaw](https://img.shields.io/badge/OpenClaw-Compatible-green)](https://github.com/openclaw/openclaw)

## Features

- 🎮 **Full Unity Control** - Create, modify, and inspect GameObjects and Components
- 📋 **Console Integration** - Capture and query Unity logs for debugging
- 🎬 **Scene Management** - List, load, and inspect scenes
- 🔧 **Component Editing** - Add, remove, and modify component properties
- 📸 **Debug Tools** - Screenshots, hierarchy view, and more
- 🔒 **Security Controls** - Configure what operations are allowed

## Installation

### Option 1: Git URL (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL...`
3. Enter:
   ```
   https://github.com/TomLeeLive/openclaw-unity-bridge.git
   ```

### Option 2: Local Package

1. Clone this repository
2. In Unity: `Window > Package Manager` → `+` → `Add package from disk...`
3. Select the `package.json` file

## Quick Start

### 1. Enable Unity Plugin in OpenClaw

The Unity plugin is bundled with OpenClaw. Verify it's loaded:

```bash
openclaw plugins list | grep unity
# Should show: Unity Bridge │ unity │ loaded

openclaw unity status
# Shows connection status
```

### 2. Add Bridge to Unity Scene

1. **Add Bridge to Scene**
   - `GameObject > OpenClaw > Add Bridge to Scene`
   - Or use `Window > OpenClaw Bridge > Quick Setup`

2. **Configure Gateway URL**
   - Open `Window > OpenClaw Bridge`
   - Gateway URL: `http://localhost:18789` (default OpenClaw port)
   - Gateway Token: Your token from `~/.openclaw/openclaw.json` → `gateway.auth.token`

3. **Enter Play Mode**
   - The bridge will auto-connect to OpenClaw
   - Status overlay shows connection state

4. **Verify Connection**
   ```bash
   openclaw unity status
   # Should show your Unity project as connected
   ```

5. **Chat with OpenClaw**
   - Ask OpenClaw to inspect your scene, create objects, or debug issues!

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

Create a config asset via `Assets > Create > OpenClaw > Config` and place in `Resources` folder.

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

## HTTP Endpoints

The OpenClaw Unity plugin registers these endpoints on the Gateway:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/unity/register` | POST | Register Unity session |
| `/unity/heartbeat` | POST | Keep session alive |
| `/unity/poll` | GET | Poll for commands |
| `/unity/result` | POST | Send tool execution results |
| `/unity/message` | POST | Send messages to AI |
| `/unity/status` | GET | Get bridge status |

## Example Usage

### Chatting with OpenClaw

```
You: What GameObjects are in my scene?

OpenClaw: Let me check your scene hierarchy...
[Executes debug.hierarchy tool]

Your scene has:
▶ Main Camera [Camera, AudioListener]
▶ Directional Light [Light]
▶ Player [PlayerController, Rigidbody, CapsuleCollider]
  ▶ Model [MeshRenderer, MeshFilter]
  ▶ WeaponMount
▶ Environment
  ▶ Ground [MeshRenderer, MeshCollider]
  ▶ Obstacles...
```

```
You: Create 5 cubes in a circle

OpenClaw: I'll create 5 cubes arranged in a circle...
[Executes gameobject.create 5 times with calculated positions]

Done! Created 5 cubes at radius 3 from origin.
```

```
You: The player is moving too fast, can you check the speed setting?

OpenClaw: Let me inspect the PlayerController component...
[Executes component.get for PlayerController]

The PlayerController has:
- moveSpeed: 15
- jumpForce: 8
- gravity: 20

The moveSpeed of 15 seems high. Would you like me to reduce it?
```

## Troubleshooting

### Bridge won't connect
1. Verify Gateway is running: `openclaw gateway status`
2. Check Gateway URL matches (default: `http://localhost:18789`)
3. Ensure Unity plugin is loaded: `openclaw plugins list | grep unity`

### No response from tools
1. Confirm Unity is in Play Mode
2. Check console for connection errors
3. Enable Debug Mode on OpenClawBridge component

### Connection drops frequently
1. Increase heartbeat interval in config
2. Check network stability
3. Review Gateway logs: `openclaw gateway logs`

## Security Notes

⚠️ **This tool is designed for development use only.**

- Disable `allowCodeExecution` in production builds
- Use API tokens when exposing gateway to network
- The bridge runs in Play Mode only by default

## Requirements

- Unity 2021.3 or later (Unity 6 recommended)
- OpenClaw Gateway with Unity plugin enabled

## License

MIT License - See [LICENSE](LICENSE)

## Contributing

Contributions welcome! Please read the contributing guidelines first.

---

Made with 🐾 by the OpenClaw community
