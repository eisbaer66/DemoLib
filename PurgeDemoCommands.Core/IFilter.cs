using System;
using System.Collections.Generic;
using PurgeDemoCommands.Core.Extensions;
using PurgeDemoCommands.Core.Logging;

namespace PurgeDemoCommands.Core
{
    public interface IFilter
    {
        bool Match(string command);
    }

    public static class Filter
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        public static IFilter From(ICollection<string> whitelist, ICollection<string> blacklist)
        {
            if (whitelist != null)
            {
                Logger.InfoFormat("using whitelist with {WhitelistCount} commands", whitelist.Count);

                return new Whitelist(whitelist);
            }
            if (blacklist != null)
            {
                Logger.InfoFormat("using blacklist with {BlacklistCount} commands", blacklist.Count);

                return new Blacklist(blacklist);
            }

            Logger.InfoFormat("using no filter, purging all commands");
            return new PassThrough();
        }

        internal class Whitelist : IFilter
        {
            private readonly HashSet<string> _list;

            public Whitelist(IEnumerable<string> whitelist)
            {
                if (whitelist == null) throw new ArgumentNullException(nameof(whitelist));
                _list = whitelist.ToHashSet();
            }

            public bool Match(string command)
            {
                return !_list.Contains(command.TillFirst(' '));
            }
        }

        internal class Blacklist : IFilter
        {
            private readonly HashSet<string> _list;

            public Blacklist(IEnumerable<string> blacklist)
            {
                if (blacklist == null) throw new ArgumentNullException(nameof(blacklist));
                _list = blacklist.ToHashSet();
            }

            public bool Match(string command)
            {
                return _list.Contains(command.TillFirst(' '));
            }
        }

        internal class PassThrough : IFilter
        {
            public bool Match(string command)
            {
                return true;
            }
        }
    }
}