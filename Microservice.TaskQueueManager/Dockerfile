#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Microservice.TaskQueueManager/Microservice.TaskQueueManager.csproj", "Microservice.TaskQueueManager/"]
COPY ["Microservice.Domain/Microservice.Domain.csproj", "Microservice.Domain/"]
RUN dotnet restore "./Microservice.TaskQueueManager/./Microservice.TaskQueueManager.csproj"
COPY . .
WORKDIR "/src/Microservice.TaskQueueManager"
RUN dotnet build "./Microservice.TaskQueueManager.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Microservice.TaskQueueManager.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microservice.TaskQueueManager.dll"]