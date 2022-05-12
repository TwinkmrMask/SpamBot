FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SpamBot/SpamBot.csproj", "SpamBot/"]
RUN dotnet restore "SpamBot/SpamBot.csproj"
COPY . .
WORKDIR "/src/SpamBot"
RUN dotnet build "SpamBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpamBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SpamBot.dll"]
