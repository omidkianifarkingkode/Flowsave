# FlowSave
> **A modular, deterministic, versioned save-system framework for Unity.**

![Unity](https://img.shields.io/badge/Unity-2021%2B-black?logo=unity)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![Status](https://img.shields.io/badge/State-Production%20Ready-blue)
![Size](https://img.shields.io/badge/Size-Lightweight-%23ffaa00)

---

## ✨ What is FlowSave?

FlowSave is a lightweight, extensible save framework for Unity focused on:

- Deterministic & atomic file writes  
- Encryption (AES) + HMAC signing  
- Versioned & migratable save formats  
- Pluggable storage providers (file, cloud, custom)  
- Clean async API  
- Minimal setup and boilerplate  

---

## 📦 Installation

### **UPM (Git URL)**

https://github.com/your-org/flowsave.git#upm

markdown
Copy code

### **Optional Scripting Defines**

| Feature                | Define             | Dependency       |
|------------------------|--------------------|------------------|
| UniTask async wrappers | `FLOWSAVE_UNITASK` | UniTask          |
| LZ4 compression        | `FLOWSAVE_LZ4`     | K4os.LZ4         |

---

## ⚙️ Configuration Overview

Create a **`FlowSaveConfiguration`** asset and assign it to a **`FlowSaveBootstrapper`** in your startup scene.

### **Default Options**
Global defaults for:
- Serializer  
- Storage provider  
- Compression  
- KeyStore  
- Logging  

### **Environments**
Override configuration per environment:
- `Editor`
- `Development`
- `Release`

### **KeyStore**
Supports:
- AES keys  
- HMAC keys  
- Password-derived keys (PBKDF2)  
- Static Base64 keys  

### **Namespaces**
Logical save groups, e.g.:

player
settings
runtime-cache
achievements

kotlin
Copy code

Each namespace may override:
- Serializer  
- Provider  
- Encryption profile  
- Compression  
- Migrators  

### **Logger**

FlowSave includes:
- Unity-style logger  
- Prefix + color support  
- Filterable log levels  

You can inject your own logger.

### **Dependency Resolver**

You may override any internal service using `IFlowSaveResolver`:

```csharp
public class MyResolver : IFlowSaveResolver
{
    public object Resolve(Type t)
    {
        if (t == typeof(ILogger))
            return new MyLogger();

        return null;
    }
}
```
🧩 API Usage
1. Access
csharp
Copy code
var fs = FlowSave.Instance;
2. Saving
```csharp
[Serializable]
public class PlayerData
{
    public int level;
    public int coins;
}

var player = new PlayerData { level = 5, coins = 120 };

var result = await FlowSave.Instance.SaveAsync("player", player);
if (!result)
    Debug.LogError(result.Error);
```
3. Loading
```csharp
var load = await FlowSave.Instance.LoadAsync<PlayerData>("player");

if (load.Ok)
{
    Debug.Log("Coins: " + load.Value.coins);
}
```
4. Raw Bytes
```csharp
var bytes = Encoding.UTF8.GetBytes("cached-data");
await FlowSave.Instance.SaveRawBytesAsync("runtime-cache", bytes);
```
6. UniTask API (optional)
```csharp
var res = await FlowSave.Instance.SaveUniAsync("player", player);
```
7. Custom Logger
8. Bootstrapper Setup
Add to your scene:

GameObject → FlowSave → Bootstrapper
Assign your configuration asset.

🚧 Future Features
Cloud provider

Binary serializers (MessagePack, Protobuf)

Save profiling window

ECS integration

Transaction/delta-based saving

📄 License
MIT License — free for commercial and open-source usage.