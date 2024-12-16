# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /source
COPY . .
RUN dotnet restore "./POSServer/POSServer.csproj" --disable-parallel
RUN dotnet publish "./POSServer/POSServer.csproj" -c Release -o /app --no-restore

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "POSServer.dll"]
