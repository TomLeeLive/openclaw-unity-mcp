# OpenClaw Unity Bridge Documentation

## Overview

OpenClaw Unity Bridge connects your Unity project to the OpenClaw AI assistant, enabling natural language control of your game development workflow.

## Architecture

```
┌─────────────────┐     HTTP      ┌─────────────────┐
│   Unity Game    │ ◄──────────► │ OpenClaw Gateway│
│                 │               │                 │
│ OpenClawBridge  │               │   AI Assistant  │
│ OpenClawTools   │               │   (Claude, etc) │
│ OpenClawLogger  │               │                 │
└─────────────────┘               └─────────────────┘
```

## Components

### OpenClawBridge
Main component that handles HTTP communication with the OpenClaw gateway.

### OpenClawConfig
ScriptableObject for project-wide settings.

### OpenClawLogger
Captures Unity console logs for AI debugging assistance.

### OpenClawTools
Contains all tool implementations for AI interaction.

## API Reference

See the [README](../README.md) for the full list of available tools.
