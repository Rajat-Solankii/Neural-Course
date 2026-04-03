# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the csproj and restore dependencies
COPY ["ELearnAggregator.csproj", "./"]
RUN dotnet restore "ELearnAggregator.csproj"

# Copy the rest of the backend files and build
COPY . .
RUN dotnet publish "ELearnAggregator.csproj" -c Release -o /app/publish

# Use the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Port 10000 is the default for Render
ENV AASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ELearnAggregator.dll"]