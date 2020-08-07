using Google.Protobuf.Reflection;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

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
        static string errorFormat;
        static bool emptySupport;
        static readonly List<string> ignoreUnwrap = new List<string>();

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
            errorFormat ??= "default";

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

        static void WriteWarning(string text, string errorKind = null, string code = null, string file = null)
        {
            file ??= Program.file ?? "sRPC";
            switch (errorFormat)
            {
                case "default":
                    Console.WriteLine(text);
                    break;
                case "msvs":
                    Console.Error.WriteLine($"{file} : {errorKind ?? ""} warning {code ?? ""}: {text}");
                    break;
            }
        }

        static void WriteError(string text, string errorKind = null, string code = null, string file = null)
        {
            file ??= Program.file ?? "sRPC";
            switch (errorFormat)
            {
                case "default":
                    Console.Error.WriteLine(text);
                    break;
                case "msvs":
                    Console.Error.WriteLine($"{file} : {errorKind ?? ""} error {code ?? ""}: {text}");
                    break;
            }
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
                        (errorFormat != "default" ? $"--error_format={errorFormat} " : "") +
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
                    WriteError(text: $"protoc exit with code {process.ExitCode} with {file}");
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
                WriteWarning(text: $"the c# namespace {targetName} has not the base {namespaceBase}. The namespace will not be shortened.",
                    file: filedesc.Name);
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
                    WriteError(text: $"Couldn't create directory for output file {targetName}: {e}",
                        file: filedesc.Name);
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
                        case "--error-format=":
                            if (errorFormat != null)
                            {
                                Console.WriteLine("--error-format is already defined");
                                return false;
                            }
                            switch (arg.Substring(ind + 1))
                            {
                                case "msvs": errorFormat = "msvs"; break;
                                case "default": errorFormat = "default"; break;
                                default: 
                                    Console.WriteLine($"unknown error format {arg.Substring(ind + 1)}"); 
                                    return false;
                            }
                            break;
                        case "--empty-support":
                            if (emptySupport)
                            {
                                Console.WriteLine("--empty-support is already defined");
                                return false;
                            }
                            emptySupport = true;
                            break;
                        case "--ignore-unwrap=":
                            {
                                var type = arg.Substring(ind + 1);
                                if (ignoreUnwrap.Contains(type))
                                {
                                    Console.WriteLine($"--ignore-unwrap already defined for {type}");
                                    return false;
                                }
                                ignoreUnwrap.Add($".{type}");
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
  --error-format=FORMAT     The format to print the errors. FORMAT may be
                            'default' or 'msvs' (Microsoft Visual Studio 
                            format). The format parameter is also set for
                            protoc.
  --ignore-unwrap=TYPE      sRPCgen will try to generate unwrapped client
                            Api call methods that will automaticly generate
                            the required request object. With this argument
                            certain protobuf request types are excluded
                            from this behaviour. It is required to define
                            the full protobuf name (include package).
                            If --empty-support is activated the type
                            google.protobuf.Empty is automaticly ignored.
  --empty-support           Add special support for google.protobuf.Empty
                            types.
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
            foreach (var @enum in descriptor.EnumType)
                LoadEnums(@enum, names, protoPackage, csharpNamespace);
        }

        static void LoadTypes(DescriptorProto descriptor, List<NameInfo> names,
            string protoPackage, string csharpNamespace)
        {
            names.Add(new NameInfo(
                descriptor: descriptor,
                name: descriptor.Name,
                protoBufName: $".{protoPackage}{descriptor.Name}",
                csharpName: $"{csharpNamespace}{descriptor.Name}"));
            foreach (var type in descriptor.NestedType)
                LoadTypes(type, names, $"{protoPackage}{descriptor.Name}.",
                    $"{csharpNamespace}{descriptor.Name}.");
            foreach (var @enum in descriptor.EnumType)
                LoadEnums(@enum, names, $"{protoPackage}{descriptor.Name}.",
                    $"{csharpNamespace}{descriptor.Name}.");
        }

        static void LoadEnums(EnumDescriptorProto descriptor, List<NameInfo> names,
            string protoPackage, string csharpNamespace)
        {
            names.Add(new NameInfo(
                descriptor: null,
                name: descriptor.Name,
                protoBufName: $".{protoPackage}{descriptor.Name}",
                csharpName: $"{csharpNamespace}{descriptor.Name}"));
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
                $"using gp = global::Google.Protobuf;",
                $"using gpw = global::Google.Protobuf.WellKnownTypes;",
                $"using s = global::System;",
                $"using scg = global::System.Collections.Generic;",
                $"using global::System.Linq;",
                $"using srpc = global::sRPC;",
                $"using st = global::System.Threading;",
                $"using stt = global::System.Threading.Tasks;",
                $"",
                $"namespace {file.Options?.CsharpNamespace ?? "Api"}",
                $"{{",
                $"\t/// <summary>",
                $"\t/// The base class for the client implementation of the {service.Name} api",
                $"\t/// </summary>",
                $"\tpublic class {service.Name}Client : srpc::IApiClientDefinition2",
                $"\t{{",
                $"\t\tevent s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> srpc::IApiClientDefinition.PerformMessage",
                $"\t\t{{",
                $"\t\t\tadd => PerformMessagePrivate += value;",
                $"\t\t\tremove => PerformMessagePrivate -= value;",
                $"\t\t}}",
                $"",
                $"\t\tevent s::Func<srpc::NetworkRequest, st::CancellationToken, stt::Task<srpc::NetworkResponse>> srpc::IApiClientDefinition2.PerformMessage2",
                $"\t\t{{",
                $"\t\t\tadd => PerformMessage2Private += value;",
                $"\t\t\tremove => PerformMessage2Private -= value;",
                $"\t\t}}",
                $"",
                $"\t\tprivate event s::Func<srpc::NetworkRequest, stt::Task<srpc::NetworkResponse>> PerformMessagePrivate;",
                $"",
                $"\t\tprivate event s::Func<srpc::NetworkRequest, st::CancellationToken, stt::Task<srpc::NetworkResponse>> PerformMessage2Private;"
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
                    WriteError(text: $"c# type for protobuf message {method.InputType} not found",
                        file: file.Name);
                    return;
                }
                if (responseType is null)
                {
                    WriteError(text: $"c# type for protobuf message {method.OutputType} not found",
                        file: file.Name);
                    return;
                }
                var resp = emptySupport && method.OutputType == ".google.protobuf.Empty"
                    ? ""
                    : $"<{responseType}>";
                var req = emptySupport && method.InputType == ".google.protobuf.Empty"
                    ? ""
                    : $"{requestType} message";
                var req2 = req == "" ? "" : $"{req}, ";
                writer.WriteLines(
                    $"",
                    $"\t\tpublic virtual stt::Task{resp} {method.Name}({req})",
                    $"\t\t{{",
                    req == "" ? null 
                        : $"\t\t\t_ = message ?? throw new s::ArgumentNullException(nameof(message));",
                    $"\t\t\treturn {method.Name}({(req == "" ? "" : "message, ")}st::CancellationToken.None);",
                    $"\t\t}}",
                    $"",
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
                    $"\t\t\t\t? await PerformMessage2Private.Invoke(networkMessage, cancellationToken)",
                    $"\t\t\t\t: await PerformMessagePrivate?.Invoke(networkMessage);",
                    resp == "" ? null
                        : $"\t\t\treturn response.Response?.Unpack<{responseType}>();",
                    $"\t\t}}",
                    $"",
                    $"\t\tpublic virtual async stt::Task{resp} {method.Name}({req2}s::TimeSpan timeout)",
                    $"\t\t{{",
                    req == "" ? null
                        : $"\t\t\t_ = message ?? throw new s::ArgumentNullException(nameof(message));",
                    $"\t\t\tif (timeout.Ticks < 0)",
                    $"\t\t\t\tthrow new s::ArgumentOutOfRangeException(nameof(timeout));",
                    $"\t\t\tusing var cancellationToken = new st::CancellationTokenSource(timeout);",
                    $"\t\t\t{(resp == "" ? "" : "return ")}await {method.Name}({(req == "" ? "" : "message, ")}cancellationToken.Token);",
                    $"\t\t}}"
                );
                if (req != "" && !ignoreUnwrap.Contains(method.InputType))
                {
                    var fields = GetRequestFields(method, names);
                    var write = new Action<(string optType, string optName)?>(par =>
                    {
                        writer.WriteLine();
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
                            writer.Write($"\t\t\t{type} {FirstLow(field)} = {defaultValue}");
                        }
                        writer.WriteLines(
                            $")",
                            $"\t\t{{",
                            $"\t\t\tvar request = new {requestType}",
                            $"\t\t\t{{"
                            );
                        foreach (var (field, _, _, converter) in fields)
                        {
                            writer.WriteLine($"\t\t\t\t{field} = {string.Format(converter, FirstLow(field))},");
                        }
                        writer.WriteLines(
                            $"\t\t\t}};",
                            $"\t\t\treturn {method.Name}(request{(par.HasValue ? $", {par.Value.optName}" : "")});",
                            $"\t\t}}"
                            );

                    });
                    write(null);
                    write(("st::CancellationToken", "cancellationToken"));
                    write(("s::TimeSpan", "timeout"));
                }
            }
            writer.WriteLines(
                $"\t}}",
                $"",
                $"\t/// <summary>",
                $"\t/// The base class for the server implementation of the {service.Name} api",
                $"\t/// </summary>",
                $"\tpublic abstract class {service.Name}ServerBase : srpc::IApiServerDefinition2",
                $"\t{{",
                $"\t\tstt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition.HandleMessage(srpc::NetworkRequest request)",
                $"\t\t\t=> ((srpc::IApiServerDefinition2)this).HandleMessage2(request, st::CancellationToken.None);",
                $"",
                $"\t\tasync stt::Task<srpc::NetworkResponse> srpc::IApiServerDefinition2.HandleMessage2(srpc::NetworkRequest request, st::CancellationToken cancellationToken)",
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
                    WriteError(text: $"c# type for protobuf message {method.InputType} not found",
                        file: file.Name);
                    return;
                }
                var resp = emptySupport && method.OutputType == ".google.protobuf.Empty";
                var req = emptySupport && method.InputType == ".google.protobuf.Empty"
                    ? ""
                    : $"request.Request?.Unpack<{requestType}>(), ";
                writer.WriteLines(
                    $"\t\t\t\tcase \"{method.Name}\":",
                    !resp ? null 
                        : $"\t\t\t\t\tawait {method.Name}({req}cancellationToken);",
                    $"\t\t\t\t\treturn new srpc::NetworkResponse()",
                    $"\t\t\t\t\t{{",
                    $"\t\t\t\t\t\tResponse = gpw::Any.Pack({(resp ? "new gpw::Empty()" : $"await {method.Name}({req}cancellationToken)")}),",
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
                    WriteError(text: $"c# type for protobuf message {method.InputType} not found",
                        file: file.Name);
                    return;
                }
                if (responseType is null)
                {
                    WriteError(text: $"c# type for protobuf message {method.OutputType} not found",
                        file: file.Name);
                    return;
                }
                var resp = emptySupport && method.OutputType == ".google.protobuf.Empty"
                    ? ""
                    : $"<{responseType}>";
                var req = emptySupport && method.InputType == ".google.protobuf.Empty"
                    ? ""
                    : $"{requestType} request";
                var req2 = req == "" ? "" : $"{req}, ";
                writer.WriteLines(
                    $"",
                    $"\t\tpublic abstract stt::Task{resp} {method.Name}({req});",
                    $"",
                    $"\t\tpublic virtual stt::Task{resp} {method.Name}({req2}st::CancellationToken cancellationToken)",
                    $"\t\t\t=> {method.Name}({(req == "" ? "" : "request")});"
                );
            }
            writer.WriteLines(
                $"\t}}",
                $"}}",
                $"",
                $"#endregion Designer generated code"
            );
        }

        static List<(string field, string type, string defaultValue, string converter)> GetRequestFields(
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
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "false" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new bool[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Bytes => 
                        ( "null"
                        , repeated 
                            ? "{{ {0}?.Select(x => gp::ByteString.CopyFrom(x ?? new byte[0])) ?? new gp::ByteString[0] }}" 
                            : "gp::ByteString.CopyFrom({0} ?? new byte[0])"
                        ),
                    FieldDescriptorProto.Types.Type.Double => 
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new double[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Enum => 
                        ( string.IsNullOrEmpty(field.DefaultValue)
                            ? $"({type})0"
                            : $"{type}.{ConvertName(field.DefaultValue, field.TypeName)}"
                        , repeated ? "{{ {0} ?? new " + type + "[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Fixed32 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new uint[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Fixed64 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new ulong[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Float =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new float[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Group => (null, null),
                    FieldDescriptorProto.Types.Type.Int32 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new int[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Int64 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new long[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Message => 
                        ( "null"
                        , repeated ? "{{ {0} ?? new " + type + "[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Sfixed32 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new int[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Sfixed64 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new long[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Sint32 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new int[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Sint64 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new long[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.String =>
                        ( Escape(field.DefaultValue)
                        , repeated ? "{{ {0}?.Select(x => x ?? \"\") ?? new string[0] }}" : "{0} ?? \"\""
                        ),
                    FieldDescriptorProto.Types.Type.Uint32 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new uint[0] }}" : "{0}"
                        ),
                    FieldDescriptorProto.Types.Type.Uint64 =>
                        ( string.IsNullOrEmpty(field.DefaultValue) ? "0" : field.DefaultValue
                        , repeated ? "{{ {0} ?? new ulong[0] }}" : "{0}"
                        ),
                    _ => ( null, null)
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
                result.Add((fieldName, type, defaultValue, converter));
            }

            return result;
        }

        static string GetCSharpName(List<NameInfo> names, string protoName)
            => names
                .Where(x => x.ProtoBufName == protoName)
                .Select(x => x.CSharpName)
                .FirstOrDefault();

        static string GetCSharpType(List<NameInfo> names, FieldDescriptorProto.Types.Type type, string typeName)
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

        static string Escape(string input)
        {
            using var writer = new StringWriter();
            using var provider = CodeDomProvider.CreateProvider("CSharp");
            provider.GenerateCodeFromExpression(
                new System.CodeDom.CodePrimitiveExpression(input),
                writer,
                null);
            return writer.ToString();
        }

        static string ConvertName(string name, string trim = null)
        {
            if (trim != null)
            {
                var ind = trim.LastIndexOf('.');
                if (ind >= 0)
                    trim = trim.Substring(ind + 1);
            }
            var sb = new StringBuilder();
            foreach (var p in name.Split(new [] { '_' }, StringSplitOptions.RemoveEmptyEntries))
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

        static string FirstLow(string name)
        {
            if (name == null || name.Length == 0)
                return name;
            return char.ToLower(name[0]) + name.Substring(1);
        }
    }
}
