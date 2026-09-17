FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /app

COPY Nexo.Api.csproj ./
COPY .config/dotnet-tools.json .config/dotnet-tools.json
RUN dotnet restore && dotnet tool restore

COPY . .
RUN dotnet build --no-restore

EXPOSE 8080
CMD ["sh", "-c", "dotnet ef database update --no-build && exec dotnet bin/Debug/net10.0/Nexo.Api.dll"]
