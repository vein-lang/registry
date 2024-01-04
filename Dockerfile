FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /build
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
RUN apt-get update \
    && apt-get install -y --no-install-recommends libc6-dev
ENTRYPOINT ["dotnet", "core.dll"]