# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2026-02-07

### Added
- **Editor Mode Support** - MCP connection now works without Play mode
- `OpenClawConnectionManager` - Unified connection manager for Editor & Play mode
- `OpenClawEditorBridge` - Editor-time initializer using `[InitializeOnLoad]`
- Automatic reconnection on connection loss
- Main thread execution queue for thread-safe tool calls

### Changed
- Renamed package from `openclaw-unity-bridge` to `openclaw-unity-plugin`
- Display name changed to "OpenClaw Unity Plugin"
- Connection maintained across Edit/Play mode transitions
- Deferred initialization to avoid Unity 6 UPM crashes

### Fixed
- UPM EPIPE crash on Unity 6 startup
- Connection status display in Editor window

## [1.0.0] - 2026-02-07

### Added
- Initial release
- 30+ Unity tools for AI interaction
- Console log capture
- Scene management
- GameObject manipulation
- Component editing
- Play mode control
- Debug tools (hierarchy, screenshot)
- Status overlay in Game view
- Configuration via ScriptableObject
