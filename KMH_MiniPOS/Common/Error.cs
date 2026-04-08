namespace Common;

public abstract class Error
{
    public string Code { get; }
    public string Message { get; }

    protected Error(string code, string message)
    {
        Code = code;
        Message = message;
    }
}

public class NotFoundError : Error
{
    public NotFoundError(string code, string message) : base(code, message)
    {
    }
}

public class ConflictError : Error
{
    public ConflictError(string code, string message) : base(code, message)
    {
    }
}

public class ValidationError : Error
{
    public ValidationError(string code, string message) : base(code, message)
    {
    }
}

public class InternalError : Error
{
    public InternalError(string code, string message) : base(code, message)
    {
    }
}

public class BadRequestError : Error
{
    public BadRequestError(string code, string message) : base(code, message)
    {
    }
}

public class UnAuthorizedError: Error
{
    public UnAuthorizedError(string code, string message) : base(code, message)
    {
    }
}