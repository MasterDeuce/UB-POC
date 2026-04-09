using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Procore;

public static class ProcoreServiceCollectionExtensions
{
    public static IServiceCollection AddProcore(this IServiceCollection services, IConfiguration configuration, bool useStub = false)
    {
        services.Configure<ProcoreOptions>(configuration.GetSection(ProcoreOptions.SectionName));

        if (useStub)
        {
            services.AddSingleton<IProcoreService, StubProcoreService>();
            return services;
        }

        services.AddHttpClient<IProcoreService, ProcoreService>();
        return services;
    }
}
