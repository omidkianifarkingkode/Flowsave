//using Flowsave.Configurations;
//using Flowsave.Security;
//using Flowsave.Serialization;
//using Flowsave.Shared;
//using Flowsave.StorageProviders;
//using System;
//using System.Threading.Tasks;

//namespace Flowsave
//{
//    public class FlowSaveService : IFlowSaveService
//    {
//        private readonly FlowSaveConfigRepository configRepository;
//        private readonly ISerializerFactory serializerFactory;
//        private readonly IStorageProviderFactory storageProviderFactory;
//        private readonly IEncryptionService encryptionService;
//        private readonly IHashingService hashingService;
//        private readonly IFileObfuscationService fileObfuscationService;

//        public FlowSaveService(
//            FlowSaveConfigRepository configRepository,
//            ISerializerFactory serializerFactory,
//            IStorageProviderFactory storageProviderFactory,
//            IEncryptionService encryptionService,
//            IHashingService hashingService,
//            IFileObfuscationService fileObfuscationService)
//        {
//            this.configRepository = configRepository;
//            this.serializerFactory = serializerFactory;
//            this.storageProviderFactory = storageProviderFactory;
//            this.encryptionService = encryptionService;
//            this.hashingService = hashingService;
//            this.fileObfuscationService = fileObfuscationService;
//        }

//        public async Task SaveAsync<T>(string namespaceId, T data)
//        {
//            configRepository.TryGet(namespaceId, out var config);

//            // Get serializer and storage provider via factories
//            var serializer = serializerFactory.CreateSerializer(config.Model.environments);
//            var storageProvider = storageProviderFactory.CreateStorageProvider(config.providerType);

//            var serializedData = serializer.Serialize(data);

//            // Apply security options
//            if (config.securityMode == SecurityMode.EncryptOnly || config.securityMode == SecurityMode.EncryptAndSign)
//            {
//                serializedData = encryptionService.Encrypt(serializedData, config.encryptionSettings.key, config.encryptionSettings.iv);
//            }

//            if (config.securityMode == SecurityMode.SignOnly || config.securityMode == SecurityMode.EncryptAndSign)
//            {
//                var signature = hashingService.ComputeHash(serializedData, config.encryptionSettings.key);
//                serializedData = CombineDataAndSignature(serializedData, signature);
//            }

//            // Apply filename obfuscation
//            var obfuscatedFilename = fileObfuscationService.ObfuscateFilename(namespaceId);
//            await storageProvider.SaveAsync(obfuscatedFilename, serializedData);
//        }

//        public async Task<T> LoadAsync<T>(string namespaceId)
//        {
//            var config = configRepository.GetConfig(namespaceId) ?? DefaultFlowSaveConfig.GetDefaultConfig(namespaceId);

//            // Get serializer and storage provider via factories
//            var serializer = serializerFactory.CreateSerializer(config.serializerType);
//            var storageProvider = storageProviderFactory.CreateStorageProvider(config.providerType);

//            var obfuscatedFilename = fileObfuscationService.ObfuscateFilename(namespaceId);
//            byte[] data = await storageProvider.LoadAsync(obfuscatedFilename);

//            if (config.securityMode == SecurityMode.EncryptOnly || config.securityMode == SecurityMode.EncryptAndSign)
//            {
//                data = encryptionService.Decrypt(data, config.encryptionSettings.key, config.encryptionSettings.iv);
//            }

//            if (config.securityMode == SecurityMode.SignOnly || config.securityMode == SecurityMode.EncryptAndSign)
//            {
//                // Verify the signature (omitted for simplicity)
//            }

//            return serializer.Deserialize<T>(data);
//        }

//        public async Task<bool> HasSaveAsync(string namespaceId)
//        {
//            var config = configRepository.GetConfig(namespaceId) ?? DefaultFlowSaveConfig.GetDefaultConfig(namespaceId);

//            var storageProvider = storageProviderFactory.CreateStorageProvider(config.providerType);
//            var obfuscatedFilename = fileObfuscationService.ObfuscateFilename(namespaceId);
//            return await storageProvider.ExistsAsync(obfuscatedFilename);
//        }

//        public async Task DeleteSaveAsync(string namespaceId)
//        {
//            var config = configRepository.GetConfig(namespaceId) ?? DefaultFlowSaveConfig.GetDefaultConfig(namespaceId);

//            var storageProvider = storageProviderFactory.CreateStorageProvider(config.providerType);
//            var obfuscatedFilename = fileObfuscationService.ObfuscateFilename(namespaceId);
//            await storageProvider.DeleteAsync(obfuscatedFilename);
//        }

//        private byte[] CombineDataAndSignature(byte[] data, byte[] signature)
//        {
//            var combined = new byte[data.Length + signature.Length];
//            Buffer.BlockCopy(data, 0, combined, 0, data.Length);
//            Buffer.BlockCopy(signature, 0, combined, data.Length, signature.Length);
//            return combined;
//        }
//    }
//}
