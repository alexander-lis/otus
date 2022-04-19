FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine3.15 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.15 AS build
WORKDIR /src
COPY ["MyApp.DbMigrator/MyApp.DbMigrator.csproj", "MyApp.DbMigrator/"]
RUN dotnet restore "MyApp.DbMigrator/MyApp.DbMigrator.csproj"
COPY . .
WORKDIR "/src/MyApp.DbMigrator"
RUN dotnet build "MyApp.DbMigrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyApp.DbMigrator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.DbMigrator.dll"]
