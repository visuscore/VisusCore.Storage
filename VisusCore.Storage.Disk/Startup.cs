using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Modules;
using VisusCore.Storage.Core.Extensions;
using VisusCore.Storage.Disk.Models;
using VisusCore.Storage.Disk.Services;

namespace VisusCore.Storage.Disk;

public class Startup : StartupBase
{
    private const string DiskStorageSection = "VisusCore_Storage_Disk";
    private readonly IShellConfiguration _shellConfiguration;

    public Startup(IShellConfiguration shellConfiguration) =>
        _shellConfiguration = shellConfiguration;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.Configure<DiskStorageOptions>(
            options => _shellConfiguration
                .GetSection($"{DiskStorageSection}:{nameof(DiskStorageOptions)}")
                .Bind(options));

        services.AddStreamSegmentStorage<StreamSegmentDiskStorage>();
    }
}
