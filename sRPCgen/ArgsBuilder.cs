using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sRPCgen
{
    class ArgsBuilder
    {
        private readonly StringBuilder buffer = new StringBuilder();

        public ArgsBuilder Key(char prefix, string value, bool condition = true)
        {
            if (!condition || !char.IsLetterOrDigit(prefix) || string.IsNullOrEmpty(value))
                return this;
            if (buffer.Length > 0)
                buffer.Append(" ");
            buffer.Append($"-{prefix}{value}");
            return this;
        }

        public ArgsBuilder Key(string key, string value, bool condition = true)
        {
            if (!condition || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return this;
            if (buffer.Length > 0)
                buffer.Append(" ");
            buffer.Append($"--{key}={value}");
            return this;
        }

        public ArgsBuilder Flag(char flag, bool condition = true)
        {
            if (!condition || !char.IsLetterOrDigit(flag))
                return this;
            if (buffer.Length > 0)
                buffer.Append(" ");
            buffer.Append($"-{flag}");
            return this;
        }

        public ArgsBuilder Flag(string flag, bool condition = true)
        {
            if (!condition || string.IsNullOrEmpty(flag))
                return this;
            if (buffer.Length > 0)
                buffer.Append(" ");
            buffer.Append($"--{flag}");
            return this;
        }

        public ArgsBuilder File(string file, bool condition = true)
        {
            if (!condition || string.IsNullOrEmpty(file))
                return this;
            if (buffer.Length > 0)
                buffer.Append(" ");
            buffer.Append($"\"{file}\"");
            return this;
        }

        public ArgsBuilder Multi<T>(IEnumerable<T> collection, Func<T, ArgsBuilder, ArgsBuilder> handler, bool condition = true)
        {
            if (!condition || collection is null || handler is null)
                return this;
            return collection.Aggregate(this, (x, y) => handler(y, x));
        }

        public ArgsBuilder DictValue(string key, IDictionary<string, string> values, bool condition = true)
        {
            if (!condition || string.IsNullOrEmpty(key) || values is null)
                return this;
            var value = string.Join(
                ",",
                values
                    .Where(x => !string.IsNullOrEmpty(x.Key) && !string.IsNullOrEmpty(x.Value))
                    .Select(x => $"{x.Key}={x.Value}")
                );
            return Key(key, value, condition: true);
        }

        public override string ToString()
            => buffer.ToString();
    }
}
