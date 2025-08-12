using GreenDonut;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Core.Users.User;
using Microsoft.Extensions.DependencyInjection;
using LiveEventService.Application.Features.Users.Queries.GetUsersByIdentityIds;

namespace LiveEventService.API.GraphQL.DataLoaders;

public sealed class UserByIdentityIdDataLoader : BatchDataLoader<string, User>
{
    private readonly IServiceProvider _serviceProvider;

    public UserByIdentityIdDataLoader(IBatchScheduler batchScheduler, IServiceProvider serviceProvider)
        : base(batchScheduler, new DataLoaderOptions())
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<IReadOnlyDictionary<string, User>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var spec = new GetUsersByIdentityIdsSpecification(keys.ToList());
        var users = await userRepository.ListReadOnlyAsync(spec, cancellationToken);
        return users.ToDictionary(u => u.IdentityId);
    }
}


