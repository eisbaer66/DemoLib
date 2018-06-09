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
        [Token(Category = "keyword", Description = "direction the commands get moved to find a matching 'hole'", Example = "< or >")]
        Direction,
        [Token(Category = "identifier", Description = "tick in which to preferably insert the command", Example = "546")]
        Tick,
        [Token(Category = "keyword", Example = ";")]
        Semicolon,
        [Token(Category = "identifier", Description = "command which will be insert", Example = "demo_gototick 500")]
        Command
    }

    static class InjectionTokanizer
    {
        private static TextParser<Unit> CommandToken = Character.ExceptIn(';', '\r', '\n').IgnoreMany();

        public static Tokenizer<Tokens> Instance{ get; } = new TokenizerBuilder<Tokens>()
            .Ignore(Span.WhiteSpace)
            .Match(Character.EqualTo('<'), Tokens.Direction)
            .Match(Character.EqualTo('>'), Tokens.Direction)
            .Match(Character.EqualTo(';'), Tokens.Semicolon)
            .Match(Numerics.IntegerInt32, Tokens.Tick)
            .Match(CommandToken, Tokens.Command)
            .Build();
    }

    static class InjectionTextParsers
    {
        public static TextParser<TickIterationDirection> DirectionTextParser { get; } =
            from dir in Character.In('<', '>')
            select dir == '>' ? TickIterationDirection.Forward : TickIterationDirection.Backward;

        public static TextParser<string> CommandTextParser { get; } =
            from span in Character.ExceptIn(';', '\r', '\n').Many()
            select new string(span).Trim();
    }

    public class InjectionParser : IInjectionParser
    {
        private static TokenListParser<Tokens, TickIterationDirection> _directionTokenListParser =
            Token.EqualTo(Tokens.Direction)
                .Apply(InjectionTextParsers.DirectionTextParser);

        private static TokenListParser<Tokens, int> _tickTokenListParser =
            Token.EqualTo(Tokens.Tick)
                .Apply(Numerics.IntegerInt32);

        private static TokenListParser<Tokens, string> _commandTokenListParser =
            Token.EqualTo(Tokens.Command)
                .Apply(InjectionTextParsers.CommandTextParser);

        private static TokenListParser<Tokens, IEnumerable<string>> _commandsTokenListParser =
            from command in _commandTokenListParser
                .ManyDelimitedBy(Token.EqualTo(Tokens.Semicolon))
            select command.Select(c => c.ToString());

        private static TokenListParser<Tokens, TickConfigItem> _itemTokenListParser =
            from dir in _directionTokenListParser
            from tick in _tickTokenListParser
            from commands in _commandsTokenListParser
            select new TickConfigItem {Dir = dir, Tick = tick, Commands = commands.ToList()};

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

    //public class InjectionParser : IInjectionParser
    //{
    //    public static readonly Parser<char> Direction = Parse.Chars('<', '>');
    //    public static readonly Parser<char> CommandSeparator = Parse.Char(';');
    //    public static readonly Parser<string> NewLine = Parse.String(Environment.NewLine).Text();
    //    public static readonly Parser<string> Whitespaces = Parse.WhiteSpace.Many().Text();

    //    public static readonly Parser<string> CommandTerminator = Parse.Char(';').AtLeastOnce().Text().Or(NewLine).Token();
    //    public static readonly Parser<string> Command = Parse.AnyChar.Except(CommandTerminator).AtLeastOnce().Text().Token();


    //    public static readonly Parser<string> RecordTerminator =
    //        Parse.Return("").End().XOr(
    //            NewLine.End()).Or(
    //            NewLine);

    //    //private static readonly Parser<TickConfigItem> EmptyLine = 
    //    //    from ws in Whitespaces
    //    //    from terminator in RecordTerminator
    //    //    select (TickConfigItem)null;
    //    private static readonly Parser<TickConfigItem> Item = 
    //        from ws in Whitespaces.Many()
    //        from dir in Direction
    //        from tick in Parse.Digit.AtLeastOnce().Text().Token()
    //        from command in Command
    //        from commands in CommandSeparator.AtLeastOnce().Then(_ => Command.Or(CommandSeparator.AtLeastOnce().Text())).Many()
    //        from terminator in RecordTerminator
    //        select TickConfigItem.From(dir, tick, Cons(command, commands));

    //    private static IEnumerable<string> Cons(string item, IEnumerable<string> items)
    //    {
    //        yield return item;
    //        foreach (string i in items)
    //        {
    //            yield return i;
    //        }
    //    }

    //    public IEnumerable<TickConfigItem> ParseFrom(string text)
    //    {
    //        return Item.XMany().End().Parse(text).Where(i => i != null);
    //    }
    //}

    public class TickConfigItem
    {
        public static TickConfigItem From(char dir, string tick, IEnumerable<string> commands)
        {
            return new TickConfigItem
            {
                Dir = dir == '>' ? TickIterationDirection.Forward : TickIterationDirection.Backward,
                Tick = int.Parse(tick),
                Commands = commands.Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList(),
            };
        }

        public TickIterationDirection Dir { get; set; }

        public List<string> Commands { get; set; }

        public int Tick { get; set; }
    }

    public enum TickIterationDirection
    {
        Forward,
        Backward
    }
}
