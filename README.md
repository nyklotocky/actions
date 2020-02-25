# Introduction
This project was a programming exercise for interview purposes. It exposes a simple API to enable recording of "actions" and the "time" it took them to occur. Another endpoint returns a JSON response containing an average "time" for each distinct "action".

# Code structure
## Persistence layer
The data is stored in SQL, although in this instance it is SQL Express. There are two tables tracking the historical record of action times. The first table `dbo.actions`, serves as a means of providing a shorthand for the long names of the actions. Inserts into this table must be ensured to be atomic, as a reliable mapping from the action's name to a unique identifier must be ensured.

The second table `dbo.actionTimes` records each separate instance of a timed action. Averages are thus simply computed by aggregating the actions' timings.

Additional support for concurrency is also handled at this layer - any new timings or aggregation of existing timings is simply done on the rowset that is currently stored in the database.

## Web API
The Web layer is built in .NET Core, and exposes one GET endpoint `GetStats` to retrieve the statistics, and a POST endpoint to recieve new action timings. The thin controller makes a call into a corresponding business logic layer component that has been dependency injected into the controller. Because our endpoint allows for arbitrary text entry, we also defend against SQL injection in this layer.

# Run code
The machine running this application should have the .NET runtime and SQL Express installed. After cloning the repository, the application can be built and run with:
```
dotnet run --project Actions.Web\Actions.Web.csproj
```
After which, the API can be accessed via:
```
POST: http://localhost:5101/ActionsTiming/v1/AddAction
GET: http://localhost:5101/ActionsTiming/v1/GetStats
```

To run tests:
```
dotnet test
```

# Next Steps
While the intention of this project was to demonstrate a production-ready API, there remains more work to be done to where this would be a robust implementation ready for extensive and reliable internal or external use.
## Client Package
A client package is useful to simplify the interaction with the true web API. It can be used to abstract away operations such as serialization and deserialization, web calls, and would handle the various wiring up logic, such as registering implementations of internal components. For this package, it is important to consider the visibility and extensibility of each public component, so making appropriate use of visibility modifiers is a very important consideration. For instance - the component we wanted to release would be a `public abstract` class, with an `internal` constructor, thereby preventing arbitrary extension of the public-facing component, while retaining an abstraction contract of the supported functionality, allowing for swapping out the concrete implementation if needed.

## Additional testing coverage
I am used to having a decent degree more of testing tooling available. Having to reimplement/manually generate boilerplate slowed me down somewhat. As such, there should be some additional coverage:

### Web API
* Regression tests - Currently the testing coverage isolates the inidividual components' logic and ensures they work in a sandboxed environment. The next step would be to ensure that the components fit together well, as well as ensuring that the web endpoints themselves handle the calls appropriately.
* Backwards compatibility tests - there should be a series of tests that ensure all prior versions of the web endpoints continue to remain valid and correct. This would be done by retaining a series of tests calling into the endpoints, and adding to them as new versions are released. For instance, if a new version of `GetStats` were added to take an `action` filter parameter, both the original endpoint returning _all_ actions and their averages and the new filtered endpoint would need to work in perpetuity.

### Client package
* Unit tests - client package code can be somewhat tricky to unit test, as the "unit" at hand is effective serialization of the communication packet and the appropriate web call being emitted. An approach that works well here is mocking out the web request infrastructure and ensuring that the appropriate url is called and with the appropriately shaped payload. Similarly, upon recieving a manually constructed "response," ensuring that it can be processed accordingly.
* Backwards compatibility tests - a series of tests that capture the compile-time types in the prior versions of the client package. As the web API grows and changes, we might release additional versions of the client package. This could provide a new compile-time type, for example: an updated `GetStats` might begin to return a `StdDev` property. We would need to have a test to ensure that the deserialization was supported into both the old class that had only an `Avg` property as well as the new type that supported both properties.
* Integration tests - Ensuring the end to end functioning of each of the web calls and the serialization and deserialization of that communication is important.
* Registration and mockability tests - These are most useful when supporting internal consumers of the package; when you can make an assumption about the technology stack used by a consumer, you can ensure they are fully supported. Some examples:
	* Dependency injection - if you are aware of a consumer's IoC framework, you can provide an automatic registration of the appropriate internal components. This should be tested to ensure correctness
	* Mockability - A consumer of a binary dependency should be able to mock integration with the component without needing to shim a thin mockable wrapper in place. Knowing the desired mocking framework allows for opening up this capability (for instance, `Moq` would be supported by providing a assembly directive `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]`) and that capability should be tested to ensure that it works as expected.
