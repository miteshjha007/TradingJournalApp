# Use the official .NET 10.0 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the solution file and project files
COPY *.slnx ./
COPY TradingJournal.API/*.csproj ./TradingJournal.API/
COPY TradingJournal.Application/*.csproj ./TradingJournal.Application/
COPY TradingJournal.Domain/*.csproj ./TradingJournal.Domain/
COPY TradingJournal.Infrastructure/*.csproj ./TradingJournal.Infrastructure/

# Restore dependencies
RUN dotnet restore TradingJournal.API/TradingJournal.API.csproj

# Copy the rest of the code
COPY . ./

# Publish the application
RUN dotnet publish TradingJournal.API/TradingJournal.API.csproj -c Release -o /app/out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port 8080 (Render default for web services)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TradingJournal.API.dll"]
