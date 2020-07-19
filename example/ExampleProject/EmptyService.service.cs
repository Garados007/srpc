// <auto-generated>
//     Generated by the sRPC compiler.  DO NOT EDIT!
//     source: empty_service.proto
// </auto-generated>
#pragma warning disable CS0076, CS0612, CS1591, CS1998, CS3021
#region Designer generated code

using gpw = global::Google.Protobuf.WellKnownTypes;
using s = global::System;
using srpc = global::sRPC;
using stt = global::System.Threading.Tasks;

namespace ExampleProject
{
    /// <summary>
    /// The base class for the client implementation of the EmptyService api
    /// </summary>
    public class EmptyServiceClientBase : srpc::IApiClientDefinition
    {
        event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> srpc::IApiClientDefinition.PerformMessage
        {
            add => PerformMessagePrivate += value;
            remove => PerformMessagePrivate -= value;
        }

        private event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> PerformMessagePrivate;
    }

    /// <summary>
    /// The base class for the server implementation of the EmptyService api
    /// </summary>
    public abstract class EmptyServiceServerBase : srpc::IApiServerDefinition
    {
        async stt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition.HandleMessage(srpc::NetworkRequest request)
        {
            _ = request ?? throw new s::ArgumentNullException(nameof(request));
            switch (request.ApiFunction)
            {
                default: throw new s::NotSupportedException($"{request.ApiFunction} is not defined");
            }
        }
    }
}

#endregion Designer generated code
