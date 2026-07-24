# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Fading To Zero** — A Unity 2D side-scroller game built for **GMTK 2026**. Unity **6000.0.77f1**, URP 2D Render Pipeline.

## Project Structure

```
Fading To Zero/
  Assets/
    Asset_2D/            # Character sprites, animations, animator controllers, sprite atlas
    Scenes/              # Unity scenes (Start Scene, previously SampleScene)
    Scripts/             # C# MonoBehaviour scripts
    Settings/            # URP pipeline assets, renderer config, scene templates
    InputSystem_Actions.inputactions   # Input Action Asset (Player + UI action maps)
```

## Key Architecture

- **Renderer**: Universal RP 2D Renderer (forward rendering, 2D lights/shadows)
- **Input**: Unity Input System package (`InputSystem_Actions.inputactions`) — Player actions: Move, Look, Attack, Interact, Crouch, Jump, Sprint, Previous, Next
- **Control schemes**: Keyboard+Mouse, Gamepad, Touch, Joystick, XR
- **Player**: `SideScrollerPlayer` (`playerMovementSideScroller.cs`) — Rigidbody2D-based movement, Animator-driven sprite animation, sprite flipping on direction change
- **Character asset**: "GandalfHardcore Warrior" — 8 weapon-state animation sets, idle/walk animations, sprite atlas batched

## Current State

Early prototype — one player controller script, one character sprite set, one playable scene. No test framework usage yet.

## Build & Run

Open the project in Unity 6000.0.77f1 via the Hub, or:

```bash
# Open from command line (Unity Hub required):
unityhub://6000.0.77f1/<project-path>

# Build for Windows (via Unity CLI):
<Unity-6000.0.77f1-path>/Editor/Unity.exe -quit -batchmode -buildWindowsPlayer Builds/FadingToZero.exe -projectPath "Fading To Zero"
```

## Development Workflow

- **C# scripts**: Live-reloadable in Play Mode via Unity's domain reload — no separate build step for script changes
- **Input changes**: Edit `Assets/InputSystem_Actions.inputactions` — generates C# wrapper automatically
- **2D assets**: Imported via Unity's 2D packages (SpriteShape, Animation, PSD Importer, Pixel Perfect, Tilemap Extras)
- **Version control**: `.gitignore` covers `Library/`, `Temp/`, `Obj/`, `Build/`, `UserSettings/`, IDE files, Unity metadata noise
- **IDE**: VS Code with `visualstudiotoolsforunity.vstuc` extension recommended; `.vsconfig` specifies Managed Game workload

## Coding Conventions

As observed in the existing codebase:
- `[SerializeField]` for private serialized fields with `[Header()]` grouping
- `GetComponent<>()` fallback in `Start()` if references not wired in Inspector
- `Update()` for input polling, `FixedUpdate()` for physics (`Rigidbody2D.linearVelocity`)
- PascalCase for public methods/classes, camelCase for private fields
- Unity messages (`Start`, `Update`, `FixedUpdate`) without access modifier (implicit `private`)
