# Use official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MovieCalendar.API/MovieReleaseCalendar.API.csproj", "MovieReleaseCalendar.API/"]
RUN dotnet restore "MovieCalendar.API/MovieReleaseCalendar.API.csproj"
COPY . .
WORKDIR "/src/MovieCalendar.API"
RUN dotnet publish "MovieReleaseCalendar.API.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MovieReleaseCalendar.API.dll"]
