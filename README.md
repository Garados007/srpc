# sRPC

[![.NET Core](https://github.com/Garados007/srpc/workflows/.NET%20Core/badge.svg?branch=master)](https://github.com/Garados007/srpc/actions?query=workflow%3A%22.NET+Core%22) [![NuGet Publish](https://github.com/Garados007/srpc/workflows/NuGet%20Publish/badge.svg)](https://www.nuget.org/packages?q=Garados007+sRPC) [![License: LGPL v2.1](https://img.shields.io/badge/License-LGPL%20v2.1-blue.svg)](https://github.com/Garados007/srpc/blob/master/LICENSE) [![Current Version](https://img.shields.io/github/tag/garados007/srpc.svg?label=release)](https://github.com/Garados007/srpc/releases) ![Top Language](https://img.shields.io/github/languages/top/garados007/srpc.svg)

sRPC is an async ProtoBuf service interface that sends its messages directly (binary) on .NET Streams. This can be used to call rpc proto services directly with TCP Sockets or Names Pipes.

## Getting Started

This will add sRPC to your project so you can use this protocoll to create a RPC interface from your .proto files.

### Prerequisites

This package includes the latest Windows, Linux and MacOSX builds for [protoc](https://github.com/protocolbuffers/protobuf/releases).

In some cases it is recommended to install [protoc](https://github.com/protocolbuffers/protobuf/releases) on your own and add this to your global PATH.

### Installing

Add the `Google.Protobuf` package. This adds the managed support for Protocol Buffer itself:

```sh
dotnet add package Google.Protobuf
```

Then add the `sRPC` package. This adds the managed support for sRPC (sRPC remote procedure call):

```sh
dotnet add package sRPC
```

Now you need to add the `sRPC.Tools` package. This is a Protobuf and sRPC compiler. It also includes the system integration for your project:

```sh
dotnet add package sRPC.Tools
```

After that your project is fully configured to use sRPC.

### Create and use API

First you need to specify your API in protobuf:

```protobuf
service SimpleService {
	rpc GetRandomNumber(RandonNumberRequest)
		returns (RandomNumberResponse);
}
```

Now you need to set the `Build Action` in your File Properties to `Protobuf`. If you don't use Visual Studio you can edit your `.csproj` file and add this:

```xml
<ItemGroup>
  <None Remove="your\file.proto" />
  <Protobuf Include="your\file.proto" />
</ItemGroup>
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
// submit your request and await the response
var response = await client.Api.GetRandomNumber(count: 5, minValue: 0, maxValue: 100);
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
- [PurpleBooth](https://github.com/PurpleBooth) for her [README.md](https://gist.github.com/PurpleBooth/109311bb0361f32d87a2) template
- [gRPC](https://github.com/grpc/grpc/tree/master/src/csharp/Grpc.Tools) for their code for `gRPC.Tools`. This helped me a lot to create my own implementation of `sRPC.Tools`.
