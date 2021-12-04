FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/core.csproj", "src/"]
COPY ["vein-lang/lib/projectsystem/vein.project.csproj", "vein-lang/lib/projectsystem/"]
COPY ["vein-lang/runtime/common/vein.common.csproj", "vein-lang/runtime/common/"]
COPY ["vein-lang/lib/ast/vein.ast.csproj", "vein-lang/lib/ast/"]
RUN dotnet restore "src/core.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK CMD curl --fail http://localhost:8080/health || exit
ENTRYPOINT ["dotnet", "core.dll"]