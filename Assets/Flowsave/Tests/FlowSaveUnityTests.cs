using Flowsave;
using NUnit.Framework;
using System.Threading.Tasks;
using UnityEngine;

public class FlowSaveUnityTests
{
    private IFlowSave _flow;
    private const string NS = "player.data";

    [System.Serializable]
    public class PlayerData
    {
        public int level;
        public string name;
    }

    [SetUp]
    public void Setup()
    {
        _flow = new FlowSave();
    }

    [Test]
    public async void SaveAndLoadObject_Works()
    {
        var data = new PlayerData { level = 5, name = "Omid" };

        var save = await _flow.SaveAsync(NS, data);

        if (!save.IsSuccess)
            Debug.Log(save.Error);

        Assert.IsTrue(save.IsSuccess);

        var load = await _flow.LoadAsync<PlayerData>(NS);

        if (!load.IsSuccess)
            Debug.Log(load.Error);

        Assert.IsTrue(load.IsSuccess);
        Assert.AreEqual(5, load.Value.level);
        Assert.AreEqual("Omid", load.Value.name);
    }

    [Test]
    public async void HasSave_ReturnsTrueAfterSave()
    {
        await _flow.SaveAsync(NS, new PlayerData { level = 1, name = "A" });

        var has = await _flow.HasSaveAsync(NS);

        Assert.IsTrue(has.IsSuccess);
        Assert.IsTrue(has.Value);
    }

    [Test]
    public async void DeleteSave_RemovesEntry()
    {
        await _flow.SaveAsync(NS, new PlayerData { level = 1, name = "A" });

        await _flow.DeleteSaveAsync(NS);

        var has = await _flow.HasSaveAsync(NS);

        Assert.IsTrue(has.IsSuccess);
        Assert.IsFalse(has.Value);
    }

    [Test]
    public async void RawBytes_SaveAndLoad()
    {
        byte[] bytes = { 10, 20, 30 };

        await _flow.SaveRawBytesAsync(NS, bytes);

        var result = await _flow.LoadRawBytesAsync(NS);

        Assert.IsTrue(result.IsSuccess);
        CollectionAssert.AreEqual(bytes, result.Value);
    }

    [Test]
    public async void RawString_SaveAndLoad()
    {
        var text = "Flowsave test!";

        await _flow.SaveRawStringAsync(NS, text);

        var load = await _flow.LoadRawStringAsync(NS);

        Assert.IsTrue(load.IsSuccess);
        Assert.AreEqual(text, load.Value);
    }
}


