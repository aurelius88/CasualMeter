using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CasualMeter.Common.Formatters
{
    public class Formatter
    {
        private static readonly ILog Logger = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        protected Dictionary<string, object> Placeholders;
        protected IFormatProvider FormatProvider;

        public Formatter() { }

        public Formatter(IEnumerable<KeyValuePair<string, object>> placeholders, IFormatProvider formatProvider)
        {
            Placeholders = placeholders.ToDictionary(x => x.Key, x => x.Value);
            FormatProvider = formatProvider;
        }

        private string ReplacePlaceHolder(string[] parts, bool containsAlignment)
        {
            if (parts.Length > 3)
                throw new FormatException("Too many parts in a place holder");
            var key = parts[0];
            string format = null;
            string alignment = null;
            if (parts.Length > 1)
            {
                if (containsAlignment)
                {
                    alignment = parts[1];
                    if (parts.Length == 3)
                        format = parts[2];
                }
                else
                {
                    format = parts[1];
                }
            }
            object value;
            if (!Placeholders.TryGetValue(key, out value))
            {
                Logger.Warn(string.Format("Unknown placeholder '{0}'", key));
                value = "-";
            }
            string result = ToString(value, format, FormatProvider);
            return containsAlignment ? string.Format($"{{0,{alignment}}}", result) : result;
        }

        private static string ToString(object o, string format, IFormatProvider formatProvider)
        {
            var formattable = o as IFormattable;
            if (formattable != null)
                return formattable.ToString(format, formatProvider);
            return o.ToString();
        }

        public string Replace(string input)
        {
            return Replace(input, ReplacePlaceHolder);
        }

        private static string Replace(string input, Func<string[], bool, string> placeHolderFunc)
        {
            bool isEscape = false;
            bool isPlaceHolder = false;
            bool isPlaceHolderAlignment = false;
            bool isPlaceHolderFormat = false;
            var result = new StringBuilder();
            var placeHolderPart = new StringBuilder();
            var placeHolderParts = new List<string>();
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (!isEscape)
                {
                    switch (c)
                    {
                        case '{':
                            if (isPlaceHolder)
                                throw new FormatException("Unexpected '{'");
                            isPlaceHolder = true;
                            goto nextChar;
                        case '}':
                            if (!isPlaceHolder)
                                throw new FormatException("Unexpected '}'");
                            placeHolderParts.Add(placeHolderPart.ToString());
                            result.Append(placeHolderFunc(placeHolderParts.ToArray(), isPlaceHolderAlignment));
                            placeHolderPart.Clear();
                            placeHolderParts.Clear();
                            isPlaceHolderAlignment = false;
                            isPlaceHolderFormat = false;
                            isPlaceHolder = false;
                            goto nextChar;
                        case '\\':
                            isEscape = true;
                            goto nextChar;
                        case ':':
                            if (!isPlaceHolder)
                                goto printChar;
                            placeHolderParts.Add(placeHolderPart.ToString());
                            placeHolderPart.Clear();
                            isPlaceHolderAlignment = false;
                            isPlaceHolderFormat = true;
                            goto nextChar;
                        case ',':
                            if (!isPlaceHolder || isPlaceHolderFormat)
                                goto printChar;
                            if (isPlaceHolderAlignment)
                                throw new FormatException("Unexpected ','");
                            isPlaceHolderAlignment = true;
                            placeHolderParts.Add(placeHolderPart.ToString());
                            placeHolderPart.Clear();
                            goto nextChar;
                    }
                }
                printChar:
                if (isPlaceHolder)
                    placeHolderPart.Append(c);
                else
                    result.Append(c);
                nextChar:;
            }
            if (isEscape)
                throw new FormatException("End of string while in escape mode");
            if (isPlaceHolder)
                throw new FormatException("End of string while in placeholder mode");
            return result.ToString();
        }

    }
}
