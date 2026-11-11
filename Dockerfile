# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for dependency layer caching
COPY ["ProjectPartB/ProjectPartB.csproj", "ProjectPartB/"]
RUN dotnet restore "ProjectPartB/ProjectPartB.csproj"

# Copy all source files and publish
COPY . .
WORKDIR "/src/ProjectPartB"
RUN dotnet publish "ProjectPartB.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Environment Configuration for Vercel Container Runtime
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__SqliteConnection="Data Source=/tmp/RentalCarWebsite.db"

EXPOSE 8080

ENTRYPOINT ["dotnet", "ProjectPartB.dll"]
