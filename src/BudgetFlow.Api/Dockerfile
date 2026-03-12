# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/BudgetFlow.Api/BudgetFlow.Api.csproj", "src/BudgetFlow.Api/"]
COPY ["src/BudgetFlow.Application/BudgetFlow.Application.csproj", "src/BudgetFlow.Application/"]
COPY ["src/BudgetFlow.Domain/BudgetFlow.Domain.csproj", "src/BudgetFlow.Domain/"]
COPY ["src/BudgetFlow.Infrastructure/BudgetFlow.Infrastructure.csproj", "src/BudgetFlow.Infrastructure/"]

# Uses NuGet to download and install all the dependencies and tools required by a project
RUN dotnet restore "src/BudgetFlow.Api/BudgetFlow.Api.csproj"

COPY . .
WORKDIR "/src/src/BudgetFlow.Api"
# Builds project and creates the final runnable output you would copy to a server, VM, or another machine
RUN dotnet publish "BudgetFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "BudgetFlow.Api.dll"]