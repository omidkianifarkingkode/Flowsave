# FlowSave

FlowSave is a modular data persistence framework for Unity projects, designed to power the Flowcast ecosystem.
This repository currently contains the runtime core, provider abstractions, and a baseline JSON file provider
implemented using Unity's `JsonUtility`.

## Package layout

```
Assets/Flowcast.FlowSave/
  Runtime/
    Core/              # Context, manager, and core interfaces
    Providers/File/    # File-system based provider implementation
    Serialization/Json/# JsonUtility-backed serializer
```

The initial implementation focuses on deterministic, versioned persistence with optional encryption and
extensibility points for custom serializers, providers, and migrators.
