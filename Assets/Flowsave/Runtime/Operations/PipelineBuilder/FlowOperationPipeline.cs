using System;
using System.Threading.Tasks;

namespace FlowSave.Operations.Builder
{
    /// <summary>
    /// Unified read/write operation pipeline for FlowSave.
    /// 
    /// Write direction:
    ///     T -> serialize -> compress -> encrypt -> sign -> storage
    ///
    /// Read direction:
    ///     storage -> verify -> decrypt -> decompress -> deserialize -> T
    /// </summary>
    public sealed partial class FlowOperationPipeline<T>
    {
        private readonly Func<T, Task<Result>> _writePath;
        private readonly Func<Task<Result<T>>> _readPath;

        private FlowOperationPipeline(
            Func<T, Task<Result>> writePath,
            Func<Task<Result<T>>> readPath)
        {
            _writePath = writePath;
            _readPath = readPath;
        }

        /// <summary>
        /// Executes the WRITE pipeline (fails if this pipeline was created as READ).
        /// </summary>
        public Task<Result> ExecuteWriteAsync(T value)
        {
            if (_writePath == null)
                return Task.FromResult(Result.Failure("This FlowOperationPipeline was not created as a write pipeline."));
            return _writePath(value);
        }

        /// <summary>
        /// Executes the READ pipeline (fails if this pipeline was created as WRITE).
        /// </summary>
        public Task<Result<T>> ExecuteReadAsync()
        {
            if (_readPath == null)
                return Task.FromResult(Result<T>.Failure("This FlowOperationPipeline was not created as a read pipeline."));
            return _readPath();
        }
    }
}
