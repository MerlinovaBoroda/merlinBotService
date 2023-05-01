using MerlinBot_Service;
using MerlinBot_Service.Services;
using Microsoft.Extensions.Hosting;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<MerlinBotProperties>();

        services.AddScoped<MerlinBotService>();

        services.AddHostedService<Worker>();
    }).Build();

await host.RunAsync();