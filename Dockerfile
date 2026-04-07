FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["src/SmartChainControl.Server/SmartChainControl.Server.csproj", "src/SmartChainControl.Server/"]
COPY ["src/SmartChainControl.Client/SmartChainControl.Client.csproj", "src/SmartChainControl.Client/"]
COPY ["src/SmartChainControl.Shared/SmartChainControl.Shared.csproj", "src/SmartChainControl.Shared/"]

RUN dotnet restore "src/SmartChainControl.Server/SmartChainControl.Server.csproj"

COPY src/ src/

WORKDIR "/app/src/SmartChainControl.Server"
RUN dotnet publish "SmartChainControl.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=5206
EXPOSE 5206

ENTRYPOINT ["dotnet", "SmartChainControl.Server.dll"]