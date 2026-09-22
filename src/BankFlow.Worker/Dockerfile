FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/BankFlow.Worker/BankFlow.Worker.csproj", "src/BankFlow.Worker/"]
COPY ["src/BankFlow.Contracts/BankFlow.Contracts.csproj", "src/BankFlow.Contracts/"]

RUN dotnet restore "src/BankFlow.Worker/BankFlow.Worker.csproj"

COPY . .

WORKDIR "/src/src/BankFlow.Worker"
RUN dotnet publish "BankFlow.Worker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BankFlow.Worker.dll"]
