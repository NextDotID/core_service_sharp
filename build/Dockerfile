FROM busybox:latest AS download
RUN CURRENT_ARCH=`uname -m` && \
    wget --no-check-certificate -O /tmp/docker-compose https://github.com/docker/compose/releases/download/v2.16.0/docker-compose-linux-$CURRENT_ARCH

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/CoreService.Api/CoreService.Api.csproj", "src/CoreService.Api/"]
COPY ["src/CoreService.Shared/CoreService.Shared.csproj", "src/CoreService.Shared/"]
RUN dotnet restore "src/CoreService.Api/CoreService.Api.csproj"
COPY . .
WORKDIR "/src/src/CoreService.Api"
RUN dotnet build "CoreService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=download /tmp/docker-compose /usr/local/bin/docker-compose
RUN chmod +x /usr/local/bin/docker-compose
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoreService.Api.dll"]
