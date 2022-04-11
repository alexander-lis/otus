FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine3.15 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.15 AS build
WORKDIR /src
COPY ["MyApp.Backend/MyApp.Backend.csproj", "MyApp.Backend/"]
RUN dotnet restore "MyApp.Backend/MyApp.Backend.csproj"
COPY . .
WORKDIR "/src/MyApp.Backend"
RUN dotnet build "MyApp.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyApp.Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Backend.dll"]
