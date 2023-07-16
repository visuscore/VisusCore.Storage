using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VisusCore.Consumer.Core.Extensions;
using VisusCore.Storage.Abstractions.Services;

namespace VisusCore.Storage.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStreamSegmentStorage<TStreamSegmentStorage>(this IServiceCollection services)
        where TStreamSegmentStorage : class, IStreamSegmentStorage
    {
        services.AddScoped<TStreamSegmentStorage>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IStreamSegmentStorage, TStreamSegmentStorage>(services =>
                services.GetRequiredService<TStreamSegmentStorage>()));
        services.AddVideoStreamSegmentConsumer<TStreamSegmentStorage>();

        return services;
    }
}
