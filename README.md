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

* If you haven't already done so, follow this [link](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) to install the dotnet 6 runtime to your machine. You could install the SDK, or the ASP.NET Core runtime.
* Open up your preferred terminal and access the root folder of the cloned project
* Run the following command to run the project. 
```
dotnet run --project src/TrueLayerPokedex/TrueLayerPokedex.csproj
```
* The project uses ports 5000 by default. If you need to change this, access `src/TrueLayerPokedex/Properties/launchSettings.json` to update them.

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

* The specification says: "If you can't translate the Pokemon's description (for whatever reason) then use the standard description". I followed this design choice in the application by allowing each `ITranslator` implementor to just return the input description. I think this logic kind of makes it hard to tell from the outside what the problem was though, as all the user sees is the standard description. I may consider adding some extra information to the response to show why the response came back as standard (if the requirements and stakeholders allowed it) or I would implement some kind of logging that shows something happened here.

* Within the application, I made use of a cache system that would allow the app to cache the response of requests. This was done with the `IDistributedCache` interface, which was implemented with an in memory distributed cache. This was a perfect implementation for this project, but in a real world scenario, you'd want to use an actually distributed cache running separately from the API. I chose this interface because it was supplied by dotnet itself, and has other implementations, such as the Redis one. Changing to an actually distributed version would be easy, only requiring me to change the DI registration in the Startup.cs to register the Redis version (and to actually set up that Redis cache). I'd also update the API to return some headers in the responses to indicate the cache was used and when it expires.

* Currently, the deployment of this application consists simply of a single dotnet API instance, this is fine for this challenge, but in a real world scenario a single instance on its own would not hold up well against high traffic usage. To deal with higher demand, I could deploy this as a number of instances which each sit behind a load balancer application, such as nginx. You could also utilise Kubernetes to manage the addition or subtraction of instances to deal with demand.

* For this project, I only needed to implement two endpoints, but in the future I may be required to add more. I think the code itself lends itself well to extension, but I may want to consider some documentation tools like Swagger that can programmatically create a documentation site for me as I add new endpoints and functionality.

* Currently, the system is quite brittle when dealing with third party APIs. For example, in the event that the PokeApi or translation Api returned a 500 error momentarily, the system would just deal with this as an error and return a response for that. But in a production app you may want more resiliency in the system. Here I would make use of the [Polly](https://github.com/App-vNext/Polly) nuget package to easily allow those interactions to retry if certain conditions happen or fallback to other options if possible.

* In production I'd add a logging sink that could log out to a DB or something like AWS cloudwatch. Currently the app only logs to the console.
* In production I'd add some kind of rate limiting functionality, public APIs such as this can be easily abused if they don't restrict usage. For the purposes of this challenge I didn't think it was necessary but it would be extremely important in a real scenario. I could do this by using [this nuget package](https://github.com/stefanprodan/AspNetCoreRateLimit) which adds a middleware to the request pipeline which can reject requests early from IPs if they violate the rate limit. 

* The translation functionality is split across a couple of services. The `ITranslationService` is used to abstract the rest away from the handlers. It is injected with a collection of `ITranslator`s. Currently, there are only two `ITranslator` implementations, the `YodaTranslator` and the `ShakespeareTranslator`. These two translators both have similar code, which could be considered a violation of DRY. In a production version, I would consider adding another service that both of these translators could use internally to avoid having to have this duplicate code. I did not do this in this challenge because I didn't want to add yet another service, which I thought would add a bit too much complexity.

* When the project gets data from the PokeApi, it is possible this might fail somehow, the most common issue might be that the pokemon name was not recognised. In this scenario the PokeApi returns a 404. When the project sees this error it will return a response of the same status code to its caller. I think this could be improved slightly. Imagine a scenario where we hit some rate limit in the PokeApi, it would probably return a 429 response, but when the caller of our API sees this response they might think they've violated our rate limit, when in reality the API has violated the PokeApi's one. This might be confusing and I think could be improved by simply handling the response from the PokeApi differently for certain responses from it. I didn't do this in this in this project because I wasn't sure what the response status code should be, should it be 400? 500? Nothing really seemed to fit properly. I also didn't want to try and handle a couple of specific scenarios and miss some important ones, so i err'd on the side of simplicity and just returned the PokeApi's response directly. 

## Project structure overview

The solution is split into 4 projects, each with a specific role. This separation of concerns is based on the clean architecture approach. I like this approach as I think the split between the projects makes sense and lends itself well to extension.

### Domain

This project contain models, dtos, options, and "low" level interfaces and logic that would be used in various different parts of the application. In this app, this section is quite thin, only containing a few models and an `IUtcNowProvider`, which allows me to more easily mock a date time instead of using `DateTime.Now` and trying to fake it.

### Application

This project contains application logic, which is split into "vertical slices" using the [MediatR](https://github.com/jbogard/MediatR) nuget package. It is dependent on the `Domain`, but has no dependencies on any other project. This section defines interfaces that are implemented by the `Infrastructure` project. For example, the `IPokemonService`, which actually gets the pokemon data, is defined in this section, but is implemented in the `Infrastructure` project. 

Find the handlers for each endpoint in this project, in the `Queries` directory.

### Infrastructure

This project contains classes for accessing external resources such as file systems, web services, smtp, and so on. These classes should be based on interfaces defined within the application section. The `PokemonService` and `TranslationService` and `Translators` are implemented here. Each `Translator` provides functionality to change a Pokemon's description in a certain way. 

The `TranslationService` is injected with a collection of them and when called upon to get a translated version, will go through them until it finds one that can translate the given pokemon info. It will then use the chosen `Translator`. The order of the translators matters, and it is preserved from when they are registered in the DI container. This collection injection allows for the translation system to be extended without having to modify the existing code. You could just add a new `ITranslator`, which defines whether it can translate the info for a pokemon as well as how to actually translate it, then add it to the DI container.

### Presentation

In the context of this project, the presentation section is a web API, but this can take many forms (such as front end website or mobile app views). This section depends on both the `Application` and `Infrastructure` projects, however, the dependency on `Infrastructure` is only to support dependency injection. This project exposes the endpoints through the `PokemonController`, which I purposefully keep very thin, only passing on the request to `MediatR` to be handled.
