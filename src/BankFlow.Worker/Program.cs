using BankFlow.Worker.Configuration;
using BankFlow.Worker.Consumers;
using BankFlow.Worker.Observability;
using BankFlow.Worker.Data;
using BankFlow.Worker.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddBankFlowObservability(builder.Configuration);

var connectionString =
    builder.Configuration.GetConnectionString("BankFlowWorker")
    ?? throw new InvalidOperationException(
        "A connection string 'BankFlowWorker' não foi configurada.");

builder.Services.AddDbContext<WorkerDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<TransactionEventProcessor>();

var rabbitMqOptions = builder.Configuration
    .GetSection(RabbitMqOptions.SectionName)
    .Get<RabbitMqOptions>()
    ?? new RabbitMqOptions();

builder.Services.AddMassTransit(configuration =>
{
    configuration.AddConsumer<TransactionCreatedConsumer>();

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

        rabbitMq.ReceiveEndpoint(
            "bankflow-transaction-created",
            endpoint =>
            {
                endpoint.UseMessageRetry(retry =>
                    retry.Interval(
                        3,
                        TimeSpan.FromSeconds(2)));

                endpoint.ConfigureConsumer<TransactionCreatedConsumer>(
                    context);
            });
    });
});

var host = builder.Build();

host.Run();

