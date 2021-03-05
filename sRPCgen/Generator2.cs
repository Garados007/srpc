using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace sRPCgen
{
    class Generator2 : Generator
    {
        public Generator2(Docs.ServiceDocFactory docs, Settings settings, Log log)
            : base(docs, settings, log)
        {

        }

        protected override void GenerateServerServiceMethods(
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
                : $"{requestType} request, ";
            writer.WriteLine();
            WriteMethodDoc(writer, false, false, names, method, methodIndex,
                ("request", "The api request object"),
                ("cancellationToken", "The token that signals the cancellation of the request")
            );
            writer.WriteLines(
                $"\t\tpublic abstract stt::Task{resp} {method.Name}({req}st::CancellationToken cancellationToken);"
            );
        }
    }
}
