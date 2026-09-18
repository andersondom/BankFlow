using BankFlow.Api.Configuration;
using BankFlow.Api.Endpoints;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

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
