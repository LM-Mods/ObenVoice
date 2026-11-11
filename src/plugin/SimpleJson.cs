using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ObenseuerTts
{
    internal enum SimpleJsonKind
    {
        Null,
        Boolean,
        Number,
        String,
        Array,
        Object
    }

    internal sealed class SimpleJsonValue
    {
        internal SimpleJsonKind Kind;
        internal bool Boolean;
        internal double Number;
        internal string String = string.Empty;
        internal List<SimpleJsonValue> Array;
        internal Dictionary<string, SimpleJsonValue> Object;

        internal SimpleJsonValue Get(string name)
        {
            SimpleJsonValue value;
            return Kind == SimpleJsonKind.Object && Object != null && Object.TryGetValue(name, out value) ? value : null;
        }

        internal string StringOrEmpty(string name)
        {
            SimpleJsonValue value = Get(name);
            return value != null && value.Kind == SimpleJsonKind.String ? value.String : string.Empty;
        }

        internal bool TryNumber(string name, out double value)
        {
            SimpleJsonValue item = Get(name);
            if (item != null && item.Kind == SimpleJsonKind.Number)
            {
                value = item.Number;
                return true;
            }
            value = 0;
            return false;
        }

        internal bool BoolOrDefault(string name, bool fallback)
        {
            SimpleJsonValue value = Get(name);
            return value != null && value.Kind == SimpleJsonKind.Boolean ? value.Boolean : fallback;
        }
    }

    internal static class SimpleJson
    {
        internal static SimpleJsonValue Parse(string json)
        {
            Parser parser = new Parser(json ?? string.Empty);
            SimpleJsonValue value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw new FormatException("Unexpected content after the JSON document.");
            return value;
        }

        internal static string Escape(string value)
        {
            StringBuilder result = new StringBuilder();
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    default:
                        if (character < 0x20) result.Append("\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else result.Append(character);
                        break;
                }
            }
            return result.ToString();
        }

        private sealed class Parser
        {
            private readonly string source;
            private int index;

            internal Parser(string source)
            {
                this.source = source;
            }

            internal bool AtEnd { get { return index >= source.Length; } }

            internal void SkipWhitespace()
            {
                while (!AtEnd && char.IsWhiteSpace(source[index])) index++;
            }

            internal SimpleJsonValue ParseValue()
            {
                SkipWhitespace();
                if (AtEnd) throw new FormatException("Unexpected end of JSON.");
                char current = source[index];
                if (current == '{') return ParseObject();
                if (current == '[') return ParseArray();
                if (current == '"') return new SimpleJsonValue { Kind = SimpleJsonKind.String, String = ParseString() };
                if (current == '-' || (current >= '0' && current <= '9')) return ParseNumber();
                if (Consume("true")) return new SimpleJsonValue { Kind = SimpleJsonKind.Boolean, Boolean = true };
                if (Consume("false")) return new SimpleJsonValue { Kind = SimpleJsonKind.Boolean, Boolean = false };
                if (Consume("null")) return new SimpleJsonValue { Kind = SimpleJsonKind.Null };
                throw new FormatException("Invalid JSON value at character " + index.ToString(CultureInfo.InvariantCulture) + ".");
            }

            private SimpleJsonValue ParseObject()
            {
                Expect('{');
                Dictionary<string, SimpleJsonValue> values = new Dictionary<string, SimpleJsonValue>(StringComparer.Ordinal);
                SkipWhitespace();
                if (TryExpect('}')) return new SimpleJsonValue { Kind = SimpleJsonKind.Object, Object = values };
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || source[index] != '"') throw new FormatException("JSON object keys must be strings.");
                    string key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    if (values.ContainsKey(key)) throw new FormatException("Duplicate JSON object key: " + key);
                    values[key] = ParseValue();
                    SkipWhitespace();
                    if (TryExpect('}')) break;
                    Expect(',');
                }
                return new SimpleJsonValue { Kind = SimpleJsonKind.Object, Object = values };
            }

            private SimpleJsonValue ParseArray()
            {
                Expect('[');
                List<SimpleJsonValue> values = new List<SimpleJsonValue>();
                SkipWhitespace();
                if (TryExpect(']')) return new SimpleJsonValue { Kind = SimpleJsonKind.Array, Array = values };
                while (true)
                {
                    values.Add(ParseValue());
                    SkipWhitespace();
                    if (TryExpect(']')) break;
                    Expect(',');
                }
                return new SimpleJsonValue { Kind = SimpleJsonKind.Array, Array = values };
            }

            private SimpleJsonValue ParseNumber()
            {
                int start = index;
                if (source[index] == '-') index++;
                if (AtEnd) throw new FormatException("Invalid JSON number.");
                if (source[index] == '0') index++;
                else
                {
                    if (source[index] < '1' || source[index] > '9') throw new FormatException("Invalid JSON number.");
                    while (!AtEnd && source[index] >= '0' && source[index] <= '9') index++;
                }
                if (!AtEnd && source[index] == '.')
                {
                    index++;
                    int fractionStart = index;
                    while (!AtEnd && source[index] >= '0' && source[index] <= '9') index++;
                    if (fractionStart == index) throw new FormatException("Invalid JSON number fraction.");
                }
                if (!AtEnd && (source[index] == 'e' || source[index] == 'E'))
                {
                    index++;
                    if (!AtEnd && (source[index] == '+' || source[index] == '-')) index++;
                    int exponentStart = index;
                    while (!AtEnd && source[index] >= '0' && source[index] <= '9') index++;
                    if (exponentStart == index) throw new FormatException("Invalid JSON number exponent.");
                }
                double number;
                if (!double.TryParse(source.Substring(start, index - start), NumberStyles.Float, CultureInfo.InvariantCulture, out number) || double.IsInfinity(number) || double.IsNaN(number))
                    throw new FormatException("Invalid JSON number.");
                return new SimpleJsonValue { Kind = SimpleJsonKind.Number, Number = number };
            }

            private string ParseString()
            {
                Expect('"');
                StringBuilder result = new StringBuilder();
                while (!AtEnd)
                {
                    char current = source[index++];
                    if (current == '"') return result.ToString();
                    if (current < 0x20) throw new FormatException("Control character in JSON string.");
                    if (current != '\\')
                    {
                        result.Append(current);
                        continue;
                    }
                    if (AtEnd) throw new FormatException("Incomplete JSON string escape.");
                    char escaped = source[index++];
                    switch (escaped)
                    {
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        case '/': result.Append('/'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'u':
                            if (index + 4 > source.Length) throw new FormatException("Incomplete JSON Unicode escape.");
                            int code;
                            if (!int.TryParse(source.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                                throw new FormatException("Invalid JSON Unicode escape.");
                            result.Append((char)code);
                            index += 4;
                            break;
                        default: throw new FormatException("Invalid JSON string escape.");
                    }
                }
                throw new FormatException("Unterminated JSON string.");
            }

            private bool Consume(string token)
            {
                if (index + token.Length > source.Length || !string.Equals(source.Substring(index, token.Length), token, StringComparison.Ordinal)) return false;
                index += token.Length;
                return true;
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (AtEnd || source[index] != expected) throw new FormatException("Expected '" + expected + "' at character " + index.ToString(CultureInfo.InvariantCulture) + ".");
                index++;
            }

            private bool TryExpect(char expected)
            {
                SkipWhitespace();
                if (AtEnd || source[index] != expected) return false;
                index++;
                return true;
            }
        }
    }
}
