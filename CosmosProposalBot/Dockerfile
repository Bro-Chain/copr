FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CosmosProposalBot/CosmosProposalBot.csproj", "CosmosProposalBot/"]
RUN dotnet restore "CosmosProposalBot/CosmosProposalBot.csproj"
COPY . .
WORKDIR "/src/CosmosProposalBot"
RUN dotnet build "CosmosProposalBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CosmosProposalBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CosmosProposalBot.dll"]
