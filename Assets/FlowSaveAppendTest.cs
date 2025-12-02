using System;
using System.Threading.Tasks;
using UnityEngine;
using FlowSave;

public class FlowSaveScenarioTest : MonoBehaviour
{
    [Header("Namespace IDs")]
    [SerializeField] private string profileNamespace = "playerprofile1";   // snapshot
    [SerializeField] private string transactionsNamespace = "transactions"; // append mode

    [Header("Options")]
    [SerializeField] private bool runBasicOnStart = false; // optional: run basic scenario 1 + append once

    private IFlowSave _service;

    // ================================
    // Test Data Models
    // ================================
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerId;
        public int Level;
        public int Coins;
    }

    [Serializable]
    public class Transaction
    {
        public string Id;
        public int Delta;
        public string Reason;
        public string Currency;
        public DateTime UtcTime;
    }

    // ================================
    // Init
    // ================================
  

    private async void Start()
    {
        _service = FlowSaveService.Instance;

        if (runBasicOnStart)
        {
            await Snapshot_Scenario1_SaveAndLoad_SameConfig();
            await Append_Scenario4_SaveAndLoad_SameConfig();
        }
    }

    // =====================================================================
    // SNAPSHOT MODE SCENARIOS (playerprofile)
    // =====================================================================

    // 1) Save & load profile with same config (baseline)
    [ContextMenu("Snapshot Scenario 1 - Save & Load (same config)")]
    private async void SnapshotScenario1_ContextMenu() =>
        await Snapshot_Scenario1_SaveAndLoad_SameConfig();

    private async Task Snapshot_Scenario1_SaveAndLoad_SameConfig()
    {
        Debug.Log("=== SNAPSHOT Scenario 1: Save & Load with SAME config ===");
        EnsureService();

        // create sample profile
        var profile = new PlayerProfile
        {
            PlayerId = "player-001",
            Level = UnityEngine.Random.Range(1, 20),
            Coins = UnityEngine.Random.Range(100, 1000)
        };

        // Save
        var save = await _service.SaveAsync(profileNamespace, profile);
        if (!save.IsSuccess)
        {
            Debug.LogError($"[Scenario 1] Save FAILED: {save.Error}");
            return;
        }

        Debug.Log($"[Scenario 1] Saved PROFILE ({profileNamespace}):\n{JsonUtility.ToJson(profile, true)}");

        // Load
        var load = await _service.LoadAsync<PlayerProfile>(profileNamespace);
        if (!load.IsSuccess)
        {
            Debug.LogError($"[Scenario 1] Load FAILED: {load.Error}");
            return;
        }

        Debug.Log($"[Scenario 1] Loaded PROFILE ({profileNamespace}):\n{JsonUtility.ToJson(load.Value, true)}");

        Debug.Log("=== SNAPSHOT Scenario 1 DONE ===");
    }

    // 2) Load profile only – should work even if env config changed in editor
    //    Use this AFTER you have an existing file and AFTER changing env config.
    [ContextMenu("Snapshot Scenario 2 - Load ONLY (config may be changed)")]
    private async void SnapshotScenario2_ContextMenu() =>
        await Snapshot_Scenario2_LoadOnly_ConfigIndependent();

    private async Task Snapshot_Scenario2_LoadOnly_ConfigIndependent()
    {
        Debug.Log("=== SNAPSHOT Scenario 2: LOAD ONLY (config may be changed) ===");
        EnsureService();

        // Assumption:
        // - playerprofile file already exists from a previous save (Scenario 1 or 3)
        // - You may have changed compression/encryption/sign/serializer in editor

        var load = await _service.LoadAsync<PlayerProfile>(profileNamespace);
        if (!load.IsSuccess)
        {
            Debug.LogError($"[Scenario 2] Load FAILED: {load.Error}");
            return;
        }

        Debug.Log($"[Scenario 2] Loaded PROFILE ({profileNamespace}) with CURRENT env config:\n{JsonUtility.ToJson(load.Value, true)}");
        Debug.Log("=== SNAPSHOT Scenario 2 DONE ===");
    }

    // 3) Save & load multiple times while you change config between runs.
    //
    // How to use:
    //  - Run Scenario 1 once (initial file).
    //  - Change env config in editor (e.g. add sign, change key, change serializer).
    //  - Run Scenario 3.
    //  - Repeat: tweak config again → run Scenario 3 again.
    //
    // Each run will:
    //  - Save a new profile
    //  - Immediately Load and log it
    [ContextMenu("Snapshot Scenario 3 - Save & Load (AFTER config change)")]
    private async void SnapshotScenario3_ContextMenu() =>
        await Snapshot_Scenario3_SaveAndLoad_AfterConfigChange();

    private async Task Snapshot_Scenario3_SaveAndLoad_AfterConfigChange()
    {
        Debug.Log("=== SNAPSHOT Scenario 3: Save & Load (AFTER config change) ===");
        EnsureService();

        var profile = new PlayerProfile
        {
            PlayerId = "player-001",
            Level = UnityEngine.Random.Range(1, 50),
            Coins = UnityEngine.Random.Range(500, 5000)
        };

        // Save with whatever CURRENT env config is
        var save = await _service.SaveAsync(profileNamespace, profile);
        if (!save.IsSuccess)
        {
            Debug.LogError($"[Scenario 3] Save FAILED: {save.Error}");
            return;
        }

        Debug.Log($"[Scenario 3] Saved PROFILE ({profileNamespace}) with CURRENT env config:\n{JsonUtility.ToJson(profile, true)}");

        // Load back under same (possibly new) env config
        var load = await _service.LoadAsync<PlayerProfile>(profileNamespace);
        if (!load.IsSuccess)
        {
            Debug.LogError($"[Scenario 3] Load FAILED: {load.Error}");
            return;
        }

        Debug.Log($"[Scenario 3] Loaded PROFILE ({profileNamespace}) with CURRENT env config:\n{JsonUtility.ToJson(load.Value, true)}");

        Debug.Log("=== SNAPSHOT Scenario 3 DONE ===");
    }

    // =====================================================================
    // APPEND MODE SCENARIOS (transactions)
    // =====================================================================

    // 4) Append mode version of the same idea:
    //    - Save several transactions (append)
    //    - Load last transaction
    //    - Load all transactions
    //
    // You can run this:
    //   - once with initial config
    //   - again after config changes (encryption/sign/compression/serializer)
    [ContextMenu("Append Scenario 4 - Append & Load (works across config changes)")]
    private async void AppendScenario4_ContextMenu() =>
        await Append_Scenario4_SaveAndLoad_SameConfig();

    private async Task Append_Scenario4_SaveAndLoad_SameConfig()
    {
        Debug.Log("=== APPEND Scenario 4: Append & Load (config may change between runs) ===");
        EnsureService();

        // Append 3 new transactions using current env config
        for (int i = 0; i < 3; i++)
        {
            var tx = new Transaction
            {
                Id = Guid.NewGuid().ToString("N"),
                Delta = UnityEngine.Random.Range(-50, 150),
                Reason = $"Scenario 4 Tx #{i + 1}",
                Currency = "GOLD",
                UtcTime = DateTime.UtcNow
            };

            var save = await _service.SaveAsync(transactionsNamespace, tx);
            if (!save.IsSuccess)
            {
                Debug.LogError($"[Scenario 4] Append FAILED: {save.Error}");
                return;
            }

            Debug.Log($"[Scenario 4] Appended TX to '{transactionsNamespace}':\n{JsonUtility.ToJson(tx, true)}");
        }

        // Load last entry (normal LoadAsync on append namespace returns last record)
        var loadLast = await _service.LoadAsync<Transaction>(transactionsNamespace);
        if (!loadLast.IsSuccess)
        {
            Debug.LogError($"[Scenario 4] Load LAST tx FAILED: {loadLast.Error}");
        }
        else
        {
            Debug.Log($"[Scenario 4] Loaded LAST TX from '{transactionsNamespace}':\n{JsonUtility.ToJson(loadLast.Value, true)}");
        }

        // Load all entries
        var loadAll = await _service.LoadAllAsync<Transaction>(transactionsNamespace);
        if (!loadAll.IsSuccess)
        {
            Debug.LogError($"[Scenario 4] Load ALL tx FAILED: {loadAll.Error}");
            return;
        }

        Debug.Log($"[Scenario 4] Loaded ALL {loadAll.Value.Length} TX entries from '{transactionsNamespace}':");
        for (int i = 0; i < loadAll.Value.Length; i++)
        {
            Debug.Log($"  TX[{i}]: {JsonUtility.ToJson(loadAll.Value[i], true)}");
        }

        Debug.Log("=== APPEND Scenario 4 DONE ===");
    }

    [ContextMenu("Append Scenario 5 - Load (works across config changes)")]
    private async void AppendScenario5_ContextMenu() =>
        await Append_Scenario5_Load_SameConfig();

    private async Task Append_Scenario5_Load_SameConfig()
    {
        Debug.Log("=== APPEND Scenario 5: Load (config may change between runs) ===");
        EnsureService();

        // Load last entry (normal LoadAsync on append namespace returns last record)
        var loadLast = await _service.LoadAsync<Transaction>(transactionsNamespace);
        if (!loadLast.IsSuccess)
        {
            Debug.LogError($"[Scenario 5] Load LAST tx FAILED: {loadLast.Error}");
        }
        else
        {
            Debug.Log($"[Scenario 5] Loaded LAST TX from '{transactionsNamespace}':\n{JsonUtility.ToJson(loadLast.Value, true)}");
        }

        // Load all entries
        var loadAll = await _service.LoadAllAsync<Transaction>(transactionsNamespace);
        if (!loadAll.IsSuccess)
        {
            Debug.LogError($"[Scenario 5] Load ALL tx FAILED: {loadAll.Error}");
            return;
        }

        Debug.Log($"[Scenario 5] Loaded ALL {loadAll.Value.Length} TX entries from '{transactionsNamespace}':");
        for (int i = 0; i < loadAll.Value.Length; i++)
        {
            Debug.Log($"  TX[{i}]: {JsonUtility.ToJson(loadAll.Value[i], true)}");
        }

        Debug.Log("=== APPEND Scenario 5 DONE ===");
    }

    // ================================
    // Helpers
    // ================================
    private void EnsureService()
    {
        if (_service == null)
        {
            Debug.LogWarning("FlowSave service was null, creating a new FlowSaveService.");
            _service = new FlowSaveService();
        }
    }
}
