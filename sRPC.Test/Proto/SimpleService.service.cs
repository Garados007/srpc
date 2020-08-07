// <auto-generated>
//     Generated by the sRPC compiler.  DO NOT EDIT!
//     source: Proto/simple_service.proto
// </auto-generated>
#pragma warning disable CS0067, CS0076, CS0612, CS1591, CS1998, CS3021
#region Designer generated code

using gp = global::Google.Protobuf;
using gpw = global::Google.Protobuf.WellKnownTypes;
using s = global::System;
using scg = global::System.Collections.Generic;
using global::System.Linq;
using srpc = global::sRPC;
using st = global::System.Threading;
using stt = global::System.Threading.Tasks;

namespace sRPC.Test.Proto
{
    /// <summary>
    /// The base class for the client implementation of the SimpleService api
    /// </summary>
    public class SimpleServiceClient : srpc::IApiClientDefinition2
    {
        event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> srpc::IApiClientDefinition.PerformMessage
        {
            add => PerformMessagePrivate += value;
            remove => PerformMessagePrivate -= value;
        }

        event s::Func<srpc::NetworkRequest, st::CancellationToken, stt::Task<srpc::NetworkResponse>> srpc::IApiClientDefinition2.PerformMessage2
        {
            add => PerformMessage2Private += value;
            remove => PerformMessage2Private -= value;
        }

        private event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> PerformMessagePrivate;

        private event s::Func<srpc::NetworkRequest, st::CancellationToken, stt::Task<srpc::NetworkResponse>> PerformMessage2Private;

        public virtual stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(sRPC.Test.Proto.SqrtRequest message)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            return Sqrt(message, st::CancellationToken.None);
        }

        public virtual async stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(sRPC.Test.Proto.SqrtRequest message, st::CancellationToken cancellationToken)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            var networkMessage = new srpc::NetworkRequest()
            {
                ApiFunction = "Sqrt",
                Request = gpw::Any.Pack(message),
            };
            var response = PerformMessage2Private != null
                ? await PerformMessage2Private.Invoke(networkMessage, cancellationToken)
                : await PerformMessagePrivate?.Invoke(networkMessage);
            return response.Response?.Unpack<sRPC.Test.Proto.SqrtResponse>();
        }

        public virtual async stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(sRPC.Test.Proto.SqrtRequest message, s::TimeSpan timeout)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            if (timeout.Ticks < 0)
                throw new s::ArgumentOutOfRangeException(nameof(timeout));
            using var cancellationToken = new st::CancellationTokenSource(timeout);
            return await Sqrt(message, cancellationToken.Token);
        }

		public virtual stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(
			double value = 0)
        {
            var request = new sRPC.Test.Proto.SqrtRequest
            {
				Value = value,
            };
            return Sqrt(request);
        }

		public virtual stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(
			st::CancellationToken cancellationToken,
			double value = 0)
        {
            var request = new sRPC.Test.Proto.SqrtRequest
            {
				Value = value,
            };
            return Sqrt(request, cancellationToken);
        }

		public virtual stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(
			s::TimeSpan timeout,
			double value = 0)
        {
            var request = new sRPC.Test.Proto.SqrtRequest
            {
				Value = value,
            };
            return Sqrt(request, timeout);
        }

        public virtual stt::Task Indefinite()
        {
            return Indefinite(st::CancellationToken.None);
        }

        public virtual async stt::Task Indefinite(st::CancellationToken cancellationToken)
        {
            var networkMessage = new srpc::NetworkRequest()
            {
                ApiFunction = "Indefinite",
                Request = gpw::Any.Pack(new gpw::Empty()),
            };
            _ = PerformMessage2Private != null
                ? await PerformMessage2Private.Invoke(networkMessage, cancellationToken)
                : await PerformMessagePrivate?.Invoke(networkMessage);
        }

        public virtual async stt::Task Indefinite(s::TimeSpan timeout)
        {
            if (timeout.Ticks < 0)
                throw new s::ArgumentOutOfRangeException(nameof(timeout));
            using var cancellationToken = new st::CancellationTokenSource(timeout);
            await Indefinite(cancellationToken.Token);
        }
    }

    /// <summary>
    /// The base class for the server implementation of the SimpleService api
    /// </summary>
    public abstract class SimpleServiceServerBase : srpc::IApiServerDefinition2
    {
        stt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition.HandleMessage(srpc::NetworkRequest request)
            => ((srpc::IApiServerDefinition2)this).HandleMessage2(request, st::CancellationToken.None);

        async stt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition2.HandleMessage2(srpc::NetworkRequest request, st::CancellationToken cancellationToken)
        {
            _ = request ?? throw new s::ArgumentNullException(nameof(request));
            switch (request.ApiFunction)
            {
                case "Sqrt":
                    return new srpc::NetworkResponse()
                    {
                        Response = gpw::Any.Pack(await Sqrt(request.Request?.Unpack<sRPC.Test.Proto.SqrtRequest>(), cancellationToken)),
                        Token = request.Token,
                    };
                case "Indefinite":
                    await Indefinite(cancellationToken);
                    return new srpc::NetworkResponse()
                    {
                        Response = gpw::Any.Pack(new gpw::Empty()),
                        Token = request.Token,
                    };
                default: throw new s::NotSupportedException($"{request.ApiFunction} is not defined");
            }
        }

        public abstract stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(sRPC.Test.Proto.SqrtRequest request);

        public virtual stt::Task<sRPC.Test.Proto.SqrtResponse> Sqrt(sRPC.Test.Proto.SqrtRequest request, st::CancellationToken cancellationToken)
            => Sqrt(request);

        public abstract stt::Task Indefinite();

        public virtual stt::Task Indefinite(st::CancellationToken cancellationToken)
            => Indefinite();
    }
}

#endregion Designer generated code
