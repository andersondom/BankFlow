using BankFlow.Worker.Configuration;
using BankFlow.Worker.Consumers;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

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
