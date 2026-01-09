FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PushService/PushService.csproj", "PushService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "PushService/PushService.csproj"

COPY . .
WORKDIR "/src/PushService"
RUN dotnet publish "PushService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 🟢 Вот здесь меняем образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PushService.dll"]
