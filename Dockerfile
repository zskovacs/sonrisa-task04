FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src

COPY global.json ./
COPY src/Sonrisa.Web/Sonrisa.Web.csproj src/Sonrisa.Web/
RUN dotnet restore src/Sonrisa.Web/Sonrisa.Web.csproj

COPY src/Sonrisa.Web/ src/Sonrisa.Web/
RUN dotnet publish src/Sonrisa.Web/Sonrisa.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sonrisa.Web.dll"]
