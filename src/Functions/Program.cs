using Functions.Workflow;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("AppDb")
            ?? context.Configuration["ConnectionStrings:AppDb"]
            ?? throw new InvalidOperationException("Connection string 'AppDb' was not found.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sqlServerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
        });

        services.AddScoped<IWorkflowStateStore, EfWorkflowStateStore>();
        services.AddScoped<IWorkflowStepExecutor, WorkflowStepExecutor>();
    })
    .Build();

host.Run();
