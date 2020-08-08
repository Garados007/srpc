// <auto-generated>
//     Generated by the sRPC compiler.  DO NOT EDIT!
//     source: Proto/build_test_service.proto
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
    /// The base class for the client implementation of the BuildTestService api
    /// </summary>
    public class BuildTestServiceClient : srpc::IApiClientDefinition2
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

        public virtual stt::Task TestMultiFields(sRPC.Test.Proto.MultiFields message)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            return TestMultiFields(message, st::CancellationToken.None);
        }

        public virtual async stt::Task TestMultiFields(sRPC.Test.Proto.MultiFields message, st::CancellationToken cancellationToken)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            var networkMessage = new srpc::NetworkRequest()
            {
                ApiFunction = "TestMultiFields",
                Request = gpw::Any.Pack(message),
            };
            _ = PerformMessage2Private != null
                ? await PerformMessage2Private.Invoke(networkMessage, cancellationToken)
                : await PerformMessagePrivate?.Invoke(networkMessage);
        }

        public virtual async stt::Task TestMultiFields(sRPC.Test.Proto.MultiFields message, s::TimeSpan timeout)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            if (timeout.Ticks < 0)
                throw new s::ArgumentOutOfRangeException(nameof(timeout));
            using var cancellationToken = new st::CancellationTokenSource(timeout);
            await TestMultiFields(message, cancellationToken.Token);
        }

		public virtual stt::Task TestMultiFields(
			bool @boolFlag = false,
			byte[] @bytesValue = null,
			double @doubleValue = 0,
			sRPC.Test.Proto.EnumObject @enumValue = (sRPC.Test.Proto.EnumObject)0,
			uint @fixed32Value = 0,
			ulong @fixed64Value = 0,
			float @floatValue = 0,
			int @int32Value = 0,
			long @int64Value = 0,
			Google.Protobuf.WellKnownTypes.Timestamp @messageValue = null,
			int @sfixed32Value = 0,
			long @sfixed64Value = 0,
			int @sint32Value = 0,
			long @sint64Value = 0,
			string @stringValue = "",
			uint @uint32Value = 0,
			ulong @uint64Value = 0,
			bool[] @repeatedBoolFlag = null,
			byte[][] @repeatedBytesValue = null,
			double[] @repeatedDoubleValue = null,
			sRPC.Test.Proto.EnumObject[] @repeatedEnumValue = null,
			uint[] @repeatedFixed32Value = null,
			ulong[] @repeatedFixed64Value = null,
			float[] @repeatedFloatValue = null,
			int[] @repeatedInt32Value = null,
			long[] @repeatedInt64Value = null,
			Google.Protobuf.WellKnownTypes.Timestamp[] @repeatedMessageValue = null,
			int[] @repeatedSfixed32Value = null,
			long[] @repeatedSfixed64Value = null,
			int[] @repeatedSint32Value = null,
			long[] @repeatedSint64Value = null,
			string[] @repeatedStringValue = null,
			uint[] @repeatedUint32Value = null,
			ulong[] @repeatedUint64Value = null,
			scg::IDictionary<int, string> @map1 = null,
			int @short = 0,
			int @foreach = 0,
			int @var = 0,
			int @null = 0,
			int @is = 0)
        {
            var request = new sRPC.Test.Proto.MultiFields
            {
				BoolFlag = @boolFlag,
				BytesValue = gp::ByteString.CopyFrom(@bytesValue ?? new byte[0]),
				DoubleValue = @doubleValue,
				EnumValue = @enumValue,
				Fixed32Value = @fixed32Value,
				Fixed64Value = @fixed64Value,
				FloatValue = @floatValue,
				Int32Value = @int32Value,
				Int64Value = @int64Value,
				MessageValue = @messageValue,
				Sfixed32Value = @sfixed32Value,
				Sfixed64Value = @sfixed64Value,
				Sint32Value = @sint32Value,
				Sint64Value = @sint64Value,
				StringValue = @stringValue ?? "",
				Uint32Value = @uint32Value,
				Uint64Value = @uint64Value,
				RepeatedBoolFlag = { @repeatedBoolFlag ?? new bool[0] },
				RepeatedBytesValue = { @repeatedBytesValue?.Select(x => gp::ByteString.CopyFrom(x ?? new byte[0])) ?? new gp::ByteString[0] },
				RepeatedDoubleValue = { @repeatedDoubleValue ?? new double[0] },
				RepeatedEnumValue = { @repeatedEnumValue ?? new sRPC.Test.Proto.EnumObject[0] },
				RepeatedFixed32Value = { @repeatedFixed32Value ?? new uint[0] },
				RepeatedFixed64Value = { @repeatedFixed64Value ?? new ulong[0] },
				RepeatedFloatValue = { @repeatedFloatValue ?? new float[0] },
				RepeatedInt32Value = { @repeatedInt32Value ?? new int[0] },
				RepeatedInt64Value = { @repeatedInt64Value ?? new long[0] },
				RepeatedMessageValue = { @repeatedMessageValue ?? new Google.Protobuf.WellKnownTypes.Timestamp[0] },
				RepeatedSfixed32Value = { @repeatedSfixed32Value ?? new int[0] },
				RepeatedSfixed64Value = { @repeatedSfixed64Value ?? new long[0] },
				RepeatedSint32Value = { @repeatedSint32Value ?? new int[0] },
				RepeatedSint64Value = { @repeatedSint64Value ?? new long[0] },
				RepeatedStringValue = { @repeatedStringValue?.Select(x => x ?? "") ?? new string[0] },
				RepeatedUint32Value = { @repeatedUint32Value ?? new uint[0] },
				RepeatedUint64Value = { @repeatedUint64Value ?? new ulong[0] },
				Map1 = { @map1 ?? new scg::Dictionary<int, string>() },
				Short = @short,
				Foreach = @foreach,
				Var = @var,
				Null = @null,
				Is = @is,
            };
            return TestMultiFields(request);
        }

		public virtual stt::Task TestMultiFields(
			st::CancellationToken cancellationToken,
			bool @boolFlag = false,
			byte[] @bytesValue = null,
			double @doubleValue = 0,
			sRPC.Test.Proto.EnumObject @enumValue = (sRPC.Test.Proto.EnumObject)0,
			uint @fixed32Value = 0,
			ulong @fixed64Value = 0,
			float @floatValue = 0,
			int @int32Value = 0,
			long @int64Value = 0,
			Google.Protobuf.WellKnownTypes.Timestamp @messageValue = null,
			int @sfixed32Value = 0,
			long @sfixed64Value = 0,
			int @sint32Value = 0,
			long @sint64Value = 0,
			string @stringValue = "",
			uint @uint32Value = 0,
			ulong @uint64Value = 0,
			bool[] @repeatedBoolFlag = null,
			byte[][] @repeatedBytesValue = null,
			double[] @repeatedDoubleValue = null,
			sRPC.Test.Proto.EnumObject[] @repeatedEnumValue = null,
			uint[] @repeatedFixed32Value = null,
			ulong[] @repeatedFixed64Value = null,
			float[] @repeatedFloatValue = null,
			int[] @repeatedInt32Value = null,
			long[] @repeatedInt64Value = null,
			Google.Protobuf.WellKnownTypes.Timestamp[] @repeatedMessageValue = null,
			int[] @repeatedSfixed32Value = null,
			long[] @repeatedSfixed64Value = null,
			int[] @repeatedSint32Value = null,
			long[] @repeatedSint64Value = null,
			string[] @repeatedStringValue = null,
			uint[] @repeatedUint32Value = null,
			ulong[] @repeatedUint64Value = null,
			scg::IDictionary<int, string> @map1 = null,
			int @short = 0,
			int @foreach = 0,
			int @var = 0,
			int @null = 0,
			int @is = 0)
        {
            var request = new sRPC.Test.Proto.MultiFields
            {
				BoolFlag = @boolFlag,
				BytesValue = gp::ByteString.CopyFrom(@bytesValue ?? new byte[0]),
				DoubleValue = @doubleValue,
				EnumValue = @enumValue,
				Fixed32Value = @fixed32Value,
				Fixed64Value = @fixed64Value,
				FloatValue = @floatValue,
				Int32Value = @int32Value,
				Int64Value = @int64Value,
				MessageValue = @messageValue,
				Sfixed32Value = @sfixed32Value,
				Sfixed64Value = @sfixed64Value,
				Sint32Value = @sint32Value,
				Sint64Value = @sint64Value,
				StringValue = @stringValue ?? "",
				Uint32Value = @uint32Value,
				Uint64Value = @uint64Value,
				RepeatedBoolFlag = { @repeatedBoolFlag ?? new bool[0] },
				RepeatedBytesValue = { @repeatedBytesValue?.Select(x => gp::ByteString.CopyFrom(x ?? new byte[0])) ?? new gp::ByteString[0] },
				RepeatedDoubleValue = { @repeatedDoubleValue ?? new double[0] },
				RepeatedEnumValue = { @repeatedEnumValue ?? new sRPC.Test.Proto.EnumObject[0] },
				RepeatedFixed32Value = { @repeatedFixed32Value ?? new uint[0] },
				RepeatedFixed64Value = { @repeatedFixed64Value ?? new ulong[0] },
				RepeatedFloatValue = { @repeatedFloatValue ?? new float[0] },
				RepeatedInt32Value = { @repeatedInt32Value ?? new int[0] },
				RepeatedInt64Value = { @repeatedInt64Value ?? new long[0] },
				RepeatedMessageValue = { @repeatedMessageValue ?? new Google.Protobuf.WellKnownTypes.Timestamp[0] },
				RepeatedSfixed32Value = { @repeatedSfixed32Value ?? new int[0] },
				RepeatedSfixed64Value = { @repeatedSfixed64Value ?? new long[0] },
				RepeatedSint32Value = { @repeatedSint32Value ?? new int[0] },
				RepeatedSint64Value = { @repeatedSint64Value ?? new long[0] },
				RepeatedStringValue = { @repeatedStringValue?.Select(x => x ?? "") ?? new string[0] },
				RepeatedUint32Value = { @repeatedUint32Value ?? new uint[0] },
				RepeatedUint64Value = { @repeatedUint64Value ?? new ulong[0] },
				Map1 = { @map1 ?? new scg::Dictionary<int, string>() },
				Short = @short,
				Foreach = @foreach,
				Var = @var,
				Null = @null,
				Is = @is,
            };
            return TestMultiFields(request, cancellationToken);
        }

		public virtual stt::Task TestMultiFields(
			s::TimeSpan timeout,
			bool @boolFlag = false,
			byte[] @bytesValue = null,
			double @doubleValue = 0,
			sRPC.Test.Proto.EnumObject @enumValue = (sRPC.Test.Proto.EnumObject)0,
			uint @fixed32Value = 0,
			ulong @fixed64Value = 0,
			float @floatValue = 0,
			int @int32Value = 0,
			long @int64Value = 0,
			Google.Protobuf.WellKnownTypes.Timestamp @messageValue = null,
			int @sfixed32Value = 0,
			long @sfixed64Value = 0,
			int @sint32Value = 0,
			long @sint64Value = 0,
			string @stringValue = "",
			uint @uint32Value = 0,
			ulong @uint64Value = 0,
			bool[] @repeatedBoolFlag = null,
			byte[][] @repeatedBytesValue = null,
			double[] @repeatedDoubleValue = null,
			sRPC.Test.Proto.EnumObject[] @repeatedEnumValue = null,
			uint[] @repeatedFixed32Value = null,
			ulong[] @repeatedFixed64Value = null,
			float[] @repeatedFloatValue = null,
			int[] @repeatedInt32Value = null,
			long[] @repeatedInt64Value = null,
			Google.Protobuf.WellKnownTypes.Timestamp[] @repeatedMessageValue = null,
			int[] @repeatedSfixed32Value = null,
			long[] @repeatedSfixed64Value = null,
			int[] @repeatedSint32Value = null,
			long[] @repeatedSint64Value = null,
			string[] @repeatedStringValue = null,
			uint[] @repeatedUint32Value = null,
			ulong[] @repeatedUint64Value = null,
			scg::IDictionary<int, string> @map1 = null,
			int @short = 0,
			int @foreach = 0,
			int @var = 0,
			int @null = 0,
			int @is = 0)
        {
            var request = new sRPC.Test.Proto.MultiFields
            {
				BoolFlag = @boolFlag,
				BytesValue = gp::ByteString.CopyFrom(@bytesValue ?? new byte[0]),
				DoubleValue = @doubleValue,
				EnumValue = @enumValue,
				Fixed32Value = @fixed32Value,
				Fixed64Value = @fixed64Value,
				FloatValue = @floatValue,
				Int32Value = @int32Value,
				Int64Value = @int64Value,
				MessageValue = @messageValue,
				Sfixed32Value = @sfixed32Value,
				Sfixed64Value = @sfixed64Value,
				Sint32Value = @sint32Value,
				Sint64Value = @sint64Value,
				StringValue = @stringValue ?? "",
				Uint32Value = @uint32Value,
				Uint64Value = @uint64Value,
				RepeatedBoolFlag = { @repeatedBoolFlag ?? new bool[0] },
				RepeatedBytesValue = { @repeatedBytesValue?.Select(x => gp::ByteString.CopyFrom(x ?? new byte[0])) ?? new gp::ByteString[0] },
				RepeatedDoubleValue = { @repeatedDoubleValue ?? new double[0] },
				RepeatedEnumValue = { @repeatedEnumValue ?? new sRPC.Test.Proto.EnumObject[0] },
				RepeatedFixed32Value = { @repeatedFixed32Value ?? new uint[0] },
				RepeatedFixed64Value = { @repeatedFixed64Value ?? new ulong[0] },
				RepeatedFloatValue = { @repeatedFloatValue ?? new float[0] },
				RepeatedInt32Value = { @repeatedInt32Value ?? new int[0] },
				RepeatedInt64Value = { @repeatedInt64Value ?? new long[0] },
				RepeatedMessageValue = { @repeatedMessageValue ?? new Google.Protobuf.WellKnownTypes.Timestamp[0] },
				RepeatedSfixed32Value = { @repeatedSfixed32Value ?? new int[0] },
				RepeatedSfixed64Value = { @repeatedSfixed64Value ?? new long[0] },
				RepeatedSint32Value = { @repeatedSint32Value ?? new int[0] },
				RepeatedSint64Value = { @repeatedSint64Value ?? new long[0] },
				RepeatedStringValue = { @repeatedStringValue?.Select(x => x ?? "") ?? new string[0] },
				RepeatedUint32Value = { @repeatedUint32Value ?? new uint[0] },
				RepeatedUint64Value = { @repeatedUint64Value ?? new ulong[0] },
				Map1 = { @map1 ?? new scg::Dictionary<int, string>() },
				Short = @short,
				Foreach = @foreach,
				Var = @var,
				Null = @null,
				Is = @is,
            };
            return TestMultiFields(request, timeout);
        }

        public virtual stt::Task TestIdentical(sRPC.Test.Proto.Identical message)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            return TestIdentical(message, st::CancellationToken.None);
        }

        public virtual async stt::Task TestIdentical(sRPC.Test.Proto.Identical message, st::CancellationToken cancellationToken)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            var networkMessage = new srpc::NetworkRequest()
            {
                ApiFunction = "TestIdentical",
                Request = gpw::Any.Pack(message),
            };
            _ = PerformMessage2Private != null
                ? await PerformMessage2Private.Invoke(networkMessage, cancellationToken)
                : await PerformMessagePrivate?.Invoke(networkMessage);
        }

        public virtual async stt::Task TestIdentical(sRPC.Test.Proto.Identical message, s::TimeSpan timeout)
        {
            _ = message ?? throw new s::ArgumentNullException(nameof(message));
            if (timeout.Ticks < 0)
                throw new s::ArgumentOutOfRangeException(nameof(timeout));
            using var cancellationToken = new st::CancellationTokenSource(timeout);
            await TestIdentical(message, cancellationToken.Token);
        }

		public virtual stt::Task TestIdentical(
			byte[] @identical_ = null)
        {
            var request = new sRPC.Test.Proto.Identical
            {
				Identical_ = gp::ByteString.CopyFrom(@identical_ ?? new byte[0]),
            };
            return TestIdentical(request);
        }

		public virtual stt::Task TestIdentical(
			st::CancellationToken cancellationToken,
			byte[] @identical_ = null)
        {
            var request = new sRPC.Test.Proto.Identical
            {
				Identical_ = gp::ByteString.CopyFrom(@identical_ ?? new byte[0]),
            };
            return TestIdentical(request, cancellationToken);
        }

		public virtual stt::Task TestIdentical(
			s::TimeSpan timeout,
			byte[] @identical_ = null)
        {
            var request = new sRPC.Test.Proto.Identical
            {
				Identical_ = gp::ByteString.CopyFrom(@identical_ ?? new byte[0]),
            };
            return TestIdentical(request, timeout);
        }
    }

    /// <summary>
    /// The base class for the server implementation of the BuildTestService api
    /// </summary>
    public abstract class BuildTestServiceServerBase : srpc::IApiServerDefinition2
    {
        stt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition.HandleMessage(srpc::NetworkRequest request)
            => ((srpc::IApiServerDefinition2)this).HandleMessage2(request, st::CancellationToken.None);

        async stt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition2.HandleMessage2(srpc::NetworkRequest request, st::CancellationToken cancellationToken)
        {
            _ = request ?? throw new s::ArgumentNullException(nameof(request));
            switch (request.ApiFunction)
            {
                case "TestMultiFields":
                    await TestMultiFields(request.Request?.Unpack<sRPC.Test.Proto.MultiFields>(), cancellationToken);
                    return new srpc::NetworkResponse()
                    {
                        Response = gpw::Any.Pack(new gpw::Empty()),
                        Token = request.Token,
                    };
                case "TestIdentical":
                    await TestIdentical(request.Request?.Unpack<sRPC.Test.Proto.Identical>(), cancellationToken);
                    return new srpc::NetworkResponse()
                    {
                        Response = gpw::Any.Pack(new gpw::Empty()),
                        Token = request.Token,
                    };
                default: throw new s::NotSupportedException($"{request.ApiFunction} is not defined");
            }
        }

        public abstract stt::Task TestMultiFields(sRPC.Test.Proto.MultiFields request);

        public virtual stt::Task TestMultiFields(sRPC.Test.Proto.MultiFields request, st::CancellationToken cancellationToken)
            => TestMultiFields(request);

        public abstract stt::Task TestIdentical(sRPC.Test.Proto.Identical request);

        public virtual stt::Task TestIdentical(sRPC.Test.Proto.Identical request, st::CancellationToken cancellationToken)
            => TestIdentical(request);
    }
}

#endregion Designer generated code
