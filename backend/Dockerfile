# 1. Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 2. Copy the csproj and restore dependencies
# We use the filename from your screenshot: ELearnAggregator.csproj
COPY ["ELearnAggregator.csproj", "./"]
RUN dotnet restore "ELearnAggregator.csproj"

# 3. Copy the rest of the backend files and build
COPY . .
RUN dotnet publish "ELearnAggregator.csproj" -c Release -o /app/publish

# 4. Use the lightweight runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# 5. Overriding default ports to work with Render's environment
# We bind to 0.0.0.0 so the container accepts outside traffic
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_HTTP_PORTS=10000

# Inform Render that we are using port 10000
EXPOSE 10000

# 6. Start the application
ENTRYPOINT ["dotnet", "ELearnAggregator.dll"]