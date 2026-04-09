using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.SharePoint;

public static class SharePointServiceCollectionExtensions
{
    public static IServiceCollection AddGraphSharePointDocumentService(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SharePointOptions>()
            .Bind(configuration.GetSection(SharePointOptions.SectionName))
            .Validate(static options => options.IsValid(), "SharePoint options are invalid")
            .ValidateOnStart();

        services.AddScoped<ISharePointDocumentService, GraphSharePointDocumentService>();

        return services;
    }
}
