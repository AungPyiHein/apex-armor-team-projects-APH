namespace POS.Shared.Models
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }
        public bool IsFailure => !IsSuccess;

        protected Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error) => new(false, default, error);
    }

    public class Result : Result<object>
    {
        protected Result(bool isSuccess, string? error) : base(isSuccess, null, error) { }

        public static Result Success() => new(true, null);
        public static new Result Failure(string error) => new(false, error);
    }
}
