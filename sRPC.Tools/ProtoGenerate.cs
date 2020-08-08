using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace sRPC.Tools
{
    public class ProtoGenerate : ToolTask
    {
        class ErrorListFilter
        {
            public Regex Pattern { get; set; }
            public Action<TaskLoggingHelper, Match> LogAction { get; set; }
        }

        static readonly TimeSpan s_regexTimeout = TimeSpan.FromMilliseconds(100);

        [Required]
        public ITaskItem[] Protobuf { get; set; }

        [Required]
        public string SrpcGenPath { get; set; }

        [Required]
        public string ProtocPath { get; set; }

        public string ProjectPath { get; set; }

        public string StandardImportsPath { get; set; }

        public string SrpcNamespaceBase { get; set; }

        public string SrpcSrpcExt { get; set; }

        public string SrpcProtoExt { get; set; }

        public string SrpcAutoSearchProj { get; set; }

        private bool IsSrpcAutoSearchProj => SrpcAutoSearchProj == "true";

        public string SrpcEmptySupport { get; set; }

        private bool IsSrpcEmptySupport => SrpcEmptySupport == "true";

        public string SrpcIgnoreUnwrap { get; set; }

        public string Report { get; set; }

        private string[] SrpcIgnoreUnwrapList
            => (SrpcIgnoreUnwrap ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Distinct()
                .ToArray();


        static readonly List<ErrorListFilter> s_errorListFilters = new List<ErrorListFilter>()
        {
            // Example warning with location
            //../Protos/greet.proto(19) : warning in column=5 : warning : When enum name is stripped and label is PascalCased (Zero),
            // this value label conflicts with Zero. This will make the proto fail to compile for some languages, such as C#.
            new ErrorListFilter
            {
                Pattern = new Regex(
                    pattern: "^(?'FILENAME'.+?)\\((?'LINE'\\d+)\\) ?: ?warning in column=(?'COLUMN'\\d+) ?: ?(?'TEXT'.*)",
                    options: RegexOptions.Compiled | RegexOptions.IgnoreCase,
                    matchTimeout: s_regexTimeout),
                LogAction = (log, match) =>
                {
                    int.TryParse(match.Groups["LINE"].Value, out var line);
                    int.TryParse(match.Groups["COLUMN"].Value, out var column);

                    log.LogWarning(
                        subcategory: null,
                        warningCode: null,
                        helpKeyword: null,
                        file: match.Groups["FILENAME"].Value,
                        lineNumber: line,
                        columnNumber: column,
                        endLineNumber: 0,
                        endColumnNumber: 0,
                        message: match.Groups["TEXT"].Value);
                }
            },

            // Example error with location
            //../Protos/greet.proto(14) : error in column=10: "name" is already defined in "Greet.HelloRequest".
            new ErrorListFilter
            {
                Pattern = new Regex(
                    pattern: "^(?'FILENAME'.+?)\\((?'LINE'\\d+)\\) ?: ?error in column=(?'COLUMN'\\d+) ?: ?(?'TEXT'.*)",
                    options: RegexOptions.Compiled | RegexOptions.IgnoreCase,
                    matchTimeout: s_regexTimeout),
                LogAction = (log, match) =>
                {
                    int.TryParse(match.Groups["LINE"].Value, out var line);
                    int.TryParse(match.Groups["COLUMN"].Value, out var column);

                    log.LogError(
                        subcategory: null,
                        errorCode: null,
                        helpKeyword: null,
                        file: match.Groups["FILENAME"].Value,
                        lineNumber: line,
                        columnNumber: column,
                        endLineNumber: 0,
                        endColumnNumber: 0,
                        message: match.Groups["TEXT"].Value);
                }
            },

            // Example warning without location
            //../Protos/greet.proto: warning: Import google/protobuf/empty.proto but not used.
            new ErrorListFilter
            {
                Pattern = new Regex(
                    pattern: "^(?'FILENAME'.+?): ?warning: ?(?'TEXT'.*)",
                    options: RegexOptions.Compiled | RegexOptions.IgnoreCase,
                    matchTimeout: s_regexTimeout),
                LogAction = (log, match) =>
                {
                    log.LogWarning(
                        subcategory: null,
                        warningCode: null,
                        helpKeyword: null,
                        file: match.Groups["FILENAME"].Value,
                        lineNumber: 0,
                        columnNumber: 0,
                        endLineNumber: 0,
                        endColumnNumber: 0,
                        message: match.Groups["TEXT"].Value);
                }
            },

            // Example error without location
            //../Protos/greet.proto: Import "google/protobuf/empty.proto" was listed twice.
            new ErrorListFilter
            {
                Pattern = new Regex(
                    pattern: "^(?'FILENAME'.+?): ?(?'TEXT'.*)",
                    options: RegexOptions.Compiled | RegexOptions.IgnoreCase,
                    matchTimeout: s_regexTimeout),
                LogAction = (log, match) =>
                {
                    log.LogError(
                        subcategory: null,
                        errorCode: null,
                        helpKeyword: null,
                        file: match.Groups["FILENAME"].Value,
                        lineNumber: 0,
                        columnNumber: 0,
                        endLineNumber: 0,
                        endColumnNumber: 0,
                        message: match.Groups["TEXT"].Value);
                }
            }
        };

        protected override string ToolName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "sRPCgen.exe" : "sRPCgen";

        protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.High;

        protected override bool ValidateParameters()
        {
            return !Log.HasLoggedErrors && base.ValidateParameters();
        }

        private void AddArg(StringBuilder sb, string name)
        {
            if (sb.Length > 0)
                sb.Append(" ");
            sb.Append($"--{name}");
        }

        private void AddArg(StringBuilder sb, string name, string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return;
            if (sb.Length > 0)
                sb.Append(" ");
            if (arg.Contains(" "))
                sb.Append($"\"--{name}={arg}\"");
            else sb.Append($"--{name}={arg}");
        }

        private void AddArg(StringBuilder sb, string name, string arg, string defaultArg)
            => AddArg(sb, name, string.IsNullOrEmpty(arg) ? defaultArg : arg);

        protected override string GenerateCommandLineCommands()
        {
            var sb = new StringBuilder();
            AddArg(sb, "output-dir", ProjectPath);
            AddArg(sb, "build-protoc");
            AddArg(sb, "proto-import", ProjectPath);
            AddArg(sb, "proto-import", StandardImportsPath);
            if (IsSrpcEmptySupport)
                AddArg(sb, "empty-support");
            if (IsSrpcAutoSearchProj)
            {
                AddArg(sb, "namespace-base", SrpcNamespaceBase);
                AddArg(sb, "file-extension", SrpcSrpcExt);
                AddArg(sb, "proto-extension", SrpcProtoExt);
                AddArg(sb, "search-dir", ProjectPath);
                AddArg(sb, "report", Report);
            }
            else
            {
                if (currentProtobuf != null)
                {
                    AddArg(sb, "namespace-base", currentProtobuf.GetMetadata(Metadata.NamespaceBase), SrpcNamespaceBase);
                    AddArg(sb, "file-extension", currentProtobuf.GetMetadata(Metadata.SrpcExt), SrpcSrpcExt);
                    AddArg(sb, "proto-extension", currentProtobuf.GetMetadata(Metadata.ProtoExt), SrpcProtoExt);
                    AddArg(sb, "file", currentProtobuf.ItemSpec);
                }
            }
            foreach (var ignore in SrpcIgnoreUnwrapList)
                AddArg(sb, "ignore-unwrap", ignore);
            return sb.ToString();
        }

        protected override string GenerateFullPathToTool() => SrpcGenPath;

        private ITaskItem currentProtobuf;

        protected override void LogToolCommand(string cmd)
        {
            var printer = new StringBuilder(1024);

            // Print 'str' slice into 'printer', wrapping in quotes if contains some
            // interesting characters in file names, or if empty string. The list of
            // characters requiring quoting is not by any means exhaustive; we are
            // just striving to be nice, not guaranteeing to be nice.
            var quotable = new[] { ' ', '!', '$', '&', '\'', '^' };
            void PrintQuoting(string str, int start, int count)
            {
                bool wrap = count == 0 || str.IndexOfAny(quotable, start, count) >= 0;
                if (wrap) printer.Append('"');
                printer.Append(str, start, count);
                if (wrap) printer.Append('"');
            }

            for (int ib = 0, ie; (ie = cmd.IndexOf('\n', ib)) >= 0; ib = ie + 1)
            {
                // First line only contains both the program name and the first switch.
                // We can rely on at least the '--out_dir' switch being always present.
                if (ib == 0)
                {
                    int iep = cmd.IndexOf(" --");
                    if (iep > 0)
                    {
                        PrintQuoting(cmd, 0, iep);
                        ib = iep + 1;
                    }
                }
                printer.Append(' ');
                if (cmd[ib] == '-')
                {
                    // Print switch unquoted, including '=' if any.
                    int iarg = cmd.IndexOf('=', ib, ie - ib);
                    if (iarg < 0)
                    {
                        // Bare switch without a '='.
                        printer.Append(cmd, ib, ie - ib);
                        continue;
                    }
                    printer.Append(cmd, ib, iarg + 1 - ib);
                    ib = iarg + 1;
                }
                // A positional argument or switch value.
                PrintQuoting(cmd, ib, ie - ib);
            }

            base.LogToolCommand(printer.ToString());
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            foreach (ErrorListFilter filter in s_errorListFilters)
            {
                Match match = filter.Pattern.Match(singleLine);

                if (match.Success)
                {
                    filter.LogAction(Log, match);
                    return;
                }
            }

            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }

        public override bool Execute()
        {
            UseCommandProcessor = false;

            if (EnvironmentVariables != null)
                EnvironmentVariables = EnvironmentVariables
                    .Select(x =>
                    {
                        if (x.StartsWith("PATH="))
                        {
                            return $"x;{new FileInfo(ProtocPath).Directory.FullName}";
                        }
                        else return x;
                    })
                    .ToArray();
            else EnvironmentVariables = new[]
            {
                $"PATH={new FileInfo(ProtocPath).Directory.FullName}",
            };

            if (IsSrpcAutoSearchProj)
            {
                var ok = base.Execute();
                if (!ok)
                    return false;
            }
            else
            {
                foreach (var task in Protobuf)
                {
                    currentProtobuf = task;
                    var compile = task.GetMetadata(Metadata.ProtoCompile).ToLowerInvariant();
                    if (compile == "false" || compile == "none")
                        continue;
                    var ok = base.Execute();
                    if (!ok)
                        return false;
                }
            }

            return true;
        }
    }
}
