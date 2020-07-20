# sRPC

![.NET Core](https://github.com/Garados007/srpc/workflows/.NET%20Core/badge.svg?branch=master)

sRPC is an async ProtoBuf service interface that sends its messages directly (binary) on .NET Streams. This can be used to call rpc proto services directly with TCP Sockets or Names Pipes.

## Getting Started

This will add sRPC to your project so you can use this protocoll to create a RPC interface from your .proto files.

### Prerequisites

You need to have `protoc` installed on your system and added to your global PATH. This step depends on your system. For more information see at [https://github.com/protocolbuffers/protobuf/releases](https://github.com/protocolbuffers/protobuf/releases).

### Installing

First you need to clone this project:

```sh
git clone https://github.com/Garados007/srpc.git
```

Now you need to build the sRPC project. For this you need to change into the directory where `srpc.sln` is located and call this:

```sh
dotnet build --configuration Release
```

After that you need to add a dependency to your project. For this you need to open your `.csproj` file and add this:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/sRPC.csproj" />
</ItemGroup>
```

Now you need to add a build step that automaticaly generates the C# files for your .proto files. For this add this snippet to your `.csproj` file:

```xml
<Target Namwe="BuildProto" BeforeTargets="PreBuildEvent">
  <Exec Command="path/to/sRPCgen --search-dir=$(ProjectDir) --output-dir=$(ProjectDir) --namespace-base=$(TargetName) --file-extension=.service.cs --build-protoc --proto-import=$(ProjectDir) --proto-extension=.g.cs" />
</Target>
```

For more information about the sRPCgen arguments: [sRPCgen documentation](https://github.com/Garados007/srpc/wiki/sRPCgen)

Finally you need to add the NuGet Package `Google.Protobuf` to your project:

```sh
dotnet add package Google.Protobuf
```

Now your project is fully configured to use sRPC.

### Create and use API

First you need to specify your API in protobuf:

```protobuf
service SimpleService {
	rpc GetRandomNumber(RandonNumberRequest)
		returns (RandomNumberResponse);
}
```

After that you need to build your project once:

```sh
dotnet build
```

Now you need to create an implementation of the server Api base class:

```csharp
public class SimpleServiceServer : SimpleServiceServerBase
{
    public override Task<RandomNumberResponse> GetRandomNumber(RandonNumberRequest request)
    {
        // generate the random numbers and the response
    }
}
```

Finally you can create your server and client:

```csharp
// ### CLIENT ###

NetworkStream stream;
// create the client with the generated API implementation
using var client = new ApiClient<SimpleServiceClient>(stream);
// start the client. This will now send and listen to requests
client.Start();
// create your request
var request = new RandomNumberRequest
{
    Count = 5,
    MinValue = 0,
    MaxValue = 100,
};
// submit your request and await the response
var response = await client.Api.GetRandomNumber(request);
```
```csharp
// ### SERVER ###

NetworkStream stream;
// create the server with the implementation API
using var server = new ApiServer<SimpleServiceServer>(stream);
// start the server. This will now listen to requests and answer them
server.Start();
```

## Example

- [example/ExampleProject](example/ExampleProject)
	- Integration of sRPC into the project
	- examples of proto services
	- usage of the api

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Versioning

We use [SemVer](semver.org) for versioning. For the versions available, see the [tags on this repository](https://github.com/Garados007/srpc/tags).

## Authors

- **Max Brauer** - *Initial work* - [Garados007](https://github.com/Garados007)

See also the list of [contributors](https://github.com/Garados007/srpc/contributors) who participated in this project.

## Lincense

This project is licensed under the LGPL-2.1 License  - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

- StackOverflow for the help
- my friends for the inspiration
- [PurpleBooth](https://github.com/PurpleBooth) for his [README.md](https://gist.github.com/PurpleBooth/109311bb0361f32d87a2) template
