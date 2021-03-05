using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace sRPCgen
{
    class Settings
    {
        public string File { get; set; }
        public string OutputDir { get; set; }
        public string NamespaceBase { get; set; }
        public string FileExtension { get; set; }

        public bool Verbose { get; set; }
        public bool BuildProtoc { get; set; }

        public string SearchDir { get; set; }
        public List<string> ProtoImports { get; } = new List<string>();
        public string ProtoExtension { get; set; }
        public string ErrorFormat { get; set; }
        public bool EmptySupport { get; set; }
        public List<string> IgnoreUnwrap { get; } = new List<string>();
        public string Report { get; set; }
        public bool RemoveWidowFiles { get; set; }
        public string OutputFormat { get; set; }
        public bool? Nullable { get; set; }

        static readonly string[] SupportedOutputFormats = new[]
        {
            // the default output format for version 1. This is the main output format.
            "1", 
            // the format that is introduced in 2.5.0. This has some changes in the server code
            "2",
        };

        public void SetDefaults()
        {
            OutputDir ??= Environment.CurrentDirectory;
            NamespaceBase ??= "";
            FileExtension ??= ".g.cs";
            ProtoExtension ??= ".cs";
            ErrorFormat ??= "default";
            OutputFormat ??= "1";
        }

        public bool ParseArgs(string[] args)
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
                            if (File != null)
                            {
                                Console.WriteLine("--file is already defined");
                                return false;
                            }
                            if (SearchDir != null)
                            {
                                Console.WriteLine("--file cannot be used in --search-dir");
                                return false;
                            }
                            File = arg.Substring(ind + 1);
                            break;
                        case "--output-dir=":
                            if (OutputDir != null)
                            {
                                Console.WriteLine("--output-dir is already defined");
                                return false;
                            }
                            OutputDir = arg.Substring(ind + 1);
                            break;
                        case "--namespace-base=":
                            if (NamespaceBase != null)
                            {
                                Console.WriteLine("--namespace-base is already defined");
                                return false;
                            }
                            NamespaceBase = arg.Substring(ind + 1);
                            break;
                        case "--file-extension=":
                            if (FileExtension != null)
                            {
                                Console.WriteLine("--file-extension is already defined");
                                return false;
                            }
                            FileExtension = arg.Substring(ind + 1);
                            break;
                        case "--build-protoc":
                            if (BuildProtoc)
                            {
                                Console.WriteLine("--build-protoc is already defined");
                                return false;
                            }
                            BuildProtoc = true;
                            break;
                        case "--proto-import=":
                            {
                                var path = arg.Substring(ind + 1);
                                if (ProtoImports.Contains(path))
                                {
                                    Console.WriteLine($"--proto-import already defined for {path}");
                                    return false;
                                }
                                if (!Directory.Exists(path))
                                {
                                    Console.WriteLine($"directory {path} doesn't exists as proto import");
                                    return false;
                                }
                                ProtoImports.Add(path);
                            }
                            break;
                        case "--proto-extension=":
                            if (ProtoExtension != null)
                            {
                                Console.WriteLine("--proto-extension is already defined");
                                return false;
                            }
                            ProtoExtension = arg.Substring(ind + 1);
                            break;
                        case "--search-dir=":
                            if (SearchDir != null)
                            {
                                Console.WriteLine("--search-dir is already defined");
                                return false;
                            }
                            if (File != null)
                            {
                                Console.WriteLine("--search-dir cannot be used in combination with --file");
                                return false;
                            }
                            SearchDir = arg.Substring(ind + 1);
                            if (!Directory.Exists(SearchDir))
                            {
                                Console.WriteLine($"the search dir {SearchDir} doesn't exists");
                                return false;
                            }
                            break;
                        case "--error-format=":
                            if (ErrorFormat != null)
                            {
                                Console.WriteLine("--error-format is already defined");
                                return false;
                            }
                            switch (arg.Substring(ind + 1))
                            {
                                case "msvs": ErrorFormat = "msvs"; break;
                                case "default": ErrorFormat = "default"; break;
                                default:
                                    Console.WriteLine($"unknown error format {arg.Substring(ind + 1)}");
                                    return false;
                            }
                            break;
                        case "--empty-support":
                            if (EmptySupport)
                            {
                                Console.WriteLine("--empty-support is already defined");
                                return false;
                            }
                            EmptySupport = true;
                            break;
                        case "--ignore-unwrap=":
                            {
                                var type = arg.Substring(ind + 1);
                                if (IgnoreUnwrap.Contains(type))
                                {
                                    Console.WriteLine($"--ignore-unwrap already defined for {type}");
                                    return false;
                                }
                                IgnoreUnwrap.Add($".{type}");
                            }
                            break;
                        case "--report=":
                            if (Report != null)
                            {
                                Console.WriteLine("--report is already defined");
                                return false;
                            }
                            Report = arg.Substring(ind + 1);
                            break;
                        case "--remove-widow-files":
                            if (RemoveWidowFiles)
                            {
                                Console.WriteLine("--remove-widow-files is already set");
                                return false;
                            }
                            RemoveWidowFiles = true;
                            break;
                        case "--output-format=":
                            if (OutputFormat != null)
                            {
                                Console.WriteLine("--output-format is already defined");
                                return false;
                            }
                            OutputFormat = arg.Substring(ind + 1);
                            if (!SupportedOutputFormats.Contains(OutputFormat))
                            {
                                Console.WriteLine("--output-format is not supported");
                                return false;
                            }
                            break;
                        case "--nullable=":
                            if (Nullable != null)
                            {
                                Console.WriteLine("--nullable is already defined");
                                return false;
                            }
                            switch (arg.Substring(ind + 1))
                            {
                                case "enable": Nullable = true; break;
                                case "disable": Nullable = false; break;
                                default:
                                    Console.WriteLine($"unknown nullable setting {arg[(ind + 1)..]}");
                                    return false;
                            }
                            break;
                        case "-h":
                        case "--help":
                            return false;
                        case "-v":
                        case "--verbose":
                            Verbose = true;
                            break;
                        default:
                            Console.WriteLine($"unknown argument: {arg}");
                            return false;
                    }
                }
                else
                {
                    if (SearchDir != null)
                    {
                        Console.WriteLine("a file cannot be used if --search-dir is used");
                        return false;
                    }
                    if (File != null)
                    {
                        Console.WriteLine("a file is already defined");
                        return false;
                    }
                    File = arg;
                }
            }
            if (File is null && SearchDir is null)
            {
                Console.WriteLine("no file or search directory are specified");
                return false;
            }
            if (!(File is null || System.IO.File.Exists(File)))
            {
                Console.WriteLine($"file not found: {File}");
                return false;
            }
            if (!BuildProtoc)
            {
                if (ProtoImports.Count > 0)
                {
                    Console.WriteLine("--proto-import can only be used if --build-protoc is defined");
                    return false;
                }
                if (ProtoExtension != null)
                {
                    Console.WriteLine("--proto-extension can only be used if --build-protoc is defined");
                    return false;
                }
            }
            if (SearchDir is null && Report != null)
            {
                Console.WriteLine("--report can only be defined if --search-dir is set");
                return false;
            }
            if (RemoveWidowFiles && Report is null)
            {
                Console.WriteLine("--remove-widow-files can only be used if --report is set");
                return false;
            }
            return true;
        }

        public void PrintHelp()
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
  --report=REPORT_FILE      Prints the building report in the specified file.
                            --report can only be defined if --search-dir
                            is used.
  --remove-widow-files      Searches for generated files which source does no
                            longer exists and delete them. This wil be done
                            after the complete generation step. For this the
                            report of the previous build is required.
                            This can only be used if --report is used.
  --output-format=FORMAT    Defines the format of the generated files. See
                            in the online wiki for more information about
                            the behaviour of the output formats.
  --nullable=NULLABLE       If set it enables the nullable configuration for
                            the output file. Only the values ""enable"" and
                            ""disable"" are allowed.
  -v, --verbose             Print more information about the build process.
  -h, --help                Print this help.
");
        }

    }
}
