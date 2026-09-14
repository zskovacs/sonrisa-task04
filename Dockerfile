FROM node:24.21.0-bookworm-slim AS css-build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY src/Sonrisa.Web/Pages/ src/Sonrisa.Web/Pages/
COPY src/Sonrisa.Web/Styles/ src/Sonrisa.Web/Styles/
RUN npm run css:build

FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src

COPY global.json ./
COPY src/Sonrisa.Web/Sonrisa.Web.csproj src/Sonrisa.Web/
RUN dotnet restore src/Sonrisa.Web/Sonrisa.Web.csproj

COPY src/Sonrisa.Web/ src/Sonrisa.Web/
COPY --from=css-build /src/src/Sonrisa.Web/wwwroot/css/site.css src/Sonrisa.Web/wwwroot/css/site.css
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
