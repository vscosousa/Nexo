FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY FakeSmtp.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
# SMTP on 1025, inbox web UI on 8025; received mail is stored in /data (mount a volume there).
ENV ASPNETCORE_URLS=http://+:8025 \
    Smtp__Address=0.0.0.0 \
    Smtp__Port=1025 \
    Store__Directory=/data
EXPOSE 1025 8025
ENTRYPOINT ["dotnet", "FakeSmtp.dll"]
