namespace POS.Shared.Models
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public T? Value { get; set; }
        public string? Error { get; set; }
        public bool IsFailure => !IsSuccess;

        public Result() { }

        public Result(bool isSuccess, T? value, string? error)
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
        public Result() { }
        public Result(bool isSuccess, string? error) : base(isSuccess, null, error) { }

        public static Result Success() => new(true, null);
        public static new Result Failure(string error) => new(false, error);
    }
}
