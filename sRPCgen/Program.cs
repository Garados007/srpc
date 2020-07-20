using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace sRPCgen
{
    class Program
    {
        static string file;
        static string outputDir;
        static string namespaceBase;
        static string fileExtension;
        static bool verbose;
        static bool buildProtoc;
        static string searchDir;
        static readonly List<string> protoImports = new List<string>();
        static string protoExtension;

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
            protoExtension ??= ".cs";

            if (verbose)
            {
                Console.WriteLine($"Protobuf descriptor: {file}");
                Console.WriteLine($"Output directory:    {outputDir}");
                Console.WriteLine($"Namespace base:      {namespaceBase}");
            }

            if (searchDir != null)
                WorkAtDir(searchDir);
            else WorkSingleFile(file);

            if (verbose)
                Console.WriteLine("Finish.");
        }

        static void WorkAtDir(string dir)
        {
            foreach (var file in Directory.EnumerateFiles(dir, buildProtoc ? "*.proto" : "*.proto.bin"))
                WorkSingleFile(file);
            foreach (var sub in Directory.EnumerateDirectories(dir))
                WorkAtDir(sub);
        }

        static void WorkSingleFile(string file)
        {
            if (buildProtoc)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "protoc",
                    Arguments = $"{string.Join(" ", protoImports.Select(x => $"-I{x}"))} " +
                        $"-o{file}.bin --csharp_out={outputDir} " +
                        $"--csharp_opt=base_namespace={namespaceBase},file_extension={protoExtension} " +
                        $"--include_imports {file}",
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                };
                using var process = Process.Start(startInfo);
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                        Console.WriteLine(e.Data);
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                        Console.Error.WriteLine(e.Data);
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine($"protoc exit with code {process.ExitCode} with {file}");
                    return;
                }
                var bin = $"{file}.bin";
                WorkSingleBinFile(bin);
                if (File.Exists(bin))
                    File.Delete(bin);
            }
            else WorkSingleBinFile(file);
        }

        static void WorkSingleBinFile(string file)
        {
            if (verbose)
                Console.WriteLine($"Read protobuf descriptor of {file}...");
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
                    WorkSingleEntry(filedesc, service, names);
                }
        }

        static void WorkSingleEntry(FileDescriptorProto filedesc, ServiceDescriptorProto service, List<NameInfo> names)
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
                            if (searchDir != null)
                            {
                                Console.WriteLine("--file cannot be used in --search-dir");
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
                        case "--build-protoc":
                            if (buildProtoc)
                            {
                                Console.WriteLine("--build-protoc is already defined");
                                return false;
                            }
                            buildProtoc = true;
                            break;
                        case "--proto-import=":
                            {
                                var path = arg.Substring(ind + 1);
                                if (protoImports.Contains(path))
                                {
                                    Console.WriteLine($"--proto-import already defined for {path}");
                                    return false;
                                }
                                if (!Directory.Exists(path))
                                {
                                    Console.WriteLine($"directory {path} doesn't exists as proto import");
                                    return false;
                                }
                                protoImports.Add(path);
                            }
                            break;
                        case "--proto-extension=":
                            if (protoExtension != null)
                            {
                                Console.WriteLine("--proto-extension is already defined");
                                return false;
                            }
                            protoExtension = arg.Substring(ind + 1);
                            break;
                        case "--search-dir=":
                            if (searchDir != null)
                            {
                                Console.WriteLine("--search-dir is already defined");
                                return false;
                            }
                            if (file != null)
                            {
                                Console.WriteLine("--search-dir cannot be used in combination with --file");
                                return false;
                            }
                            searchDir = arg.Substring(ind + 1);
                            if (!Directory.Exists(searchDir))
                            {
                                Console.WriteLine($"the search dir {searchDir} doesn't exists");
                                return false;
                            }
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
                    if (searchDir != null)
                    {
                        Console.WriteLine("a file cannot be used if --search-dir is used");
                        return false;
                    }
                    if (file != null)
                    {
                        Console.WriteLine("a file is already defined");
                        return false;
                    }
                    file = arg;
                }
            }
            if (file is null && searchDir is null)
            {
                Console.WriteLine("no file or search directory are specified");
                return false;
            }
            if (!(file is null || File.Exists(file)))
            {
                Console.WriteLine($"file not found: {file}");
                return false;
            }
            if (!buildProtoc)
            {
                if (protoImports.Count > 0)
                {
                    Console.WriteLine("--proto-import can only be used if --build-protoc is defined");
                    return false;
                }
                if (protoExtension != null)
                {
                    Console.WriteLine("--proto-extension can only be used if --build-protoc is defined");
                    return false;
                }
            }
            return true;
        }

        static void PrintHelp()
        {
            Console.Write(@"Usage: sRPCgen [OPTION] PROTO_BIN_FILE
Parse the PROTO_BIN_FILE and generate C# code for the rpc services based on 
the options given:
  --file=PROTO_BIN_FILE     Specify the descriptor file for the protobuf
                            implementation of the RPC interface. This file
                            has to contains all required imports.
                            This file can generated with protoc and the
                            --descriptor_set_out option.
                            This option is automatilcy set if a file name
                            is given.
  --search-dir=PROTO_DIR    Will search for a suitable files in the directory
                            and parse them. This is works as if you search for
                            each file and call this process. This cannot be
                            combined with --file.
                            This option expects the files to have the
                            extension .proto.bin
  --output-dir=OUT_DIR      The path where the generated C# source files has
                            to be placed. If not set the current working
                            directory is used.
  --namespace-base=NS_BASE  The base name for the output namespace. This is
                            only used to determine the output file name and
                            will shorten the path. If not set the whole
                            namespace name of the service will recreated
                            as directory structure.
  --file-extension=EXT      The extension to use for the C# files. Default
                            is .g.cs
  --build-protoc            --file and --search-dir will work directly with
                            the .protoc files. This expects to have the
                            protoc in the PATH environment variable.
                            --search-dir will now search for .proto files.
  --proto-import=IMP_DIR    Only if --build-protoc defined.
                            This will add the --proto_path=IMP_DIR argument
                            to the protoc call. IMP_DIR is the path of the
                            protoc import files. This argument can be defined
                            multiple times.
  --proto-extension=EXT     Only if --build-protoc defined.
                            This will add the file_extension=EXT to the
                            --csharp_opt argument to the protoc call.
                            EXT is the extension the C# file generated by
                            protoc should get. Default is .cs
  -v, --verbose             Print more information about the build process.
  -h, --help                Print this help.
");
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
                $"#pragma warning disable CS0067, CS0076, CS0612, CS1591, CS1998, CS3021",
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
                $"\tpublic class {service.Name}Client : srpc::IApiClientDefinition",
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
