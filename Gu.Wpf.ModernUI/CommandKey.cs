﻿namespace Gu.Wpf.ModernUI
{
    using System;
    using System.Text.RegularExpressions;

    public sealed class CommandKey : IEquatable<string>, IEquatable<CommandKey>
    {
        private readonly string key;

        private static readonly string cmdPattern = @"cmd:[/]+(?<key>\w+)";
        public CommandKey(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentException();
            }
            var match = Regex.Match(s, cmdPattern);
            if (match.Success)
            {
                this.key = match.Groups["key"].Value;
            }
            else
            {
                this.key = s;
            }
        }

        public static bool TryCreate(string s, out  CommandKey key)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                key = null;
                return false;
            }
            key = new CommandKey(s);
            return true;
        }

        public static bool TryCreate(Uri uri, out CommandKey key)
        {
            if (uri == null)
            {
                key = null;
                return false;
            }
            return TryCreate(uri.ToString(), out key);
        }

        internal static bool TryCreate(object key, out CommandKey commandKey)
        {
            var s = key as string;
            if (s != null)
            {
                return TryCreate(s, out commandKey);
            }

            var uri = key as Uri;
            if (uri != null)
            {
                return TryCreate(uri, out commandKey);
            }
            commandKey = null;
            return false;
        }

        public static bool operator ==(CommandKey left, CommandKey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CommandKey left, CommandKey right)
        {
            return !Equals(left, right);
        }

        public bool Equals(string other)
        {
            CommandKey key;
            if (!TryCreate(other, out key))
            {
                return false;
            }
            return Equals(key);
        }

        public bool Equals(CommandKey other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return string.Equals(this.key, other.key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((CommandKey)obj);
        }

        public override int GetHashCode()
        {
            return this.key.GetHashCode();
        }
    }
}