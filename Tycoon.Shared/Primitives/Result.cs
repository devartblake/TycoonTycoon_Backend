namespace Tycoon.Shared.Primitives
{
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        private Result(bool ok, string? err) { IsSuccess = ok; Error = err; }
        public static Result Ok() => new(true, null);
        public static Result Fail(string error) => new(false, error);
    }

    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public T? Value { get; }
        private Result(bool ok, T? val, string? err) { IsSuccess = ok; Value = val; Error = err; }
        public static Result<T> Ok(T value) => new(true, value, null);
        public static Result<T> Fail(string error) => new(false, default, error);
    }
}
