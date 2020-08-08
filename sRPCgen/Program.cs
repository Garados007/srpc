using Google.Protobuf.Reflection;
using sRPCgen.Report;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace sRPCgen
{
    class Program
    {
        static readonly Settings settings = new Settings();
        static readonly Log log = new Log(settings);
        static ReportRegistry report;
        static ReportRegistry oldReport;

        static void Main(string[] args)
        {
            if (!settings.ParseArgs(args))
            {
                settings.PrintHelp();
                return;
            }

            settings.SetDefaults();
            if (settings.Report != null)
            {
                report = new ReportRegistry();
                oldReport = ReportRegistry.Load(settings.Report);
            }

            if (settings.Verbose)
            {
                Console.WriteLine($"Protobuf descriptor: {settings.File}");
                Console.WriteLine($"Output directory:    {settings.OutputDir}");
                Console.WriteLine($"Namespace base:      {settings.NamespaceBase}");
            }

            if (settings.SearchDir != null)
                WorkAtDir(settings.SearchDir);
            else WorkSingleFile(settings.File);

            if (oldReport != null && settings.RemoveWidowFiles)
            {
                if (settings.Verbose)
                    Console.WriteLine("searching for widow files");
                var remove = oldReport.Generateds.Select(x => x.File)
                    .Except(report.Generateds.Select(x => x.File));
                foreach (var file in remove)
                    if (File.Exists(file))
                        File.Delete(file);
            }

            report?.Save(settings.Report);

            if (settings.Verbose)
                Console.WriteLine("Finish.");
        }

        static void WorkAtDir(string dir)
        {
            foreach (var file in Directory.EnumerateFiles(dir, settings.BuildProtoc ? "*.proto" : "*.proto.bin"))
                WorkSingleFile(file);
            foreach (var sub in Directory.EnumerateDirectories(dir))
                WorkAtDir(sub);
        }

        static void WorkSingleFile(string file)
        {
            if (settings.BuildProtoc)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "protoc",
                    Arguments = new ArgsBuilder()
                        .Multi(settings.ProtoImports, 
                            (x, b) => b.Key('I', x))
                        .Key('o', $"{file}.bin")
                        .Key("csharp_out", settings.OutputDir)
                        .DictValue("csharp_opt", new Dictionary<string, string>
                        {
                            { "base_namespace", settings.NamespaceBase },
                            { "file_extension", settings.ProtoExtension }
                        })
                        .Key("error_format", settings.ErrorFormat, 
                            condition: settings.ErrorFormat != "default")
                        .Flag("include_imports")
                        .Key("dependency_out", $"{file}.dep", 
                            condition: report != null)
                        .File(file)
                        .ToString(),
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
                    log.WriteError(text: $"protoc exit with code {process.ExitCode} with {file}");
                    return;
                }
                var bin = $"{file}.bin";
                WorkSingleBinFile(bin);
                if (File.Exists(bin))
                    File.Delete(bin);
                if (report != null)
                {
                    var dep = $"{file}.dep";
                    var (rep, gen) = ProtoReport.ReadReport(dep, file);
                    if (rep != null)
                        report.Protos.Add(rep);
                    if (gen != null)
                        report.Generateds.Add(gen);
                    if (File.Exists(dep))
                        File.Delete(dep);
                }
            }
            else WorkSingleBinFile(file);
        }

        static void WorkSingleBinFile(string file)
        {
            if (settings.Verbose)
                Console.WriteLine($"Read protobuf descriptor of {file}...");
            FileDescriptorSet descriptor;
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            try { descriptor = FileDescriptorSet.Parser.ParseFrom(stream); }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return;
            }

            if (settings.Verbose)
                Console.WriteLine("Load type list...");
            List<NameInfo> names = new List<NameInfo>();
            foreach (var filedesc in descriptor.File)
                LoadTypes(filedesc, names);

            foreach (var filedesc in descriptor.File)
                foreach (var service in filedesc.Service)
                {
                    WorkSingleEntry(filedesc, service, names, file);
                }
        }

        static void WorkSingleEntry(FileDescriptorProto filedesc, ServiceDescriptorProto service, List<NameInfo> names, string sourceFile)
        {
            var targetName = filedesc.Options?.CsharpNamespace ?? "";
            if (targetName.StartsWith(settings.NamespaceBase))
                targetName = targetName.Substring(settings.NamespaceBase.Length);
            else
                log.WriteWarning(text: $"the c# namespace {targetName} has not the base {settings.NamespaceBase}. The namespace will not be shortened.",
                    file: filedesc.Name);
            if (targetName != null)
                targetName += '.';
            targetName = (targetName + service.Name).Replace('.', '/');
            if (targetName.StartsWith("/"))
                targetName = targetName.Substring(1);
            targetName = Path.Combine(settings.OutputDir + "/", targetName + settings.FileExtension);
            if (settings.Verbose)
                Console.WriteLine($" Generate service for {service.Name} at {targetName}...");
            var fi = new FileInfo(targetName);
            if (!fi.Directory.Exists)
                try { fi.Directory.Create(); }
                catch (IOException e)
                {
                    log.WriteError(text: $"Couldn't create directory for output file {targetName}: {e}",
                        file: filedesc.Name);
                    return;
                }
            using var writer = new StreamWriter(new FileStream(targetName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite));
            writer.BaseStream.SetLength(0);
            var generator = new Generator(settings, log);
            generator.GenerateServiceFile(filedesc, service, writer, names);
            writer.Flush();

            if (report != null)
            {
                if (settings.BuildProtoc)
                    sourceFile = sourceFile[0..^4];
                var gen = new GeneratedReport
                {
                    File = targetName,
                    Source = sourceFile,
                    LastBuild = new FileInfo(targetName).LastWriteTimeUtc,
                    Srpc = true
                };
                report.Generateds.Add(gen);
            }
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

    }
}
