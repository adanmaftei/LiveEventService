using GreenDonut;
using LiveEventService.Application.Common.Interfaces;
using LiveEventService.Core.Users.User;
using LiveEventService.Application.Features.Users.Queries.GetUsersByIdentityIds;

namespace LiveEventService.API.GraphQL.DataLoaders;

public sealed class UserByIdentityIdDataLoader : BatchDataLoader<string, User>
{
    private readonly IUserRepository _userRepository;

    public UserByIdentityIdDataLoader(
        IBatchScheduler batchScheduler,
        IUserRepository userRepository)
        : base(batchScheduler, new DataLoaderOptions
        {
            // Keep batches at a reasonable size for DB and index efficiency
            MaxBatchSize = 250
        })
    {
        _userRepository = userRepository;
    }

    protected override async Task<IReadOnlyDictionary<string, User>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        // Normalize keys to avoid duplicates and trim whitespace
        var normalized = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalized.Count == 0)
        {
            return new Dictionary<string, User>(StringComparer.Ordinal);
        }

        var spec = new GetUsersByIdentityIdsSpecification(normalized);
        var users = await _userRepository.ListReadOnlyAsync(spec, cancellationToken);

        // Use an ordinal comparer; adjust if IDs are case-insensitive in your IdP
        var map = new Dictionary<string, User>(normalized.Count, StringComparer.Ordinal);
        foreach (var u in users)
        {
            if (!string.IsNullOrWhiteSpace(u.IdentityId))
            {
                map[u.IdentityId] = u;
            }
        }
        return map;
    }
}


