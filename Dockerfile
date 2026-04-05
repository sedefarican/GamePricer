# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["gamepricer/gamepricer.csproj", "gamepricer/"]
RUN dotnet restore "gamepricer/gamepricer.csproj"

# Copy all source files
COPY . .
WORKDIR "/src/gamepricer"

# Build and publish
RUN dotnet publish "gamepricer.csproj" -c Release -o /app/publish --no-restore

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
EXPOSE 8443

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "gamepricer.dll"]
