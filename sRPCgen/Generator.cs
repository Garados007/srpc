﻿using Google.Protobuf.Reflection;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace sRPCgen
{
    class Generator
    {
        public Settings Settings { get; }
        
        public Log Log { get; }

        public Docs.ServiceDocFactory Doc { get; }

        public Generator(Docs.ServiceDocFactory docs, Settings settings, Log log)
        {
            Doc = docs ?? throw new ArgumentNullException(nameof(docs));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected virtual string Nullable => Settings.Nullable == true ? "?" : "";

        public virtual void GenerateServiceFile(
            FileDescriptorProto file,
            ServiceDescriptorProto service,
            StreamWriter writer,
            List<NameInfo> names)
        {
            writer.WriteLines(
                $"// <auto-generated>",
                $"// \tGenerated by the sRPC compiler.  DO NOT EDIT!",
                $"// \tsource: {file.Name}",
                $"// </auto-generated>",
                // CS0067: event is newer used. This is a false positive. The events are indeed used by sRPC
                // CS0612: obsolete is accessed (a strategy to work with these is missing)
                // CS1591: xml comment is missing for public member (this feature is in todo)
                $"#pragma warning disable CS0067, CS0612, CS1591",
                $"#region Designer generated code",
                $""
            );
            GenerateFileUsingsHeader(writer);
            if (Settings.Nullable != null)
                writer.WriteLines(
                    $"#nullable {(Settings.Nullable.Value ? "enable" : "disable")}",
                    $""
                );
            writer.WriteLines(
                $"namespace {file.Options?.CsharpNamespace ?? "Api"}",
                $"{{"
            );
            GenerateClientService(file, service, writer, names);
            GenerateServerService(file, service, writer, names);
            writer.WriteLines(
                $"}}",
                $"",
                $"#endregion Designer generated code"
            );
        }

        protected virtual void GenerateFileUsingsHeader(StreamWriter writer)
        {
            writer.WriteLines(
                $"using gp = global::Google.Protobuf;",
                $"using gpw = global::Google.Protobuf.WellKnownTypes;",
                $"using s = global::System;",
                $"using scg = global::System.Collections.Generic;",
                $"using global::System.Linq;",
                $"using srpc = global::sRPC;",
                $"using st = global::System.Threading;",
                $"using stt = global::System.Threading.Tasks;",
                $""
            );
        }

        protected virtual void GenerateClientService(
            FileDescriptorProto file,
            ServiceDescriptorProto service,
            StreamWriter writer,
            List<NameInfo> names)
        {
            writer.WriteLines(
                $"\t/// <summary>",
                $"\t/// The client of the {service.Name} api"
            );
            foreach (var line in Docs.DocFactory.GetComment(Doc.GetServiceDoc()))
            {
                writer.WriteLine($"\t/// <br/>");
                foreach (var part in Docs.DocFactory.SplitNewLines(line, 92))
                    writer.WriteLine($"\t/// {part}");
            }
            writer.WriteLines(
                $"\t/// </summary>",
                $"\tpublic class {service.Name}Client : srpc::IApiClientDefinition2",
                $"\t{{",
                $"\t\tevent s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>>{Nullable} srpc::IApiClientDefinition.PerformMessage",
                $"\t\t{{",
                $"\t\t\tadd => PerformMessagePrivate += value;",
                $"\t\t\tremove => PerformMessagePrivate -= value;",
                $"\t\t}}",
                $"",
                $"\t\tevent s::Func<srpc::NetworkRequest, st::CancellationToken, stt::Task<srpc::NetworkResponse>>{Nullable} srpc::IApiClientDefinition2.PerformMessage2",
                $"\t\t{{",
                $"\t\t\tadd => PerformMessage2Private += value;",
                $"\t\t\tremove => PerformMessage2Private -= value;",
                $"\t\t}}",
                $"",
                $"\t\tprivate event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>>{Nullable} PerformMessagePrivate;",
                $"",
                $"\t\tprivate event s::Func<srpc::NetworkRequest, st::CancellationToken, stt::Task<srpc::NetworkResponse>>{Nullable} PerformMessage2Private;"
            );
            for (int i = 0; i < service.Method.Count; ++i)
                GenerateClientServiceMethod(file, service, writer, names, service.Method[i], i);
            writer.WriteLines(
                $"\t}}",
                $"");
        }

        protected virtual void WriteMethodDoc(
            StreamWriter writer,
            bool isClient,
            bool unpack,
            List<NameInfo> names,
            MethodDescriptorProto method,
            int methodIndex,
            params (string name, string doc)?[] additional
        )
        {
            writer.WriteLines(
                $"\t\t/// <summary>",
                $"\t\t/// {(isClient ? "Client" : "Server")} call for {method.Name}"
            );
            foreach (var line in Docs.DocFactory.GetComment(Doc.GetServiceRpcDoc(methodIndex)))
            {
                writer.WriteLine($"\t\t/// <br/>");
                foreach (var part in Docs.DocFactory.SplitNewLines(line, 88))
                    writer.WriteLine($"\t\t/// {HttpUtility.HtmlEncode(part)}");
            }
            writer.WriteLine($"\t\t/// </summary>");
            if (unpack)
            {
                var fields = GetFieldsDoc(method, names);
                foreach (var (field, docs) in fields)
                {
                    if (field is null)
                        continue;
                    writer.WriteLine(
                        $"\t\t/// <param name=\"{HttpUtility.HtmlAttributeEncode(FirstLow(field))}\">"
                    );
                    var first = true;
                    foreach (var line in docs)
                    {
                        if (first) first = false;
                        else writer.WriteLine($"\t\t/// <br>/");
                        foreach (var part in Docs.DocFactory.SplitNewLines(line, 88))
                            writer.WriteLine($"\t\t/// {HttpUtility.HtmlEncode(part)}");
                    }
                    writer.WriteLine(
                        $"\t\t/// </param>"
                    );
                }
            }
            foreach (var add in additional)
                if (add != null)
                {
                    var (name, doc) = add.Value;
                    writer.WriteLines(
                        $"\t\t/// <param name=\"{HttpUtility.HtmlAttributeEncode(name)}\">",
                        $"\t\t/// {HttpUtility.HtmlEncode(doc)}",
                        $"\t\t/// </param>"
                    );
                }
            writer.WriteLine($"\t\t/// <returns>The result of the Api call</returns>");
        }

        protected virtual void GenerateClientServiceMethod(
            FileDescriptorProto file,
            ServiceDescriptorProto service,
            StreamWriter writer,
            List<NameInfo> names,
            MethodDescriptorProto method,
            int methodIndex)
        {
            var requestType = names
                .Where(x => x.ProtoBufName == method.InputType)
                .Select(x => x.CSharpName)
                .FirstOrDefault();
            var responseType = names
                .Where(x => x.ProtoBufName == method.OutputType)
                .Select(x => x.CSharpName)
                .FirstOrDefault();
            if (requestType is null)
            {
                Log.WriteError(text: $"c# type for protobuf message {method.InputType} not found",
                    file: file.Name);
                return;
            }
            if (responseType is null)
            {
                Log.WriteError(text: $"c# type for protobuf message {method.OutputType} not found",
                    file: file.Name);
                return;
            }
            var resp = Settings.EmptySupport && method.OutputType == ".google.protobuf.Empty"
                ? ""
                : $"<{responseType}{Nullable}>";
            var req = Settings.EmptySupport && method.InputType == ".google.protobuf.Empty"
                ? ""
                : $"{requestType} message";
            var req2 = req == "" ? "" : $"{req}, ";
            writer.WriteLine();
            WriteMethodDoc(writer, true, false, names, method, methodIndex,
                req == "" 
                    ? null
                    : ((string, string)?)("message", "request message")
            );
            writer.WriteLines(
                $"\t\tpublic virtual stt::Task{resp} {method.Name}({req})",
                $"\t\t{{",
                req == "" ? null
                    : $"\t\t\t_ = message ?? throw new s::ArgumentNullException(nameof(message));",
                $"\t\t\treturn {method.Name}({(req == "" ? "" : "message, ")}st::CancellationToken.None);",
                $"\t\t}}",
                $""
            );
            WriteMethodDoc(writer, true, false, names, method, methodIndex,
                req == "" 
                    ? null
                    : ((string, string)?)("message", "request message"),
                ("cancellationToken", "The token to cancel this request")
            );
            writer.WriteLines(
                $"\t\tpublic virtual async stt::Task{resp} {method.Name}({req2}st::CancellationToken cancellationToken)",
                $"\t\t{{",
                req == "" ? null
                    : $"\t\t\t_ = message ?? throw new s::ArgumentNullException(nameof(message));",
                $"\t\t\tvar networkMessage = new srpc::NetworkRequest()",
                $"\t\t\t{{",
                $"\t\t\t\tApiFunction = \"{method.Name}\",",
                $"\t\t\t\tRequest = gpw::Any.Pack({(req == "" ? "new gpw::Empty()" : "message")}),",
                $"\t\t\t}};",
                $"\t\t\t{(resp == "" ? "_" : "var response")} = PerformMessage2Private != null",
                $"\t\t\t\t? await PerformMessage2Private.Invoke(networkMessage, cancellationToken).ConfigureAwait(false)",
                $"\t\t\t\t: await (PerformMessagePrivate?.Invoke(networkMessage) ?? stt::Task.FromResult(new srpc::NetworkResponse())).ConfigureAwait(false);",
                resp == "" ? null
                    : $"\t\t\treturn response.Response?.Unpack<{responseType}{Nullable}>();",
                $"\t\t}}",
                $""
            );
            WriteMethodDoc(writer, true, false, names, method, methodIndex,
                req == "" 
                    ? null
                    : ((string, string)?)("message", "request message"),
                ("timeout", "The timeout after which the request should be cancelled")
            );
            writer.WriteLines(
                $"\t\tpublic virtual async stt::Task{resp} {method.Name}({req2}s::TimeSpan timeout)",
                $"\t\t{{",
                req == "" ? null
                    : $"\t\t\t_ = message ?? throw new s::ArgumentNullException(nameof(message));",
                $"\t\t\tif (timeout.Ticks < 0)",
                $"\t\t\t\tthrow new s::ArgumentOutOfRangeException(nameof(timeout));",
                $"\t\t\tusing var cancellationToken = new st::CancellationTokenSource(timeout);",
                $"\t\t\t{(resp == "" ? "" : "return ")}await {method.Name}({(req == "" ? "" : "message, ")}cancellationToken.Token).ConfigureAwait(false);",
                $"\t\t}}"
            );
            if (req != "" && !Settings.IgnoreUnwrap.Contains(method.InputType))
            {
                var fields = GetRequestFields(method, names);
                var write = new Action<(string optType, string optName, string optDesc)?>(par =>
                {
                    writer.WriteLine();
                    WriteMethodDoc(writer, true, true, names, method, methodIndex,
                        par == null ? null : 
                            ((string,string)?)(par.Value.optName, par.Value.optDesc)
                    );
                    writer.Write($"\t\tpublic virtual stt::Task{resp} {method.Name}(");
                    var first = true;
                    if (par != null)
                    {
                        first = false;
                        writer.WriteLine();
                        writer.Write($"\t\t\t{par.Value.optType} {par.Value.optName}");
                    }
                    foreach (var (field, type, defaultValue, _) in fields)
                    {
                        if (field is null || type is null || defaultValue is null)
                            continue;
                        if (first) first = false;
                        else writer.Write(",");
                        writer.WriteLine();
                        writer.Write($"\t\t\t{type} @{FirstLow(field)} = {defaultValue}");
                    }
                    writer.WriteLines(
                        $")",
                        $"\t\t{{",
                        $"\t\t\tvar request = new {requestType}",
                        $"\t\t\t{{"
                        );
                    foreach (var (field, _, _, converter) in fields)
                    {
                        writer.WriteLine($"\t\t\t\t{field} = {string.Format(converter, "@" + FirstLow(field))},");
                    }
                    writer.WriteLines(
                        $"\t\t\t}};",
                        $"\t\t\treturn {method.Name}(request{(par.HasValue ? $", {par.Value.optName}" : "")});",
                        $"\t\t}}"
                        );

                });
                write(null);
                write(("st::CancellationToken", "cancellationToken", "The token to cancel this request"));
                write(("s::TimeSpan", "timeout", "The timeout after which the request should be cancelled"));
            }
        }

        protected virtual void GenerateServerService(
            FileDescriptorProto file,
            ServiceDescriptorProto service,
            StreamWriter writer,
            List<NameInfo> names)
        {
            writer.WriteLines(
                $"\t/// <summary>",
                $"\t/// The base class for the server implementation of the {service.Name} api",
                $"\t/// </summary>",
                $"\tpublic abstract class {service.Name}ServerBase : srpc::IApiServerDefinition2",
                $"\t{{",
                $"\t\tstt::Task<srpc::NetworkResponse{Nullable}> srpc::IApiServerDefinition.HandleMessage(srpc::NetworkRequest request)",
                $"\t\t\t=> ((srpc::IApiServerDefinition2)this).HandleMessage2(request, st::CancellationToken.None);",
                $"",
                $"\t\tasync stt::Task<srpc::NetworkResponse{Nullable}> srpc::IApiServerDefinition2.HandleMessage2(srpc::NetworkRequest request, st::CancellationToken cancellationToken)",
                $"\t\t{{",
                $"\t\t\t_ = request ?? throw new s::ArgumentNullException(nameof(request));",
                $"\t\t\tswitch (request.ApiFunction)",
                $"\t\t\t{{"
            );
            foreach (var method in service.Method)
                GenerateServerServiceMessageHandler(file, service, writer, names, method);
            writer.WriteLines(
                $"\t\t\t\tdefault:",
                $"\t\t\t\t\tawait stt::Task.CompletedTask.ConfigureAwait(false);",
                $"\t\t\t\t\tthrow new s::NotSupportedException($\"{{request.ApiFunction}} is not defined\");",
                $"\t\t\t}}",
                $"\t\t}}"
            );
            for (int i = 0; i < service.Method.Count; ++i)
                GenerateServerServiceMethods(file, service, writer, names, service.Method[i], i);
            writer.WriteLines(
                $"\t}}"
            );
        }

        protected virtual void GenerateServerServiceMessageHandler(
            FileDescriptorProto file,
            ServiceDescriptorProto service,
            StreamWriter writer,
            List<NameInfo> names,
            MethodDescriptorProto method)
        {
            var requestType = names
                .Where(x => x.ProtoBufName == method.InputType)
                .Select(x => x.CSharpName)
                .FirstOrDefault();
            if (requestType is null)
            {
                Log.WriteError(text: $"c# type for protobuf message {method.InputType} not found",
                    file: file.Name);
                return;
            }
            var resp = Settings.EmptySupport && method.OutputType == ".google.protobuf.Empty";
            var reqc = Settings.EmptySupport && method.InputType == ".google.protobuf.Empty"
                ? "" 
                : $"req, ";
            writer.WriteLines(
                $"\t\t\t\tcase \"{method.Name}\":",
                $"\t\t\t\t{{"
            );
            if (reqc != null)
                writer.WriteLines(
                    $"\t\t\t\t\t\tvar req = request.Request?.Unpack<{requestType}{Nullable}>();",
                    $"\t\t\t\t\t\tif (req is null)",
                    $"\t\t\t\t\t\t\treturn new srpc::NetworkResponse()",
                    $"\t\t\t\t\t\t\t{{",
                    $"\t\t\t\t\t\t\t\tToken = request.Token,",
                    $"\t\t\t\t\t\t\t}};"
                );
            writer.WriteLines(
                !resp ? null
                    : $"\t\t\t\t\t\tawait {method.Name}({reqc}cancellationToken).ConfigureAwait(false);",
                $"\t\t\t\t\t\treturn new srpc::NetworkResponse()",
                $"\t\t\t\t\t\t{{",
                $"\t\t\t\t\t\t\tResponse = gpw::Any.Pack({(resp ? "new gpw::Empty()" : $"await {method.Name}({reqc}cancellationToken).ConfigureAwait(false)")}),",
                $"\t\t\t\t\t\t\tToken = request.Token,",
                $"\t\t\t\t\t\t}};",
                $"\t\t\t\t\t}}"
            );
        }

        protected virtual void GenerateServerServiceMethods(
            FileDescriptorProto file,
            ServiceDescriptorProto service,
            StreamWriter writer,
            List<NameInfo> names,
            MethodDescriptorProto method,
            int methodIndex)
        {
            var requestType = names
                .Where(x => x.ProtoBufName == method.InputType)
                .Select(x => x.CSharpName)
                .FirstOrDefault();
            var responseType = names
                .Where(x => x.ProtoBufName == method.OutputType)
                .Select(x => x.CSharpName)
                .FirstOrDefault();
            if (requestType is null)
            {
                Log.WriteError(text: $"c# type for protobuf message {method.InputType} not found",
                    file: file.Name);
                return;
            }
            if (responseType is null)
            {
                Log.WriteError(text: $"c# type for protobuf message {method.OutputType} not found",
                    file: file.Name);
                return;
            }
            var resp = Settings.EmptySupport && method.OutputType == ".google.protobuf.Empty"
                ? ""
                : $"<{responseType}{Nullable}>";
            var req = Settings.EmptySupport && method.InputType == ".google.protobuf.Empty"
                ? ""
                : $"{requestType} request";
            var req2 = req == "" ? "" : $"{req}, ";
            writer.WriteLine();
            WriteMethodDoc(writer, false, false, names, method, methodIndex,
                ("request", "The api request object")
            );
            writer.WriteLines(
                $"\t\tpublic abstract stt::Task{resp} {method.Name}({req});",
                $""
            );
            WriteMethodDoc(writer, false, false, names, method, methodIndex,
                ("request", "The api request object"),
                ("cancellationToken", "The token that signals the cancellation of the request")
            );
            writer.WriteLines(
                $"\t\tpublic virtual stt::Task{resp} {method.Name}({req2}st::CancellationToken cancellationToken)",
                $"\t\t\t=> {method.Name}({(req == "" ? "" : "request")});"
            );
        }

        protected virtual List<(string field, IEnumerable<string> doc)> GetFieldsDoc(
            MethodDescriptorProto method,
            List<NameInfo> names)
        {
            _ = method ?? throw new ArgumentNullException(nameof(method));
            _ = names ?? throw new ArgumentNullException(nameof(names));
            var result = new List<(string field, IEnumerable<string> doc)>();
            var request = names
                .Where(x => x.ProtoBufName == method.InputType)
                .FirstOrDefault();
            if (request is null || request.Descriptor is null)
                return result;

            for (int i = 0; i< request.Descriptor.Field.Count; ++i)
            {
                var field = request.Descriptor.Field[i];
                var fieldName = ConvertName(field.Name);
                if (fieldName.ToLower() == request.Name.ToLower())
                    fieldName += "_";
                var docLoc = request.Doc is Docs.IMessageDocFactory docFactory ?
                    docFactory.GetFieldDoc(i) : null;
                var docs = Docs.DocFactory.GetComment(docLoc);
                result.Add((fieldName, docs));
            }

            return result;
        }

        protected virtual List<(string field, string type, string defaultValue, string converter)> GetRequestFields(
            MethodDescriptorProto method,
            List<NameInfo> names)
        {
            _ = method ?? throw new ArgumentNullException(nameof(method));
            _ = names ?? throw new ArgumentNullException(nameof(names));
            var result = new List<(string field, string type, string defaultValue, string converter)>();
            var request = names
                .Where(x => x.ProtoBufName == method.InputType)
                .FirstOrDefault();
            if (request is null || request.Descriptor is null)
                return null;

            var getCSharpName = new Func<string, string>(protoName => names
                .Where(x => x.ProtoBufName == protoName)
                .Select(x => x.CSharpName)
                .FirstOrDefault());

            foreach (var field in request.Descriptor.Field)
            {
                var repeated = field.Label == FieldDescriptorProto.Types.Label.Repeated;
                var type = GetCSharpType(names, field.Type, field.TypeName);
                var (defaultValue, converter) = field.Type switch
                {
                    FieldDescriptorProto.Types.Type.Bool =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "false" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new bool[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Bytes =>
                        ("null"
                        , repeated
                            ? "{{ {0}?.Select(x => gp::ByteString.CopyFrom(x ?? new byte[0])) ?? new gp::ByteString[0] }}"
                            : "gp::ByteString.CopyFrom({0} ?? new byte[0])"
                        ),
                    FieldDescriptorProto.Types.Type.Double =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new double[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Enum =>
                        (string.IsNullOrEmpty(field.DefaultValue)
                            ? $"({type})0"
                            : $"{type}.{ConvertName(field.DefaultValue, field.TypeName)}"
                        , repeated ? "{{ {0} ?? new " + type + "[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Fixed32 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new uint[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Fixed64 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new ulong[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Float =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new float[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Group => (null, null),
                    FieldDescriptorProto.Types.Type.Int32 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new int[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Int64 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new long[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Message =>
                        ("null"
                        , repeated ? "{{ {0} ?? new " + type + "[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Sfixed32 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new int[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Sfixed64 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new long[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Sint32 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new int[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Sint64 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new long[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.String =>
                        (Escape(field.DefaultValue)
                        , repeated ? "{{ {0}?.Select(x => x ?? \"\") ?? new string[0] }}" : "{0} ?? \"\""
                        ),
                    FieldDescriptorProto.Types.Type.Uint32 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new uint[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Uint64 =>
                        (string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new ulong[0] }}" : "{0}"
                        ),
                    _ => (null, null)
                };
                if (repeated)
                {
                    type = type == null ? null : type + "[]";
                    defaultValue = "null";
                }
                if (field.TypeName.EndsWith("Entry"))
                {
                    var entry = names
                        .Where(x => x.ProtoBufName == field.TypeName)
                        .Where(x => x.Descriptor?.Options.MapEntry == true)
                        .FirstOrDefault();
                    if (entry != null)
                    {
                        var key = entry.Descriptor.Field
                            .Where(x => x.Name == "key")
                            .FirstOrDefault();
                        var value = entry.Descriptor.Field
                            .Where(x => x.Name == "value")
                            .FirstOrDefault();
                        var keyType = GetCSharpType(names, key.Type, key.TypeName);
                        var valueType = GetCSharpType(names, value.Type, value.TypeName);

                        type = $"scg::IDictionary<{keyType}, {valueType}>";
                        defaultValue = "null";
                        converter = "{{ {0} ?? new scg::Dictionary<" + keyType + ", " + valueType + ">() }}";
                    }
                }
                var fieldName = ConvertName(field.Name);
                if (fieldName.ToLower() == request.Name.ToLower())
                    fieldName += "_";
                if (defaultValue == "null")
                    type = $"{type}{Nullable}";
                result.Add((fieldName, type, defaultValue, converter));
            }

            return result;
        }
        protected virtual string GetCSharpName(List<NameInfo> names, string protoName)
            => names
                .Where(x => x.ProtoBufName == protoName)
                .Select(x => x.CSharpName)
                .FirstOrDefault();

        protected virtual string GetCSharpType(List<NameInfo> names, FieldDescriptorProto.Types.Type type, string typeName)
            => type switch
            {
                FieldDescriptorProto.Types.Type.Bool => "bool",
                FieldDescriptorProto.Types.Type.Bytes => "byte[]",
                FieldDescriptorProto.Types.Type.Double => "double",
                FieldDescriptorProto.Types.Type.Enum => GetCSharpName(names, typeName),
                FieldDescriptorProto.Types.Type.Fixed32 => "uint",
                FieldDescriptorProto.Types.Type.Fixed64 => "ulong",
                FieldDescriptorProto.Types.Type.Float => "float",
                FieldDescriptorProto.Types.Type.Group => null,
                FieldDescriptorProto.Types.Type.Int32 => "int",
                FieldDescriptorProto.Types.Type.Int64 => "long",
                FieldDescriptorProto.Types.Type.Message => GetCSharpName(names, typeName),
                FieldDescriptorProto.Types.Type.Sfixed32 => "int",
                FieldDescriptorProto.Types.Type.Sfixed64 => "long",
                FieldDescriptorProto.Types.Type.Sint32 => "int",
                FieldDescriptorProto.Types.Type.Sint64 => "long",
                FieldDescriptorProto.Types.Type.String => "string",
                FieldDescriptorProto.Types.Type.Uint32 => "uint",
                FieldDescriptorProto.Types.Type.Uint64 => "ulong",
                _ => null
            };

        protected virtual string Escape(string input)
        {
            using var writer = new StringWriter();
            using var provider = CodeDomProvider.CreateProvider("CSharp");
            provider.GenerateCodeFromExpression(
                new System.CodeDom.CodePrimitiveExpression(input),
                writer,
                null);
            return writer.ToString();
        }

        protected virtual string ConvertName(string name, string trim = null)
        {
            if (trim != null)
            {
                var ind = trim.LastIndexOf('.');
                if (ind >= 0)
                    trim = trim.Substring(ind + 1);
            }
            var sb = new StringBuilder();
            foreach (var p in name.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (trim?.ToLower() == p.ToLower())
                {
                    trim = null;
                    continue;
                }
                if (p.Length == 0)
                    continue;
                sb.Append(char.ToUpper(p[0]));
                if (p.Length > 1)
                    sb.Append(p.Substring(1).ToLower());
            }
            return sb.ToString();
        }

        protected virtual string FirstLow(string name)
        {
            if (name == null || name.Length == 0)
                return name;
            return char.ToLower(name[0]) + name.Substring(1);
        }

    }
}
