using System;
using FlowSave.Runtime.Shared;

namespace FlowSave.Runtime.Operations.Storage;

internal static class StorageErrors
{
    private const string StoragePrefix = "[FlowSave]:storage:";
    private const string DiskPrefix = "[FlowSave]:DiskStorageProvider:";
    private const string PlayerPrefsPrefix = "[FlowSave]:PlayerPrefsStorageProvider:";

    public static readonly Result<byte[]> KeyNullBytes = Result<byte[]>.Failure($"{StoragePrefix}Key is null.");
    public static readonly Result<bool> KeyNullBool = Result<bool>.Failure($"{StoragePrefix}Key is null.");
    public static readonly Result KeyNullResult = Result.Failure($"{StoragePrefix}Key is null.");
    public static readonly Result<byte[]> KeyRequired = Result<byte[]>.Failure($"{StoragePrefix}Key is required.");
    public static readonly Result<bool> KeyRequiredBool = Result<bool>.Failure($"{StoragePrefix}Key is required.");
    public static readonly Result KeyRequiredResult = Result.Failure($"{StoragePrefix}Key is required.");
    public static readonly Result DataNull = Result.Failure($"{StoragePrefix}Data is null.");

    public static Result<byte[]> KeyNotFound(string key, string? prefix = null)
        => Result<byte[]>.Failure($"{prefix ?? StoragePrefix}Key not found: {key}");

    public static Result<byte[]> LoadFailed(string message, string? prefix = null)
        => Result<byte[]>.Failure($"{prefix ?? StoragePrefix}Load failed: {message}");

    public static Result SaveFailed(string message, string? prefix = null)
        => Result.Failure($"{prefix ?? StoragePrefix}Save failed: {message}");

    public static Result<bool> ExistsFailed(string message, string? prefix = null)
        => Result<bool>.Failure($"{prefix ?? StoragePrefix}Exists check failed: {message}");

    public static Result DeleteFailed(string message, string? prefix = null)
        => Result.Failure($"{prefix ?? StoragePrefix}Delete failed: {message}");

    public static Result<byte[]> FileTooLarge(long length)
        => Result<byte[]>.Failure($"{DiskPrefix}File too large. Size: {length} bytes.");

    public static Result<byte[]> LegacyDataRecovered(byte[] data)
        => Result<byte[]>.Success(data);

    public static Result<byte[]> CorruptEntry(string key)
        => Result<byte[]>.Failure($"{PlayerPrefsPrefix}Corrupt PlayerPrefs entry for key: {key}");

    public static string DiskModule => DiskPrefix;
    public static string PlayerPrefsModule => PlayerPrefsPrefix;
}
