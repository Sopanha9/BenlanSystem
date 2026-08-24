# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["BenLanSystem/BenLanSystem.csproj", "BenLanSystem/"]
RUN dotnet restore "BenLanSystem/BenLanSystem.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/BenLanSystem"
RUN dotnet build "BenLanSystem.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "BenLanSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BenLanSystem.dll"]
