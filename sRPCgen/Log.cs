using System;

namespace sRPCgen
{
    class Log
    {
        public Settings Settings { get; }

        public Log(Settings settings)
            => Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public void WriteWarning(string text, string errorKind = null, string code = null, string file = null)
        {
            file ??= Settings.File ?? "sRPC";
            switch (Settings.ErrorFormat)
            {
                case "default":
                    Console.WriteLine(text);
                    break;
                case "msvs":
                    Console.Error.WriteLine($"{file} : {errorKind ?? ""} warning {code ?? ""}: {text}");
                    break;
            }
        }

        public void WriteError(string text, string errorKind = null, string code = null, string file = null)
        {
            file ??= Settings.File ?? "sRPC";
            switch (Settings.ErrorFormat)
            {
                case "default":
                    Console.Error.WriteLine(text);
                    break;
                case "msvs":
                    Console.Error.WriteLine($"{file} : {errorKind ?? ""} error {code ?? ""}: {text}");
                    break;
            }
        }

    }
}
