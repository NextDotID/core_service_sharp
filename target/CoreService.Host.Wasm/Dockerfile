FROM alpine:3 AS download
RUN CURRENT_ARCH=`uname -m` && \
    wget -O /tmp/docker-compose https://github.com/docker/compose/releases/download/v2.16.0/docker-compose-linux-$CURRENT_ARCH

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
# See (https://en.wikipedia.org/wiki/W%5EX). This feature crashes the Qemu building for .NET 7
ARG DOTNET_EnableWriteXorExecute=0
WORKDIR /src
COPY ["target/CoreService.Host.Wasm/CoreService.Host.Wasm.csproj", "target/CoreService.Host.Wasm/"]
COPY ["src/CoreService.Api/CoreService.Api.csproj", "src/CoreService.Api/"]
COPY ["src/CoreService.Shared/CoreService.Shared.csproj", "src/CoreService.Shared/"]
COPY ["src/CoreService.Web/CoreService.Web.csproj", "src/CoreService.Web/"]
RUN dotnet restore "target/CoreService.Host.Wasm/CoreService.Host.Wasm.csproj"
COPY . .
WORKDIR "/src/target/CoreService.Host.Wasm"
RUN dotnet build "CoreService.Host.Wasm.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreService.Host.Wasm.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=download /tmp/docker-compose /usr/local/bin/docker-compose
RUN chmod +x /usr/local/bin/docker-compose
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoreService.Host.Wasm.dll"]
