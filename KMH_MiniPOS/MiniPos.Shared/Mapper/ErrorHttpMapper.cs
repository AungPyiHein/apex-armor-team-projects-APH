using Common;
using Microsoft.AspNetCore.Http;

namespace Mapper;

public class ErrorHttpMapper
{
    private static readonly Dictionary<Type, int> _mappings = new();

    public static void Register<TError>(int statusCode) where TError : Error
    {
        _mappings[typeof(TError)] = statusCode;
    }

    public static int GetStatusCode(Error error)
    {
        return _mappings.GetValueOrDefault(error.GetType(), StatusCodes.Status500InternalServerError);
    }
}