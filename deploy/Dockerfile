FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore ./src/TrueLayerPokedex/TrueLayerPokedex.csproj 
RUN dotnet publish ./src/TrueLayerPokedex/TrueLayerPokedex.csproj -c Debug -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime-env

# Below allows for debugging in Rider IDE
RUN apt-get update
RUN apt-get -y --no-install-recommends install openssh-server && mkdir -p /run/sshd && echo root:test | chpasswd
RUN printf "Port 22\nPermitRootLogin yes" >> /etc/ssh/sshd_config

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["/bin/sh", "-c", "service ssh start && dotnet TrueLayerPokedex.dll"]