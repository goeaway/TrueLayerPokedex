# TrueLayerPokedex

Project specification: https://docs.google.com/document/d/13EtWfHtIXEvMf-0HmbhsgX83EUlTUEdqPPIv4InbuuI/edit

TrueLayer SE Challenge from Joe Thompson-Murdoch

Built with .Net 6

## Contents

* How to run
* How to use
* Changes for a production version
* Project structure overview
* My approach

## How to run
* Clone the repo to your computer
* Use one of the following methods below that best suits you

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

* If you haven't already done so, follow this [link](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) to install dotnet 6 to your machine. You could install the SDK, or the ASP.NET Core runtime.
* Open up your preferred terminal and access the root folder of the cloned project
* Run the following command to run the project. 
```
dotnet run --project src/TrueLayerPokedex/TrueLayerPokedex.csproj
```
* The project uses port 5000 by default. If you need to change this, access `src/TrueLayerPokedex/Properties/launchSettings.json` to update it.

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

#### Add some indication of translation failure

The specification says: "If you can't translate the Pokemon's description (for whatever reason) then use the standard description". I followed this design choice by allowing each `ITranslator` implementor to just return the input description. I think this design makes it a bit hard to tell from the outside what the problem was though, as all the user sees is the standard description. I may consider adding some extra information to the response to show why the response came back as standard (if the requirements and stakeholders allowed it) or I would implement some kind of logging that shows something happened here.

#### Update to use a distributed cache implementation like Redis

When investigating the translation api I noticed the rate limit for their free tier was quite restrictive, callers could only make 5 requests per hour and 60 per day. I decided it would be good to add caching to the app, which would cache the responses from the public APIs and return them in future if asked for again.

This was done with the `IDistributedCache` interface, implemented with an in memory distributed cache. This was a perfect implementation for this project, but in a real world scenario, you'd want to use an actually distributed cache running separately from the API. I chose this interface because it was supplied by dotnet itself, and has other implementations, such as the Redis one. Changing to an actually distributed version would be easy, only requiring me to change the DI registration in the Startup.cs to register the Redis version. I'd also consider updating the API to return some headers to indicate the cache was used and when it expires.

#### Deploy to multiple instances

Currently, the deployment of this application is a single dotnet API instance. This is fine for this challenge, but in a real world scenario a single instance would not hold up well against high traffic usage. To deal with higher demand, I could deploy this as a number of instances protected by a load balancer application. You could also utilise Kubernetes to manage the addition or subtraction of instances to deal with demand fluctuations.

#### "Automatic" Documentation e.g. Swagger

For this project, I only needed to implement two endpoints, but in the future more may be required. I think the code lends itself well to extension, but I may want to consider some documentation tools like Swagger that can programmatically create a documentation site for me as I add new endpoints and functionality.

#### Resiliency using Polly

