#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/CoreService.Shared/CoreService.Shared.csproj", "src/CoreService.Shared/"]
COPY ["src/CoreService.Api/CoreService.Api.csproj", "src/CoreService.Api/"]
RUN dotnet restore "src/CoreService.Shared/CoreService.Shared.csproj"
RUN dotnet restore "src/CoreService.Api/CoreService.Api.csproj"
COPY . .
WORKDIR "/src/src/CoreService.Api"
RUN dotnet build "CoreService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoreService.Api.dll"]
