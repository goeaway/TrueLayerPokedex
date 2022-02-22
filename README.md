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

## Changes for a production version

* The specification says: "If you can't translate the Pokemon's description (for whatever reason) then use the standard description". I followed this design choice in the application by allowing each `ITranslator` implementor to just return the input description. I think this logic kind of makes it hard to tell from the outside what the problem was though, as all the user sees is the standard description. I may consider adding some extra information to the response to show why the response came back as standard (if the requirements and stakeholders allowed it) or I would implement some kind of logging that shows something happened here

* Within the application, I made use of a cache system that would allow the app to cache the response of requests. This was done with the `IDistributedCache` interface, which was implemented with an in memory distributed cache. This was a perfect implementation for this project, but in a real world scenario, you'd want to use an actually distributed cache running separately from the API. I chose this interface because it was supplied by dotnet itself, and has other implementations, such as the Redis one. Changing to an actually distributed version would be easy, only requiring me to change the DI registration in the Startup.cs to register the Redis version (and to actually set up that Redis cache). I'd also update the API to return some headers in the responses to indicate the cache was used and when it expires.

* Currently, the deployment of this application consists simply of a single dotnet API instance, this is fine for this challenge, but in a real world scenario a single instance on its own would not hold up well against high traffic usage. To deal with higher demand, I could deploy this as a number of instances which each sit behind a load balancer application, such as nginx. You could also utilise Kubernetes to manage the addition or subtraction of instances to deal with demand.

* For this project, I only needed to implement two endpoints, but in the future I may be required to add more. I think the code itself lends itself well to extension, but I may want to consider some documentation tools like Swagger that can programmatically create a documentation site for me as I add new endpoints and functionality.

* Currently, the system is quite brittle when dealing with third party APIs. For example, in the event that the PokeApi or translation Api returned a 500 error momentarily, the system would just deal with this as an error and return a response for that. But in a production app you may want more resiliency in the system. Here I would make use of the [Polly](https://github.com/App-vNext/Polly) nuget package to easily allow those interactions to retry if certain conditions happen or fallback to other options if possible.

* In production I'd add a logging sink that could log out to a DB or something like AWS cloudwatch. Currently the app only logs to the console.
* In production I'd add some kind of rate limiting functionality, public APIs such as this can be easily abused if they don't restrict usage. For the purposes of this challenge I didn't think it was necessary but it would be extremely important in a real scenario. I could do this by using [this nuget package](https://github.com/stefanprodan/AspNetCoreRateLimit) which adds a middleware to the request pipeline which can reject requests early from IPs if they violate the rate limit. 


## Project structure overview

For this project, I followed a clean architecture approach. Clean Architecture is an architectural style created and popularised by Robert C Martin. To put it simply, it tries to define clear boundaries between sections of an application and dictates the direction dependencies can be created between those sections. I chose this approach because I think it does a good job of separating the concerns of differents parts of the application and I think it's quite popular, at least amongst .Net devs!

Below is a diagram that illustrates the architecture quite well

<img src="https://netsharpdev.com/images/posts/shape.png" width="400" alt="Clean Architecture Diagram" />

The diagram depicts the application as a set of concentric circles, each layer wrapping another, notice that the arrows that depict a dependency link between the sections are only going inward.

### Domain

This contain models, enums, exceptions, and "low" level interfaces and logic that would be used throughout the rest of the application. In the context of this project, this is a class library and is quite thin, only containing a few models and an `IUtcNowProvider`, which allows me to more easily mock a date time instead of using `DateTime.Now` and trying to fake it.

### Application

This section contains all application logic. It is dependent on the `Domain`, but has no dependencies on any other section. This section defines interfaces that are implemented by outer sections. For example, the `IPokemonService`, which actually gets the pokemon data, is defined in this section, but is implemented in the `Infrastructure` section.

### Infrastructure + Persistence

This section contains classes for accessing external resources such as file systems, web services, smtp, and so on. These classes should be based on interfaces defined within the application section. The `PokemonService` and `TranslationService` and `Translators` are implemented here. 

### Presentation

In the context of this project, the presentation section is a web API, but this can take many forms (such as front end website or mobile app views). This section depends on both the `Application` and `Infrastructure` layers, however, the dependency on `Infrastructure` is only to support dependency injection. 
