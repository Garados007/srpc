using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;

namespace sRPCgen.Docs
{
    public class DocFactory
    {
        protected readonly FileDescriptorSet descriptor;

        public DocFactory(FileDescriptorSet descriptor)
        {
            this.descriptor = descriptor;
        }

        public SourceCodeInfo.Types.Location GetDoc(int fileIndex, params int[] path)
        {
            if (fileIndex < 0 || fileIndex >= descriptor.File.Count || path == null)
                return null;
            var fileInfo = descriptor.File[fileIndex];
            if (fileInfo.SourceCodeInfo == null)
                return null;
            foreach (var loc in fileInfo.SourceCodeInfo.Location)
            {
                if (path.Length != loc.Path.Count)
                    continue;
                for (int i = 0; i < path.Length; ++i)
                    if (path[i] != loc.Path[i])
                        goto next;
                return loc;
                next:;
            }
            return null;
        }
    
        public SourceCodeInfo.Types.Location GetServiceDoc(int fileIndex, int serviceIndex)
            => GetDoc(fileIndex, 6, serviceIndex);
        
        public SourceCodeInfo.Types.Location GetServiceRpcDoc(int fileIndex, int serviceIndex, int rpcIndex)
            => GetDoc(fileIndex, 6, serviceIndex, 2, rpcIndex);

        public SourceCodeInfo.Types.Location GetMessageDoc(int fileIndex, int messageIndex)
            => GetDoc(fileIndex, 4, messageIndex);

        public int GetFileIndex(string name)
        {
            for (int i = 0; i < descriptor.File.Count; ++i)
                if (descriptor.File[i].Name == name)
                    return i;
            return -1;
        }

        public static IEnumerable<string> GetComment(SourceCodeInfo.Types.Location location)
        {
            if (location == null)
                yield break;
            foreach (var part in location.LeadingDetachedComments)
                yield return part.Trim();
            if (location.HasLeadingComments)
                yield return location.LeadingComments.Trim();
            if (location.HasTrailingComments)
                yield return location.TrailingComments.Trim();
        }

        public static IEnumerable<string> SplitNewLines(string line, int maxLength)
        {
            _ = line ?? throw new ArgumentNullException(nameof(line));
            if (maxLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            int start = 0;
            int position = 0;
            int lastWhitespace = 0;
            while (position < line.Length)
            {
                // do forced line wrap
                if (position - start >= maxLength)
                {
                    if (lastWhitespace <= start)
                    {
                        yield return line[start .. position];
                        start = position;
                    }
                    else
                    {
                        yield return line[start .. lastWhitespace];
                        start = lastWhitespace;
                    }
                    continue;
                }
                // check for line wrap
                if (line[position] == '\r' || line[position] == '\n')
                {
                    yield return line[start .. position];
                    // check if \r\n
                    if (line[position] == '\r' && position + 1 < line.Length && line[position] == '\n')
                        position++;
                    start = ++position;
                    continue;
                }
                // check for white space
                if (char.IsWhiteSpace(line[position]))
                    lastWhitespace = position;
                position++;
            }
            // return remaining
            if (start < line.Length)
                yield return line[start ..];
        }

    }
}