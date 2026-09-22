using BankFlow.Api.Configuration;
using BankFlow.Api.Data;
using BankFlow.Api.Endpoints;
using BankFlow.Api.Observability;
using BankFlow.Api.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddBankFlowObservability(builder.Configuration);

var connectionString =
    builder.Configuration.GetConnectionString("BankFlow")
    ?? throw new InvalidOperationException(
        "A connection string 'BankFlow' não foi configurada.");

builder.Services.AddDbContext<BankFlowDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<BankFlowDbContext>();

var rabbitMqOptions = builder.Configuration
    .GetSection(RabbitMqOptions.SectionName)
    .Get<RabbitMqOptions>()
    ?? new RabbitMqOptions();

builder.Services.AddMassTransit(configuration =>
{
    configuration.UsingRabbitMq((context, rabbitMq) =>
    {
        rabbitMq.Host(
            rabbitMqOptions.Host,
            rabbitMqOptions.VirtualHost,
            host =>
            {
                host.Username(rabbitMqOptions.Username);
                host.Password(rabbitMqOptions.Password);
            });
    });
});

builder.Services.AddHostedService<OutboxPublisherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "BankFlow.Api",
    status = "Running",
    utc = DateTimeOffset.UtcNow
}))
.WithName("ServiceInfo");

app.MapHealthChecks("/health");

app.MapTransactionEndpoints();

app.Run();

public partial class Program;

