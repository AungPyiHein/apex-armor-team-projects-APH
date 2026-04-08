using Common;
using Mapper;

namespace MiniPos.Backend.Extensions;

public static class ErrorMappingExtensions
{
    public static IServiceCollection AddErrorMappings(this IServiceCollection services)
    {
        ErrorHttpMapper.Register<NotFoundError>(StatusCodes.Status404NotFound);
        ErrorHttpMapper.Register<ConflictError>(StatusCodes.Status409Conflict);
        ErrorHttpMapper.Register<InternalError>(StatusCodes.Status500InternalServerError);
        ErrorHttpMapper.Register<ValidationError>(StatusCodes.Status400BadRequest);
        ErrorHttpMapper.Register<BadRequestError>(StatusCodes.Status400BadRequest);
        ErrorHttpMapper.Register<UnAuthorizedError>(StatusCodes.Status401Unauthorized);
        
        return services;
    }
}