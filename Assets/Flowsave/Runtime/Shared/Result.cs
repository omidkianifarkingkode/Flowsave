using System.Threading.Tasks;

namespace FlowSave
{
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        private Result(bool ok, string error)
        {
            IsSuccess = ok;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Failure(string error) => new Result(false, error);
    }


    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Error { get; }

        private Result(bool ok, T value, string error)
        {
            IsSuccess = ok;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null);
        public static Result<T> Failure(string error) => new Result<T>(false, default, error);

        public static implicit operator Result<T>(Result result)
        {
            if (result.IsSuccess)
                return Failure("Cannot convert success Result to Result<T>.");

            return Failure(result.Error);
        }
    }

    public static class ResultExtensions
    {
        public static Task<Result<byte[]>> ToTask(this Result<byte[]> result)
            => Task.FromResult(result);

        public static Task<Result> ToTask(this Result result)
            => Task.FromResult(result);

        public static Task<Result<T>> ToTask<T>(this Result<T> result) 
            => Task.FromResult(result);
    }

}
