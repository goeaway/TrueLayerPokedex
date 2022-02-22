# TrueLayerPokedex

Project specification: https://docs.google.com/document/d/13EtWfHtIXEvMf-0HmbhsgX83EUlTUEdqPPIv4InbuuI/edit

TrueLayer SE Challenge from Joe Thompson-Murdoch

Built with .Net 6

## Contents

* How to run
* How to use
* Changes for a production version
* Project structure overview

## How to run
* Clone the repo to your computer
* You can run the project with Docker or using the dotnet CLI, or alternatively open the project in Visual Studio or Rider or another IDE and run from there, more details below.

#### Run with Docker

* If you haven't already done so, follow this [link](https://docs.docker.com/get-docker/) to install docker to your machine
* Then open up your preferred terminal and access the root folder of the cloned project 
* Run the following two commands to build the project image and then run the container, set a port that is free and give the container a name if you wish
```
docker build -t truelayerpokedex:latest -f deploy/Dockerfile .
```

```
docker run -p <any available port>:80 --name <any container name> truelayerpokedex:latest 
```

#### Run with dotnet CLI

* If you haven't already done so, follow this [link](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) to isntall the dotnet 6 runtime to your machine. You could install the SDK, or the ASP.NET Core runtime.
* Open up your preferred terminal and access the root folder of the cloned project
* Run the following command to run the project. 
```
dotnet run --project src/TrueLayerPokedex/TrueLayerPokedex.csproj
```
* The project uses ports 5000 and 5001 by default. If you need to change this, access `src/TrueLayerPokedex/Properties/launchSettings.json` to update them.

#### Run with an IDE

* Open the cloned project in your preferred IDE
* Ensure your IDE supports dotnet 6
* Run the project through the IDE, the entry point for the app is `TrueLayerPokedex.csproj`


## How to Use

Two endpoints are available:

`/pokemon/<pokemonName>` - with which you can get standard information on a pokemon

`/pokemon/translated/<pokemonName>` - with which you can get the translated information on a pokemon

Make an HTTP GET request to either of these endpoints through your preferred method, e.g. a curl request like the below:

`curl http://localhost:9000/pokemon/mewtwo`

Returns the below response

```
{
	"name":"mewtwo",
	"description":"It was created by a scientist after years of horrific gene splicing and DNA engineering experiments.",
	"habitat":"rare",
	"isLegendary":true
}
```

A request to the translated endpoint like the below:

`curl http://localhost:9000/pokemon/translated/mewtwo`

Returns the below response

```
{
    "name": "mewtwo",
    "description": "Created by a scientist after years of horrific gene splicing and dna engineering experiments,  it was.",
    "habitat": "rare",
    "isLegendary": true
}
```
