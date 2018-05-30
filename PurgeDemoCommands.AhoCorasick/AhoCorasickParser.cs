using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ganss.Text;
using MoreLinq;
using PurgeDemoCommands.AhoCorasick.Logging;
using PurgeDemoCommands.Core;

namespace PurgeDemoCommands.AhoCorasick
{
    public class AhoCorasickParser : IParser
    {
        private static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        private Ganss.Text.AhoCorasick _ahoCorasick;
        private int _commandCount;

        public AhoCorasickParser(string[] value)
        {
            _commandCount = value.Length;

            _ahoCorasick = new Ganss.Text.AhoCorasick(value);
        }

        public async Task<IList<CommandPosition>> ReadDemo(string filename)
        {
            Log.DebugFormat("reading demo from {Filename}", filename);

            string content = File.ReadAllText(filename, Encoding.ASCII);
            var matches = Match(content).ToArray();
            Log.DebugFormat("found {CountOccurrences} potential occurrences", matches.Length);

            IList<CommandPosition> positions = (await FindPositions(filename, matches)).ToList();
            return positions;
        }

        private IOrderedEnumerable<WordMatch> Match(string content)
        {
            return _ahoCorasick
                .Search(content)
                .DistinctBy(m => m.Index)
                .OrderBy(m => m.Index);
        }

        private static async Task<IEnumerable<CommandPosition>> FindPositions(string filename, IEnumerable<WordMatch> matches)
        {
            List<CommandPosition> positions = new List<CommandPosition>();
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                foreach (WordMatch match in matches)
                {
                    MoveToPosition(stream, match.Index);

                    MoveToTextStart(stream);

                    var messageTypeMatches = MessageTypeMatches(stream);
                    if (!messageTypeMatches)
                        continue;

                    var expectedLength = await ReadExpectedLength(stream);
                    var bytesTillNull = await FindTextLength(stream, expectedLength);
                    if (bytesTillNull < 0)
                        continue;

                    long index = stream.Position;

                    CommandPosition position = new CommandPosition
                    {
                        Index = index,
                        NumberOfBytes = bytesTillNull,
                    };

                    Log.TraceFormat("found CommandPosition {CommandPosition} Bytes for command {ReplacedCommand}", position, match.Word);
                    positions.Add(position);
                }
            }

            return positions;
        }

        private static void MoveToTextStart(FileStream stream)
        {
            stream.Seek(-1, SeekOrigin.Current);
            while (char.IsWhiteSpace((char)stream.ReadByte()))
            {
                stream.Seek(-2, SeekOrigin.Current);
            }
        }

        private static void MoveToPosition(FileStream stream, long index)
        {
            long bytesToMove = index - stream.Position;
            stream.Seek(bytesToMove, SeekOrigin.Current);
        }

        private static bool MessageTypeMatches(FileStream stream)
        {
            stream.Seek(-9, SeekOrigin.Current);
            int messageType = stream.ReadByte();
            bool messageTypeMatches = messageType == 4;
            return messageTypeMatches;
        }

        private static async Task<long> ReadExpectedLength(FileStream stream)
        {
            byte[] buffer = new byte[8];
            await stream.ReadAsync(buffer, 0, 8);
            long expectedLength = BitConverter.ToInt64(buffer, 0);
            return expectedLength;
        }

        private static async Task<long> FindTextLength(FileStream stream, long expectedLength)
        {
            long textStartIndex = stream.Position;
            int length = 0;
            byte b = 1;
            while (b != 0)
            {
                if (length > expectedLength)
                    return -1;

                byte[] buffer = new byte[1];
                await stream.ReadAsync(buffer, 0, 1);
                b = buffer[0];
                length++;
            }
            long bytesTillNull = stream.Position - textStartIndex;
            if (bytesTillNull > int.MaxValue)
                throw new ArgumentOutOfRangeException("bytesTillNull");
            stream.Seek(-bytesTillNull, SeekOrigin.Current);
            return bytesTillNull;
        }
    }
}