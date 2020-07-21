// <auto-generated>
//     Generated by the sRPC compiler.  DO NOT EDIT!
//     source: Proto/simple_service.proto
// </auto-generated>
#pragma warning disable CS0067, CS0076, CS0612, CS1591, CS1998, CS3021
#region Designer generated code

using gpw = global::Google.Protobuf.WellKnownTypes;
using s = global::System;
using srpc = global::sRPC;
using stt = global::System.Threading.Tasks;

namespace sRPC.Test.Proto
{
    /// <summary>
    /// The base class for the client implementation of the SimpleService api
    /// </summary>
    public class SimpleServiceClient : srpc::IApiClientDefinition
    {
        event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> srpc::IApiClientDefinition.PerformMessage
        {
            add => PerformMessagePrivate += value;
            remove => PerformMessagePrivate -= value;
        }

        private event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> PerformMessagePrivate;

        public virtual async stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(sRPC.Test.Proto.SqrtRequest message)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            var networkMessage = new srpc::NetworkRequest()
            {
                ApiFunction = "Sqrt",
                Request = gpw::Any.Pack(message),
            };
            var response = await PerformMessagePrivate?.Invoke(networkMessage);
            return response.Response?.Unpack<sRPC.Test.Proto.SqrtResponse>();
        }
    }

    /// <summary>
    /// The base class for the server implementation of the SimpleService api
    /// </summary>
    public abstract class SimpleServiceServerBase : srpc::IApiServerDefinition
    {
        async stt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition.HandleMessage(srpc::NetworkRequest request)
        {
            _ = request ?? throw new s::ArgumentNullException(nameof(request));
            switch (request.ApiFunction)
            {
                case "Sqrt":
                    return new srpc::NetworkResponse()
                    {
                        Response = gpw::Any.Pack(await Sqrt(request.Request?.Unpack<sRPC.Test.Proto.SqrtRequest>())),
                        Token = request.Token,
                    };
                default: throw new s::NotSupportedException($"{request.ApiFunction} is not defined");
            }
        }

        public abstract stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(sRPC.Test.Proto.SqrtRequest request);
    }
}

#endregion Designer generated code
