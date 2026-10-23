FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props global.json ./
COPY src/ ./src/
RUN dotnet restore src/Tickora.Web/Tickora.Web.csproj
RUN dotnet publish src/Tickora.Web/Tickora.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data && chown -R $APP_UID /app/App_Data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s CMD curl --fail http://localhost:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "Tickora.Web.dll"]
