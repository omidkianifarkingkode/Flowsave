using FlowSave.Compression;
using FlowSave.Configurations;
using FlowSave.Encryption;
using FlowSave.Operations;
using FlowSave.Serialization;
using FlowSave.Signing;
using FlowSave.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CompressionType = FlowSave.Compression.CompressionType;

namespace FlowSave.Tests
{
    public class FlowSaveUnityTests
    {
        private IFlowSave _flow;
        private const string NS = "player.data";
        private EnvironmentConfiguration _baseEnvironment;

        private static readonly byte[] EncryptionKey = Enumerable.Repeat((byte)0x23, 32).ToArray();
        private static readonly byte[] SigningKey = Enumerable.Repeat((byte)0x42, 32).ToArray();

        [System.Serializable]
        public class PlayerData
        {
            public int level;
            public string name;
        }

        [SetUp]
        public void Setup()
        {
            FlowSaveConfiguration.ModeResolver = () => AppMode.Editor;
            KeyResolver.Initialize("flowsave-tests");
            PlayerPrefs.DeleteAll();

            _baseEnvironment = CreateBaseEnvironment(StorageType.FileSystem);
            _flow = CreateFlowSave(_baseEnvironment, NS);
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

        [Test]
        public async void SaveAndLoad_UsingPlayerPrefsStorage_Works()
        {
            var env = EnvironmentConfiguration.Clone(_baseEnvironment);
            env.StorageOptions = StorageOptions.Clone(_baseEnvironment.StorageOptions);
            env.StorageOptions.UseDefault = false;
            env.StorageOptions.StorageType = StorageType.PlayerPrefs;
            env.StorageOptions.PlayerPrefsStorage = new PlayerPrefsStorageOptions
            {
                Prefix = "fs-tests:",
                AutoSave = true,
                ChunkChars = 4096
            };

            var flow = CreateFlowSave(env, "playerprefs.ns");

            var data = new PlayerData { level = 7, name = "Prefs" };
            var save = await flow.SaveAsync("playerprefs.ns", data);
            Assert.IsTrue(save.IsSuccess, save.Error);

            var load = await flow.LoadAsync<PlayerData>("playerprefs.ns");
            Assert.IsTrue(load.IsSuccess, load.Error);
            Assert.AreEqual(data.level, load.Value.level);
            Assert.AreEqual(data.name, load.Value.name);
        }

        [Test]
        public async void SaveAndLoad_WithCompression_Works()
        {
            var env = CreateOperationsEnvironment(OperationMode.Compression, static opts =>
            {
                opts.CompressionOptions.UseDefault = false;
                opts.CompressionOptions.CompressionType = CompressionType.Deflate;
            });

            var flow = CreateFlowSave(env, "compression.ns");
            var data = new PlayerData { level = 10, name = "Compression" };

            var save = await flow.SaveAsync("compression.ns", data);
            Assert.IsTrue(save.IsSuccess, save.Error);

            var load = await flow.LoadAsync<PlayerData>("compression.ns");
            Assert.IsTrue(load.IsSuccess, load.Error);
            Assert.AreEqual(data.level, load.Value.level);
            Assert.AreEqual(data.name, load.Value.name);
        }

        [Test]
        public async void SaveAndLoad_WithEncryption_Works()
        {
            var env = CreateOperationsEnvironment(OperationMode.Encrypt, opts =>
            {
                opts.EncryptionOptions.UseDefault = false;
                opts.EncryptionOptions.EncryptionType = EncryptionType.Aes256Cbc;
                opts.EncryptionOptions.Aes256 = new AesOptions
                {
                    KeyBits = KeyBits._256,
                    DeriveKey = false,
                    KeyB64 = Convert.ToBase64String(EncryptionKey)
                };
            });

            var flow = CreateFlowSave(env, "encryption.ns");
            var data = new PlayerData { level = 15, name = "Encryption" };

            var save = await flow.SaveAsync("encryption.ns", data);
            Assert.IsTrue(save.IsSuccess, save.Error);

            var load = await flow.LoadAsync<PlayerData>("encryption.ns");
            Assert.IsTrue(load.IsSuccess, load.Error);
            Assert.AreEqual(data.level, load.Value.level);
            Assert.AreEqual(data.name, load.Value.name);
        }

        [Test]
        public async void SaveAndLoad_WithSigning_Works()
        {
            var env = CreateOperationsEnvironment(OperationMode.Sign, opts =>
            {
                opts.SigningOptions.UseDefault = false;
                opts.SigningOptions.SigningType = SigningType.Hmac;
                opts.SigningOptions.Hmac = new HmacOptions
                {
                    DeriveKey = false,
                    KeyB64 = Convert.ToBase64String(SigningKey),
                    KeyId = "tests",
                    TruncateTo = HmacTruncate._32
                };
            });

            var flow = CreateFlowSave(env, "signing.ns");
            var data = new PlayerData { level = 20, name = "Signing" };

            var save = await flow.SaveAsync("signing.ns", data);
            Assert.IsTrue(save.IsSuccess, save.Error);

            var load = await flow.LoadAsync<PlayerData>("signing.ns");
            Assert.IsTrue(load.IsSuccess, load.Error);
            Assert.AreEqual(data.level, load.Value.level);
            Assert.AreEqual(data.name, load.Value.name);
        }

        [Test]
        public async void SaveAndLoad_WithAllOperations_Works()
        {
            var env = CreateOperationsEnvironment(new[]
            {
            OperationMode.Compression,
            OperationMode.Encrypt,
            OperationMode.Sign
        }, opts =>
        {
            opts.CompressionOptions.UseDefault = false;
            opts.CompressionOptions.CompressionType = CompressionType.Deflate;

            opts.EncryptionOptions.UseDefault = false;
            opts.EncryptionOptions.EncryptionType = EncryptionType.Aes128Cbc;
            opts.EncryptionOptions.Aes128 = new AesOptions
            {
                KeyBits = KeyBits._128,
                DeriveKey = false,
                KeyB64 = Convert.ToBase64String(EncryptionKey.Take(16).ToArray())
            };

            opts.SigningOptions.UseDefault = false;
            opts.SigningOptions.SigningType = SigningType.Hmac;
            opts.SigningOptions.Hmac = new HmacOptions
            {
                DeriveKey = false,
                KeyB64 = Convert.ToBase64String(SigningKey),
                KeyId = "tests-all",
                TruncateTo = HmacTruncate._16
            };
        });

            var flow = CreateFlowSave(env, "allops.ns");
            var data = new PlayerData { level = 25, name = "Everything" };

            var save = await flow.SaveAsync("allops.ns", data);
            Assert.IsTrue(save.IsSuccess, save.Error);

            var load = await flow.LoadAsync<PlayerData>("allops.ns");
            Assert.IsTrue(load.IsSuccess, load.Error);
            Assert.AreEqual(data.level, load.Value.level);
            Assert.AreEqual(data.name, load.Value.name);
        }

        private static EnvironmentConfiguration CreateBaseEnvironment(StorageType storageType)
        {
            return new EnvironmentConfiguration
            {
                AppMode = AppMode.Editor,
                StorageOptions = new StorageOptions
                {
                    UseDefault = false,
                    StorageType = storageType,
                    DiskStorage = new DiskStorageOptions
                    {
                        PathRoot = StoragePathRoot.ProjectRoot,
                        PathTemplate = "flowsave-tests/{NAMESPACE}.bin",
                        KeepBackup = false,
                        MaxBackup = 1
                    },
                    PlayerPrefsStorage = new PlayerPrefsStorageOptions
                    {
                        Prefix = "fs-tests:",
                        AutoSave = true,
                        ChunkChars = 4096
                    },
                    ObfuscateFileName = false
                },
                Operations = new List<OperationMode>(),
                CompressionOptions = new CompressionOptions
                {
                    UseDefault = false,
                    CompressionType = CompressionType.None
                },
                SerializationOptions = new SerializationOptions
                {
                    UseDefault = false,
                    SerializationType = SerializationType.Json
                },
                EncryptionOptions = new EncryptionOptions
                {
                    UseDefault = false,
                    EncryptionType = EncryptionType.None
                },
                SigningOptions = new SigningOptions
                {
                    UseDefault = false,
                    SigningType = SigningType.None
                }
            };
        }

        private FlowSaveService CreateFlowSave(EnvironmentConfiguration env, string namespaceId)
        {
            var config = ScriptableObject.CreateInstance<FlowSaveConfiguration>();
            config.Namespaces = new List<NamespaceConfiguration>
            {
                new() {
                    NamespaceId = namespaceId,
                    Environments = new List<EnvironmentConfiguration> { env }
                }
            };

            return new FlowSaveService(config);
        }

        private EnvironmentConfiguration CreateOperationsEnvironment(OperationMode mode, Action<EnvironmentConfiguration> configure)
        {
            return CreateOperationsEnvironment(new[] { mode }, configure);
        }

        private EnvironmentConfiguration CreateOperationsEnvironment(IEnumerable<OperationMode> modes, Action<EnvironmentConfiguration> configure)
        {
            var envClone = EnvironmentConfiguration.Clone(_baseEnvironment);
            envClone.Operations = modes.ToList();
            configure(envClone);
            return envClone;
        }
    }
}
