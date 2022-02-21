FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore ./src/TrueLayerPokedex/TrueLayerPokedex.csproj 
RUN dotnet publish ./src/TrueLayerPokedex/TrueLayerPokedex.csproj -c Debug -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime-env

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "TrueLayerPokedex.dll"]