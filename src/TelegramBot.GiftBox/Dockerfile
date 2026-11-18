FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/TelegramBot.GiftBox/TelegramBot.GiftBox.csproj src/TelegramBot.GiftBox/
RUN dotnet restore src/TelegramBot.GiftBox/TelegramBot.GiftBox.csproj

COPY . .
RUN dotnet publish src/TelegramBot.GiftBox/TelegramBot.GiftBox.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TelegramBot.GiftBox.dll"]
