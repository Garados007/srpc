using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Cache;

namespace sRPCgen
{
    class Program
    {
        static string file;
        static string outputDir;
        static string namespaceBase;
        static string fileExtension;
        static bool verbose;

        static void Main(string[] args)
        {
            if (!ParseArgs(args))
            {
                PrintHelp();
                return;
            }

            outputDir ??= Environment.CurrentDirectory;
            namespaceBase ??= "";
            fileExtension ??= ".g.cs";

            if (verbose)
            {
                Console.WriteLine($"Protobuf descriptor: {file}");
                Console.WriteLine($"Output directory:    {outputDir}");
                Console.WriteLine($"Namespace base:      {namespaceBase}");
            }

            if (verbose)
                Console.WriteLine("Read protobuf descriptor...");
            FileDescriptorSet descriptor;
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            try { descriptor = FileDescriptorSet.Parser.ParseFrom(stream); }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return;
            }

            if (verbose)
                Console.WriteLine("Load type list...");
            List<NameInfo> names = new List<NameInfo>();
            foreach (var filedesc in descriptor.File)
                LoadTypes(filedesc, names);

            foreach (var filedesc in descriptor.File)
                foreach (var service in filedesc.Service)
                {
                    var targetName = filedesc.Options?.CsharpNamespace ?? "";
                    if (targetName.StartsWith(namespaceBase))
                        targetName = targetName.Substring(namespaceBase.Length);
                    else
                        Console.WriteLine($"the c# namespace {targetName} has not the base {namespaceBase}. The namespace will not be shortened.");
                    if (targetName != null)
                        targetName += '.';
                    targetName = (targetName + service.Name).Replace('.', '/');
                    if (targetName.StartsWith("/"))
                        targetName = targetName.Substring(1);
                    targetName = Path.Combine(outputDir + "/", targetName + fileExtension);
                    if (verbose)
                        Console.WriteLine($" Generate service for {service.Name} at {targetName}...");
                    var fi = new FileInfo(targetName);
                    if (!fi.Directory.Exists)
                        try { fi.Directory.Create(); }
                        catch (IOException e)
                        {
                            Console.Error.WriteLine($"Couldn't create directory for output file {targetName}: {e}");
                            return;
                        }
                    using var writer = new StreamWriter(new FileStream(targetName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite));
                    writer.BaseStream.SetLength(0);
                    GenerateServiceFile(filedesc, service, writer, names);
                    writer.Flush();
                }

            if (verbose)
                Console.WriteLine("Finish.");
        }

