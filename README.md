# UPack-Common

This is a collection of common scripts, utilities, and editor tools used frequently across projects.

## Core Scripts
- **`ObjectSort.cs`**: Handles sorting logic for Unity objects.
- **`PlatformUtil.cs`**: Utility for handling platform-specific behaviors or checks.
- **`ScreenShot.cs`**: Provides functionality to capture and save screenshots in-game.
- **`UIClickDetector.cs` / `UIClickDetector_InputSystem.cs`**: Detects UI interactions/clicks, with support for the legacy input manager and the new Input System.

## Attributes
- **`ShowIfAttribute.cs`**: Custom property attribute to conditionally show/hide fields in the Unity Inspector based on other field values.

## Constants
- **`ColorConst.cs`**: Constants for commonly used colors.
- **`ConstGenerator.cs`**: Editor tool/utility for automatically generating constant classes (e.g., tags, layers, scenes).
- **`InspectorConst.cs`**: Constants related to custom Inspector drawing.
- **`SceneConst.cs`**: Constants defining scene names or build indexes.
- **`TagConst.cs`**: Constants defining Unity Tags.

## Design Patterns
- **`Singleton.cs`**: Generic implementation of the Singleton pattern for `MonoBehaviour`.
- **ComponentStateMachine**: A component-based State Machine architecture.
  - **`ComponentStateMachine.cs`**: The core state machine manager attached to GameObjects.
  - **`ComponentState.cs`**: Base class for individual states.
  - **`Events.cs`**: Event definitions used by the state machine.
  - **`Initialization.cs` / `InitializationRequirements.cs`**: Handles initialization phases and dependencies for states.

## Helpers
Static helper classes for specific domains:
- **`ColorHelper.cs`**: Utilities for color conversions and manipulation.
- **`DebugLogger.cs` / `DebugLogger.Addition.cs`**: Advanced console logging wrappers with formatting and filtering.
- **`HapticsHelper.cs`**: Cross-platform haptic feedback triggers.
- **`LayerMaskHelper.cs`**: Simplifies bitwise operations for Unity LayerMasks.
- **`MathsHelper.cs`**: Extended mathematical functions beyond `Mathf`.
- **`NetworkHelper.cs`**: Basic connectivity and network checks.
- **`RandomHelper.cs`**: Advanced random generation (weighted randoms, shuffles).
- **`StringHelper.cs`**: String formatting and parsing utilities.
- **`TimeHelper.cs`**: Time formatting and timestamp conversions.
- **`VideoHelper.cs`**: Utilities for video player interactions or processing.
- **`YieldHelper.cs`**: Cached `WaitForSeconds` and other `IEnumerator` yield instructions to reduce garbage collection.

## Extension
Contains various C# extension methods to simplify common tasks:
- **Collections**: `ArrayExtension.cs`, `DictionaryExtension.cs`, `DictionaryListComparer.cs`, `DictionarySortExtensions.cs`, `ListExtension.cs`, `QueueExtension.cs`.
- **Unity Components & Types**: `CameraExtension.cs`, `CanvasExtension.cs`, `ColorExtension.cs`, `ComponentExtension.cs`, `GameObjectExtension.cs`, `GizmosExtension.cs`, `LineRendererExtension.cs`, `NavmeshExtension.cs`, `TransformExtension.cs`.
- **Math & Vectors**: `NumberExtension.cs`, `QuaternionExtension.cs`, `RandomExtension.cs`, `Vector2Extension.cs`, `Vector3Extension.cs`.
- **System Types**: `DateTimeExtension.cs`, `StringExtension.cs`, `CastExtension.cs`.
- **Misc**: `EditorExtension.cs`, `GestureExtension.cs`, `PlayerPrefsExtension.cs`.

## Tags
- **`DontDestroyOnLoadTag.cs`**: Component to automatically call `DontDestroyOnLoad` on the GameObject.
- **`GeometryRenderQueueOffset.cs`**: Component to adjust the material render queue offset at runtime.
- **`SpriteScreenFitter.cs`**: Scales a `SpriteRenderer` to perfectly fit or fill the screen's orthographic bounds.

## EditorTools
- **`AssetImportProcess.cs`**: Custom asset import pipeline modifications.
- **`AssetNameSwapper.cs`**: Tool to batch rename or swap asset names.
- **`OpenPersistentDataPath.cs`**: Menu item to quickly open `Application.persistentDataPath` in the file explorer.
- **`RemoveMissingScripts.cs`**: Utility to recursively find and remove missing script references from GameObjects/Prefabs.
- **`SceneSwitcher.cs`**: Provides quick-access editor menu items to switch between frequently used scenes.
- **`WindowTemplate.cs`**: A boilerplate template for creating new Unity Editor windows with Undo/Redo support.
- **`Window_AssetNameModifier.cs`**: Editor window for batch renaming and modifying asset names.
- **`Window_ComponentsRemover.cs`**: Editor window to batch remove specific components from multiple GameObjects.
- **`Window_EnumGenerator.cs`**: Editor window to generate C# Enums from structured data or lists.
- **`Window_SceneSizeAnalyzer.cs`**: Tool to analyze and report on memory/storage size usage within a scene.
- **`Window_ScriptsModifier.cs`**: Utility for batch processing or modifying scripts.
- **`WindowsBuildScript.cs`**: Automation script for building the project for the Windows platform.
- **`WindowMaterialPropsExecute.cs`**: Editor window to copy or swap properties between two materials.

## Native Handle
- **`NativeLeakDetectionBoot.cs`**: Configures Unity's native memory leak detection settings at startup.
