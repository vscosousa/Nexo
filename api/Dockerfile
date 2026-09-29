# Development image, used by compose.yaml (`target: dev`): Debug build on the SDK, applying migrations on start.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /app

COPY Nexo.Api.csproj ./
COPY .config/dotnet-tools.json .config/dotnet-tools.json
RUN dotnet restore && dotnet tool restore

COPY . .
RUN dotnet build --no-restore

EXPOSE 8080
CMD ["sh", "-c", "dotnet ef database update --no-build && exec dotnet bin/Debug/net10.0/Nexo.Api.dll"]

# Release build plus a migrations bundle (`efbundle`) for the production image.
FROM dev AS publish
RUN dotnet publish Nexo.Api.csproj -c Release -o /out --no-restore \
    && dotnet ef migrations bundle --configuration Release -o /out/efbundle

# Production image (the default target): runtime only, as the image's non-root user. It does not migrate on start;
# run `./efbundle --connection "<connection string>"` from this image as a separate deploy step first.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
WORKDIR /app
COPY --from=publish /out ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Nexo.Api.dll"]