Currently, the system is quite brittle when dealing with third party APIs. For example, if the PokeApi or translation Api returned a 500 error momentarily, the system would just deal with this as an error and return a response, but in a production app you may want more resiliency. Here I would make use of the [Polly](https://github.com/App-vNext/Polly) nuget package to easily allow those interactions to retry if certain conditions happen or fallback to other options if possible.

#### More versatile logging sink

In production I'd add a logging sink that could log out to a DB or something like AWS cloudwatch. Currently the app only logs to the console.

#### Rate Limiting

In production I'd add some kind of rate limiting functionality, public APIs such as this can be easily abused if they don't restrict usage. For the purposes of this challenge I didn't think it was necessary but it would be valuable in a real world scenario. I could do this by using [this nuget package](https://github.com/stefanprodan/AspNetCoreRateLimit) which adds middleware to the request pipeline that can reject requests early from IPs if they violate the rate limit. 

#### Common low level service between ITranslators

The translation functionality is split across a couple of services. The `ITranslationService` is used to abstract the rest away from the handlers. It is injected with a collection of `ITranslator`s. Currently, there are only two `ITranslator` implementations, the `YodaTranslator` and the `ShakespeareTranslator`. These two translators both have similar code, which could be considered a violation of DRY. In a production version, I would consider adding another service that both of these translators could use internally to avoid having this duplicate code. I did not do this in this challenge because I didn't want to add yet another service, which I thought would add a bit too much complexity.

#### Handle certain errors from PokeApi differently

When the project gets data from the PokeApi, it is possible this might fail somehow, the most common issue might be that the pokemon name was not recognised. In this case the PokeApi returns a 404. When the project sees this error it will return a response of the same status code to its caller. I think this could be improved slightly. Imagine a scenario where we hit some rate limit in the PokeApi, it would probably return a 429 response, but when the caller of our API sees this response they might think they've violated our rate limit, when in reality the API has violated the PokeApi's one. This might be confusing and I think could be improved by handling the response from the PokeApi differently for certain responses. I didn't do this in this project because I didn't want to have to handle multiple different cases and worry that I had missed out an important one, so I err'd on the side of simplicity and just returned the PokeApi's response directly. 

## Project structure overview

The solution is split into 4 projects, each with a specific role. This separation of concerns is based on the clean architecture approach. I like this approach as I think the split between the projects makes sense and lends itself well to extension.

### Domain

This project contain models, dtos, options, and "low" level interfaces and logic that would be used in various different parts of the application. This project is quite thin, only containing a few models and an `IUtcNowProvider`, which allows me to more easily mock a date time instead of using `DateTime.Now` and trying to fake it.

### Application

This project contains application logic, which is split into "vertical slices" using the [MediatR](https://github.com/jbogard/MediatR) nuget package. It is dependent on the `Domain` only. Here I define interfaces that are implemented by the `Infrastructure` project. For example, the `IPokemonService`, which actually gets the pokemon data.

Find the handlers for each endpoint in this project, in the `Queries` directory.

### Infrastructure

This project contains the services that interact with the public APIs. The `PokemonService` and `TranslationService` and `Translators` are implemented here. Each `Translator` provides functionality to change a Pokemon's description in a certain way. 

The `TranslationService` is injected with a collection of `ITranslator` implementations and when called upon to get a translated version, will go through them until it finds one that can translate the given pokemon info. It will then get the translated description using it. The order of the translators matters, and it is preserved from when they are registered in the DI container. This collection injection allows for the translation system to be extended without having to modify the existing code. You could just add a new `ITranslator`, which defines whether it can translate the info for a pokemon as well as how to actually translate it, then add it to the DI container.

### Presentation

This project exposes the application, and allows for registration of all services and the request pipeline. I have a single MVC controller `PokemonController` which enables API callers to make requests. This project depends on both the `Application` and `Infrastructure` projects. 

## My Approach

I first read through the specification fully, and wrote down my thoughts on bits that stood out to me. The translation logic would obviously be a big part of this application, so I set aside some time to think about how best to approach that. I also noticed the bonus points section, and so planned to preserve my git history from the start, and planned to use Docker.

Before writing any code, I spent some time interacting with the public APIs manually in Postman. I made a few requests to the PokeApi to see what the responses looked like and then did the same for each of the translations. I noticed from the funtranslations docs that they had a pretty low rate limit allowance, so I decided to implement some caching to allow for repeated requests to avoid using up that allowance.

I then thought about the translations implementation itself, I wanted to ensure it stuck to SOLID principles as best it could, and felt a simple set of if statements wouldn't cut it. If, once I was finished, I was asked to add another translation I would have to go in and modify those if statements, which violates the O of SOLID. To avoid this, I decided it was best to create a set of `ITranslator` implementations, each of which could tell if they can translate a given pokemon and then apply a translation to the description. This would allow me to extend the functionality by adding new implementors, which would not effect existing ones. I would then bring them together using an `ITranslationService` which could be injected with all the `ITranslator` implementations and abstract them away from the caller.

After I'd decided this, I got started and created an ASPNET Core Web API. I implemented the basic endpoint from end to end first, using a TDD style. I used MediatR to implement a "vertical slice" architecture and so started with the handler for this flow. 

When implementing a feature like this, I like to start with the handler. Any dependencies I will need start as just an interface for now, and while building the handler I will mock those dependencies out in tests. I like this approach because it allows me to think first about how I want to use a dependency and how its API should work. I'd then drill down into it to create it properly (and drilling down further if it has dependencies).

I then did the same for the Translation endpoint and made sure to reuse common code (such as the `IPokemonService`) between both endpoints.

Once I was happy with both endpoints, I decided to implement the caching. This took a few different shapes before I was happy with the implementation. I started by using the `IDistributedCache` in both the endpoint handlers, but didn't like this because there was a fair bit of duplicate code between them. Also, due to the mocking library I was using, I couldn't use the more friendly extension methods on the `IDistributedCache` and had to opt for serialising the data, then converting it to a `byte[]`.

I then attempted to use a MediatR behaviour implementation, which would allow me hook into the request pipeline that MediatR creates. However, this proved difficult as I had to use generic versions of the requests and responses and so could not easily serialise or deserialise the data.

The 3rd version simply wrapped the awkward `IDistributedCache` API in another service that I created, but this still left the handlers having to make use of it.

I finally settled on a version that made use of the [Scrutor](https://github.com/khellang/Scrutor) library, which allowed me to easily utilise the decorator pattern. I created implementations of the `IPokemonService` and `ITranslationService` which both check the `IDistributedCache` and return what is there (if there is anything), before trying the actual implementations respectively. I liked this because it meant I could remove all caching code from the handlers, and rely on the DI container to inject the cache version of each service instead.

