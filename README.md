# FlowSave

FlowSave is a modular data persistence framework for Unity projects, powering the Flowcast ecosystem with deterministic, versioned saves. The package offers a small bootstrapper, extensible provider and serializer abstractions, and sensible defaults so you can add robust saves to new or existing projects with minimal boilerplate.

## Package layout

```
Assets/Flowcast.FlowSave/
  Runtime/
    Core/              # Context, manager, and core interfaces
    Providers/File/    # File-system based provider implementation
    Serialization/Json/# JsonUtility-backed serializer
```

The runtime ships with a JSON file provider built on Unity's `JsonUtility` and can be extended with custom providers, serializers, and migrators to fit your pipeline.

## Intro: package usage

FlowSave is centered around the `IFlowSave` interface, which orchestrates serialization, versioning, and storage through pluggable providers. You obtain an instance from `FlowSave.Instance`, configure it during startup, and then read/write your save models using strongly typed methods.

### Key capabilities

- **Versioned saves** via migrators to evolve data formats safely.
- **Provider abstraction** for file-system, cloud, or custom storage targets.
- **Serializer abstraction** to plug in JSON, protobuf, MessagePack, or other codecs.
- **Deterministic persistence** with optional compression/encryption depending on provider and serializer choices.

## How to install (add to project)

1. Copy the `Assets/Flowcast.FlowSave` folder into your Unity project (or add it as a Git submodule under `Packages` if you prefer UPM-style consumption).
2. Ensure assembly definition references are updated if you place the folder under a custom path.
3. Open Unity to allow the editor to import the scripts and generate the FlowSave assemblies.

## How to set up

FlowSave expects two pieces of setup: editor configuration and a runtime bootstrapper that wires up providers and serializers.

### Editor configuration

1. In the Unity Project window, select **Edit → Project Settings → Player** and make sure the scripting runtime is set to `.NET 4.x` equivalent or later.
2. If you plan to use optional dependencies (see below), add the corresponding packages (e.g., **MessagePack for C#**, **Google.Protobuf**, **Cysharp UniTask**) via the Unity Package Manager or by placing them in your `Packages/` folder.
3. Regenerate assembly definition references if you add custom serializers or providers in other assemblies.

### Bootstrapper (environment, namespaces, and operations)

Create a startup MonoBehaviour that configures FlowSave once during application initialization:

```csharp
using Flowcast.FlowSave.Core;
using Flowcast.FlowSave.Providers.File;
using Flowcast.FlowSave.Serialization.Json;

public class FlowSaveBootstrapper : MonoBehaviour
{
    void Awake()
    {
        var provider = new JsonFileSaveProvider(
            Application.persistentDataPath,
            prettyPrint: false);

        FlowSave.Instance.Configure(
            saveProvider: provider,
            serializer: new JsonSaveSerializer(),
            migrators: new List<ISaveMigrator>()); // add migrators as needed
    }
}
```

**Namespaces & operations**

- Core contracts live under `Flowcast.FlowSave.Core` (e.g., `IFlowSave`, `ISaveProvider`, `ISaveSerializer`, `ISaveMigrator`).
- Built-in file provider lives under `Flowcast.FlowSave.Providers.File`.
- JSON serializer lives under `Flowcast.FlowSave.Serialization.Json`.
- Typical operations include `LoadAsync<T>()`, `SaveAsync<T>()`, `DeleteAsync()` on `IFlowSave` or the singleton `FlowSave.Instance`.

### Custom logger setup

FlowSave surfaces logging hooks so you can route messages to your preferred logger (e.g., `UnityEngine.Debug`, Serilog, or a bespoke solution). Implement `IFlowSaveLogger` and register it during configuration:

```csharp
using Flowcast.FlowSave.Core;

public class UnityDebugLogger : IFlowSaveLogger
{
    public void LogInfo(string message) => Debug.Log(message);
    public void LogWarning(string message) => Debug.LogWarning(message);
    public void LogError(string message) => Debug.LogError(message);
}

// During bootstrap
FlowSave.Instance.Configure(
    saveProvider: provider,
    serializer: new JsonSaveSerializer(),
    migrators: new List<ISaveMigrator>(),
    logger: new UnityDebugLogger());
```

## How to use (`FlowSave.Instance` and `IFlowSave` methods)

Once configured, use the singleton or inject `IFlowSave` into systems that need persistence.

```csharp
// Saving a model
await FlowSave.Instance.SaveAsync("player-progress", playerData);

// Loading a model
var loadResult = await FlowSave.Instance.LoadAsync<PlayerData>("player-progress");
if (loadResult.IsSuccess)
{
    playerData = loadResult.Value;
}

// Deleting a save slot
await FlowSave.Instance.DeleteAsync("player-progress");
```

Additional helpers like `ExistsAsync`, `ListSavesAsync`, and `MigrateAsync` are available depending on the provider/serializer combination you register.

## Optional dependencies

FlowSave is serialization- and provider-agnostic. To take advantage of richer codecs or async patterns, add the following packages and plug in the matching implementations:

- **Protobuf** (`Google.Protobuf`): implement `ISaveSerializer` using protobuf messages for compact, deterministic binaries.
- **MessagePack** (`MessagePack-CSharp`): create a `MessagePackSaveSerializer` with LZ4 compression if desired.
- **UniTask** (`Cysharp.UniTask`): expose async methods returning `UniTask` instead of `Task` for allocation-friendly async in Unity.
- **LZ4** (`K4os.Compression.LZ4` or MessagePack built-in): wrap file payloads with compression before writing through your provider.

To resolve packages in the editor:

1. Open **Window → Package Manager**.
2. Select **Add package from Git URL...** or **Add package from disk...** and supply the relevant package source.
3. If using assembly definitions, add references to the new packages so FlowSave runtime assemblies can see the serializer/provider types.
4. Reopen the Unity editor or trigger a recompile to ensure the types are discoverable by your bootstrapper.

## License

This project is licensed under the terms of the MIT License. See [LICENSE](LICENSE) for details.
