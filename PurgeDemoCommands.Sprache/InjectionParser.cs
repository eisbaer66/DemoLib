using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace PurgeDemoCommands.Sprache
{
    public interface IInjectionParser
    {
        Result<IEnumerable<TickConfigItem>> ParseFrom(string text);
    }

    public enum Tokens
    {
        None,
        [Token(Category = "identifier", Description = "tick in which to preferably insert the command", Example = "546")]
        Tick,
        [Token(Category = "identifier", Description = "command which will be insert", Example = "demo_gototick 500")]
        Command
    }

    static class InjectionTokanizer
    {
        private static TextParser<Unit> CommandToken = Character.ExceptIn('\r', '\n').IgnoreMany();

        public static Tokenizer<Tokens> Instance{ get; } = new TokenizerBuilder<Tokens>()
            .Ignore(Span.WhiteSpace)
            .Match(Numerics.IntegerInt32, Tokens.Tick)
            .Match(CommandToken, Tokens.Command)
            .Build();
    }

    static class InjectionTextParsers
    {
        public static TextParser<string> CommandTextParser { get; } =
            from span in Character.ExceptIn('\r', '\n').Many()
            select new string(span).Trim();
    }

    public class InjectionParser : IInjectionParser
    {
        private static TokenListParser<Tokens, int> _tickTokenListParser =
            Token.EqualTo(Tokens.Tick)
                .Apply(Numerics.IntegerInt32);

        private static TokenListParser<Tokens, string> _commandTokenListParser =
            Token.EqualTo(Tokens.Command)
                .Apply(InjectionTextParsers.CommandTextParser);

        private static TokenListParser<Tokens, TickConfigItem> _itemTokenListParser =
            from tick in _tickTokenListParser
            from commands in _commandTokenListParser
            select new TickConfigItem {Tick = tick, Commands = commands};

        private static TokenListParser<Tokens, TickConfigItem[]> _documentTokenListParser =
            from i in _itemTokenListParser
                .Many()
            select i;

        public Result<IEnumerable<TickConfigItem>> ParseFrom(string text)
        {
            var tokens = InjectionTokanizer.Instance.TryTokenize(text);
            if (!tokens.HasValue)
            {
                return new Result<IEnumerable<TickConfigItem>>
                {
                    Message = tokens.ToString(),
                    Line = tokens.ErrorPosition.Line,
                    Column = tokens.ErrorPosition.Column,
                };
            }

            var items = _documentTokenListParser.TryParse(tokens.Value);
            if (!items.HasValue)
            {
                return new Result<IEnumerable<TickConfigItem>>
                {
                    Message = items.ToString(),
                    Line = items.ErrorPosition.Line,
                    Column = items.ErrorPosition.Column,
                };
            }

            return new Result<IEnumerable<TickConfigItem>>
            {
                Success = true,
                Items = items.Value,
            };
        }
    }

    public class Result<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public TickConfigItem[] Items { get; set; }
    }

    public class TickConfigItem
    {
        public static TickConfigItem From(string tick, string commands)
        {
            return new TickConfigItem
            {
                Tick = int.Parse(tick),
                Commands = commands.Trim(),
            };
        }

        public string Commands { get; set; }

        public int Tick { get; set; }
    }
}
