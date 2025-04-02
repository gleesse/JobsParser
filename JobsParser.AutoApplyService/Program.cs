using JobsParser.AutoApplyService;
using JobsParser.AutoApplyService.DSL;
using JobsParser.AutoApplyService.Models;
using JobsParser.AutoApplyService.Repositories;
using JobsParser.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configuration
        services.Configure<AutoApplyServiceOptions>(hostContext.Configuration.GetSection("AutoApplyOptions"));
        services.Configure<PlaywrightOptions>(hostContext.Configuration.GetSection("PlaywrightOptions"));

        // Database context
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(hostContext.Configuration.GetSection("DatabaseSettings")["ConnectionString"]);
        });

        // DSL and Interpreter
        services.AddSingleton<JsonDslInterpreter>();
        services.AddSingleton<IWorkflowRepository, WorkflowRepository>(); //can be a singleton because it uses file storage, so concurrent work won't happen anyway since the file will be locked. 
        services.AddSingleton<IFormRepository, FormRepository>(); //can be a singleton because it uses file storage, so concurrent work won't happen anyway since the file will be locked. 

        // Register the background service
        services.AddHostedService<AutoApplyBackgroundService>();
    })
    .Build();

await host.RunAsync();
