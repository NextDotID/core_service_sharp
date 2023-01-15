#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
ADD https://download.docker.com/linux/static/stable/x86_64/docker-20.10.9.tgz /tmp/
ADD https://github.com/docker/compose/releases/download/v2.14.2/docker-compose-linux-x86_64 /tmp/
RUN tar -xzf /tmp/docker-20.10.9.tgz -C /tmp/ && \
	mv /tmp/docker/* /usr/local/bin/ && \
	mv /tmp/docker-compose-linux-x86_64 /usr/local/bin/docker-compose && \
	rm -rf /tmp/docker /tmp/docker-20.10.9.tgz /tmp/docker-compose-linux-x86_64 && \
	chmod +x /usr/local/bin/docker-compose
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/CoreService.Api/CoreService.Api.csproj", "src/CoreService.Api/"]
COPY ["src/CoreService.Shared/CoreService.Shared.csproj", "src/CoreService.Shared/"]
COPY ["src/CoreService.Web/CoreService.Web.csproj", "src/CoreService.Web/"]
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
