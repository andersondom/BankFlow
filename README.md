# 🏦 BankFlow

Sistema bancário genérico orientado a eventos desenvolvido com .NET para demonstrar mensageria, resiliência e arquitetura de sistemas distribuídos.

## Objetivo

O BankFlow recebe operações por uma API, publica eventos e realiza processamento assíncrono através de consumidores independentes.

## Arquitetura planejada

Client -> BankFlow.Api -> MassTransit -> RabbitMQ -> BankFlow.Worker -> Persistência

## Projetos

- BankFlow.Api: entrada HTTP e publicação de eventos.
- BankFlow.Contracts: contratos compartilhados.
- BankFlow.Worker: consumo e processamento assíncrono.
- BankFlow.Api.Tests: testes da API e contratos.
- BankFlow.Worker.Tests: testes do processamento assíncrono.

## Tecnologias planejadas

.NET 10, ASP.NET Core, Worker Service, MassTransit, RabbitMQ, Entity Framework Core, SQL Server, Docker, xUnit, GitHub Actions e OpenTelemetry.

## Status

Projeto em desenvolvimento incremental.