        static bool ParseArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    var ind = arg.IndexOf('=');
                    var command = ind == -1 ? arg : arg.Remove(ind + 1);
                    switch (command)
                    {
                        case "--file=":
                            if (file != null)
                            {
                                Console.WriteLine("--file is already defined");
                                return false;
                            }
                            file = arg.Substring(ind + 1);
                            break;
                        case "--output-dir=":
                            if (outputDir != null)
                            {
                                Console.WriteLine("--output-dir is already defined");
                                return false;
                            }
                            outputDir = arg.Substring(ind + 1);
                            break;
                        case "--namespace-base=":
                            if (namespaceBase != null)
                            {
                                Console.WriteLine("--namespace-base is already defined");
                                return false;
                            }
                            namespaceBase = arg.Substring(ind + 1);
                            break;
                        case "--file-extension=":
                            if (fileExtension != null)
                            {
                                Console.WriteLine("--file-extension is already defined");
                                return false;
                            }
                            fileExtension = arg.Substring(ind + 1);
                            break;
                        case "-h":
                        case "--help":
                            return false;
                        case "-v":
                        case "--verbose":
                            verbose = true;
                            break;
                        default:
                            Console.WriteLine($"unknown argument: {arg}");
                            return false;
                    }
                }
                else
                {
                    file = arg;
                }
            }
            if (file is null)
            {
                Console.WriteLine("no file specified");
                return false;
            }
            if (!File.Exists(file))
            {
                Console.WriteLine($"file not found: {file}");
                return false;
            }
            return true;
        }

        static void PrintHelp()
        {
            Console.Write(Properties.Resources.help);
        }
        
        static void LoadTypes(FileDescriptorProto descriptor, List<NameInfo> names)
        {
            _ = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _ = names ?? throw new ArgumentNullException(nameof(names));
            string protoPackage = descriptor.Package ?? "";
            string csharpNamespace = descriptor.Options?.CsharpNamespace ?? "";
            if (protoPackage != "")
                protoPackage += '.';
            if (csharpNamespace != "")
                csharpNamespace += ".";
            foreach (var type in descriptor.MessageType)
                LoadTypes(type, names, protoPackage, csharpNamespace);
        }

        static void LoadTypes(DescriptorProto descriptor, List<NameInfo> names,
            string protoPackage, string csharpNamespace)
        {
            names.Add(new NameInfo(
                name: descriptor.Name,
                protoBufName: $".{protoPackage}{descriptor.Name}",
                csharpName: $"{csharpNamespace}{descriptor.Name}"));
            foreach (var type in descriptor.NestedType)
                LoadTypes(type, names, $"{protoPackage}{descriptor.Name}.",
                    $"{csharpNamespace}{descriptor.Name}.");
        }

        static void GenerateServiceFile(
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
                $"#pragma warning disable CS0076, CS0612, CS1591, CS1998, CS3021",
                $"#region Designer generated code",
                $"",
                $"using gpw = global::Google.Protobuf.WellKnownTypes;",
                $"using s = global::System;",
                $"using srpc = global::sRPC;",
                $"using stt = global::System.Threading.Tasks;",
                $"",
                $"namespace {file.Options?.CsharpNamespace ?? "Api"}",
                $"{{",
                $"\t/// <summary>",
                $"\t/// The base class for the client implementation of the {service.Name} api",
                $"\t/// </summary>",
                $"\tpublic class {service.Name}ClientBase : srpc::IApiClientDefinition",
                $"\t{{",
                $"\t\tevent s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> srpc::IApiClientDefinition.PerformMessage",
                $"\t\t{{",
                $"\t\t\tadd => PerformMessagePrivate += value;",
                $"\t\t\tremove => PerformMessagePrivate -= value;",
                $"\t\t}}",
                $"",
                $"\t\tprivate event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> PerformMessagePrivate;"
            );
            foreach (var method in service.Method)
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
                    Console.Error.WriteLine($"c# type for protobuf message {method.InputType} not found");
                    return;
                }
                if (responseType is null)
                {
                    Console.Error.WriteLine($"c# type for protobuf message {method.OutputType} not found");
                    return;
                }
                writer.WriteLines(
                    $"",
                    $"\t\tpublic virtual async stt::Task<{responseType}> {method.Name}({requestType} message)",
                    $"\t\t{{",
                    $"\t\t\t_ = message ?? throw new s::ArgumentNullException(nameof(message));",
                    $"\t\t\tvar networkMessage = new srpc::NetworkRequest()",
                    $"\t\t\t{{",
                    $"\t\t\t\tApiFunction = \"{method.Name}\",",
                    $"\t\t\t\tRequest = gpw::Any.Pack(message),",
                    $"\t\t\t}};",
                    $"\t\t\tvar response = await PerformMessagePrivate?.Invoke(networkMessage);",
                    $"\t\t\treturn response.Response?.Unpack<{responseType}>();",
                    $"\t\t}}"
                );
            }
            writer.WriteLines(
                $"\t}}",
                $"",
                $"\t/// <summary>",
                $"\t/// The base class for the server implementation of the {service.Name} api",
                $"\t/// </summary>",
                $"\tpublic abstract class {service.Name}ServerBase : srpc::IApiServerDefinition",
                $"\t{{",
                $"\t\tasync stt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition.HandleMessage(srpc::NetworkRequest request)",
                $"\t\t{{",
                $"\t\t\t_ = request ?? throw new s::ArgumentNullException(nameof(request));",
                $"\t\t\tswitch (request.ApiFunction)",
                $"\t\t\t{{"
            );
            foreach (var method in service.Method)
            {
                var requestType = names
                    .Where(x => x.ProtoBufName == method.InputType)
                    .Select(x => x.CSharpName)
                    .FirstOrDefault();
                if (requestType is null)
                {
                    Console.Error.WriteLine($"c# type for protobuf message {method.InputType} not found");
                    return;
                }
                writer.WriteLines(
                    $"\t\t\t\tcase \"{method.Name}\":",
                    $"\t\t\t\t\treturn new srpc::NetworkResponse()",
                    $"\t\t\t\t\t{{",
                    $"\t\t\t\t\t\tResponse = gpw::Any.Pack(await {method.Name}(request.Request?.Unpack<{requestType}>())),",
                    $"\t\t\t\t\t\tToken = request.Token,",
                    $"\t\t\t\t\t}};"
                );
            }
            writer.WriteLines(
                $"\t\t\t\tdefault: throw new s::NotSupportedException($\"{{request.ApiFunction}} is not defined\");",
                $"\t\t\t}}",
                $"\t\t}}"
            );
            foreach (var method in service.Method)
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
                    Console.Error.WriteLine($"c# type for protobuf message {method.InputType} not found");
                    return;
                }
                if (responseType is null)
                {
                    Console.Error.WriteLine($"c# type for protobuf message {method.OutputType} not found");
                    return;
                }
                writer.WriteLines(
                    $"",
                    $"\t\tpublic abstract stt::Task<{responseType}> {method.Name}({requestType} request);"
                );
            }
            writer.WriteLines(
                $"\t}}",
                $"}}",
                $"",
                $"#endregion Designer generated code"
            );
        }
    }
}
