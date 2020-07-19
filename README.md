# sRPC

![.NET Core](https://github.com/Garados007/srpc/workflows/.NET%20Core/badge.svg?branch=master)

The RPC implementation for ProtoBuf usings .NET Streams.

sRPC sends all messages as ProtoBuf packages on any Stream. There is no extra protocol with additional overhead (like gRPC) used.

## Transmission protocol

Each message has a 4 byte header with an int32 (lowest byte first) encoded length of the message itself. After that a ProtoBuf message like this is sent:

```protobuf
message NetworkRequest {
	// the name of the api function to request
	string api_function = 1;
	// the request object with the data
	google.protobuf.Any request = 2;
	// the token to identify the response with the request
	int64 token = 3;
}
```

the `api_function` defines the RPC call. All arguments are packed in a single `google.protobuf.Any` field. To reference the response with the request a single `token` is used. This implementation increment this token with each request but there is not need to do so. Tokens can be reused but the same token should never be used twice at the same time.

The response has the a 4 byte header with an int32 (lowest byte first) encoded length and after that the message itself:

```protobuf
message NetworkResponse {
	// the response object with the data
	google.protobuf.Any response = 2;
	// the token to identify this response
	int64 token = 3;
}
```

## Create the sRPC Api

First you need to specify your rpc API in protobuf:

```protobuf
service SimpleService {
	rpc GetRandomNumber(RandonNumberRequest)
		returns (RandomNumberResponse);
}
```

After that you need to create the descriptor information of this `.proto` file:

```sh
protoc \
    --proto_path=path/of/your/imports \
    --descriptor_set_out=path/of/new/descriptor/file \
    --include_imports \
    path/to/your/proto/file
```

It is important to include the parameter `--include_imports` if you included definitions from other files in your `.proto`. Otherwise the generator won't find the the references.

The descriptor file contains all information about your `.proto` file and their definitions and imports. This file is only used for the next step.

```sh
sRPCgen path/of/descriptor/file
```

This will generate the server and client base implementation of your API with the default settings.

After that you need to add the reference to `sRPC` to your project (maybe there is a NuGet Package in future) and add the generated files.

Now you need to create an implementation of you the abstract base class for the server.

## Use the API

For the client implementation you only need to create the client handler and start it. After that you can use the API.

```csharp
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

The server needs first to implement the API base class.

```csharp
public class SimpleServiceServer : SimpleServiceServerBase
{
    public override Task<RandomNumberResponse> GetRandomNumber(RandonNumberRequest request)
    {
        // generate the random numbers and the response
    }
}
```

And now the server can be created. For this the server handler needs to created and started. After that it can serve the requests:

```csharp
NetworkStream stream;
// create the server with the implementation API
using var server = new ApiServer<SimpleServiceServer>(stream);
// start the client. This will now listen to requests and answer them
server.Start();
```
