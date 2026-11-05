using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PowerOps.Core.Flows;

/// <summary>The 39 functions of the expression language that flow tests support, each written from its section of Learn's expression functions reference.</summary>
public static class ExpressionFunctions
{
    /// <summary>Learn's limit on the result of <c>concat</c>, applied to every string the simulator builds.</summary>
    internal const long MaxStringLength = 104_857_600;

    /// <summary>The bytes a recorded node is counted as, besides its text.</summary>
    internal const long NodeBytes = 64;

    /// <summary>The most a run may record (see <c>FlowSimulator</c>); a function that builds many nodes checks its result against it first.</summary>
    internal const long MaxRunBytes = 256L * 1024 * 1024;

    private delegate JsonNode? Function(JsonNode?[] args, IExpressionContext context);

    private static readonly Dictionary<string, Function> Table = new()
    {
        // Workflow
        ["outputs"] = (a, c) => c.Outputs(Str("outputs", Count("outputs", a, 1)[0])),
        ["body"] = (a, c) => Expression.Read(c.Outputs(Str("body", Count("body", a, 1)[0])), JsonValue.Create("body"), false),
        ["triggerOutputs"] = (a, c) => NoArguments("triggerOutputs", a, c).TriggerOutputs(),
        ["triggerBody"] = (a, c) => Expression.Read(NoArguments("triggerBody", a, c).TriggerOutputs(), JsonValue.Create("body"), false),
        ["variables"] = (a, c) => c.Variable(Str("variables", Count("variables", a, 1)[0])),
        ["parameters"] = (a, c) => c.Parameter(Str("parameters", Count("parameters", a, 1)[0])),
        ["items"] = (a, c) => c.Items(Str("items", Count("items", a, 1)[0])),
        ["item"] = (a, c) => NoArguments("item", a, c).Item(),
        ["workflow"] = (a, c) => NoArguments("workflow", a, c).Workflow(),
        ["result"] = (a, c) => c.Result(Str("result", Count("result", a, 1)[0])),

        // String
        ["concat"] = (a, c) => Concat(Count("concat", a, 2, int.MaxValue), c),
        ["substring"] = (a, _) => Substring(Count("substring", a, 2, 3)),
        ["replace"] = (a, c) => Replace(Count("replace", a, 3), c),
        ["split"] = (a, c) => Split(Count("split", a, 2), c),
        ["toLower"] = (a, _) => JsonValue.Create(Str("toLower", Count("toLower", a, 1)[0]).ToLowerInvariant()),
        ["trim"] = (a, _) => JsonValue.Create(Str("trim", Count("trim", a, 1)[0]).Trim()),
        ["guid"] = (a, c) => a.Length > 0 ? throw new UnsupportedException("guid() with a format argument is not supported.") : JsonValue.Create(c.Guid()),

        // Collection
        ["contains"] = (a, _) => Contains(Count("contains", a, 2)),
        ["empty"] = (a, _) => Empty(Count("empty", a, 1)[0]),
        ["first"] = (a, _) => Edge("first", Count("first", a, 1)[0], first: true),
        ["last"] = (a, _) => Edge("last", Count("last", a, 1)[0], first: false),
        ["length"] = (a, _) => Length(Count("length", a, 1)[0]),

        // Logical
        ["equals"] = (a, _) => JsonValue.Create(Count("equals", a, 2) is [var x, var y] && Equal(x, y)),
        ["greater"] = (a, _) => JsonValue.Create(Compare("greater", Count("greater", a, 2)) > 0),
        ["greaterOrEquals"] = (a, _) => JsonValue.Create(Compare("greaterOrEquals", Count("greaterOrEquals", a, 2)) >= 0),
        ["lessOrEquals"] = (a, _) => JsonValue.Create(Compare("lessOrEquals", Count("lessOrEquals", a, 2)) <= 0),
        ["if"] = (a, _) => Count("if", a, 3) is [var test, var yes, var no] ? (Bool("if", test) ? yes : no) : null,
        ["and"] = (a, _) => JsonValue.Create(Bools("and", a).All(b => b)),
        ["or"] = (a, _) => JsonValue.Create(Bools("or", a).Any(b => b)),
        ["not"] = (a, _) => JsonValue.Create(!Bool("not", Count("not", a, 1)[0])),

        // Conversion
        ["json"] = (a, c) => Json(Count("json", a, 1)[0], c),
        ["string"] = (a, c) => Concat(Count("string", a, 1), c),
        ["decodeBase64"] = (a, _) => DecodeBase64(Str("decodeBase64", Count("decodeBase64", a, 1)[0])),
        ["createArray"] = (a, _) => new JsonArray(Count("createArray", a, 1, int.MaxValue).Select(x => x?.DeepClone()).ToArray()),
        ["int"] = (a, _) => ToInt(Count("int", a, 1)[0]),

        // Math
        ["min"] = (a, _) => Min(a),

        // Date and time
        ["utcNow"] = (a, c) => a.Length > 0 ? throw new UnsupportedException("utcNow() with a format argument is not supported.") : JsonValue.Create(Format(Timestamp("utcNow", c.UtcNow()))),
        ["formatDateTime"] = (a, c) => FormatDateTime(Count("formatDateTime", a, 1, 3), c),
        ["addDays"] = (a, c) => AddDays(a, c),
    };

    /// <summary>The supported function names, sorted.</summary>
    public static IReadOnlyList<string> Names { get; } = [.. Table.Keys.Order(StringComparer.Ordinal)];

    /// <exception cref="UnsupportedException">A string of <paramref name="length"/> characters is past <see cref="MaxStringLength"/>.</exception>
    internal static void CheckLength(long length)
    {
        if (length > MaxStringLength)
        {
            throw new UnsupportedException($"a string longer than {MaxStringLength} characters is not supported (Learn's limit on concat).");
        }
    }

    internal static bool IsKnown(string name) => Table.ContainsKey(name);

    /// <exception cref="UnsupportedException">The function isn't one of <see cref="Names"/>.</exception>
    internal static JsonNode? Invoke(string name, JsonNode?[] args, IExpressionContext context) =>
        Table.TryGetValue(name, out var function) ? function(args, context) : throw new UnsupportedException($"Unsupported function '{name}'.");

    internal static string Describe(JsonNode? node) => node switch
    {
        null => "null",
        JsonObject => "an object",
        JsonArray => "an array",
        JsonValue v => v.GetValueKind() switch
        {
            JsonValueKind.String => "a string",
            JsonValueKind.Number => "a number",
            _ => "a boolean",
        },
        _ => "a value",
    };

    /// <summary>Parses JSON text and rejects a duplicate property at once: a JsonObject would find it only when first read, as an ArgumentException.</summary>
    /// <exception cref="JsonException">The text is not valid JSON, is nested deeper than 64 levels, has a duplicate property or holds a string with a lone surrogate, escaped in the text or a real one in the string.</exception>
    internal static JsonNode? ParseJson(string text)
    {
        try
        {
            var node = JsonNode.Parse(text, null, new JsonDocumentOptions { AllowDuplicateProperties = false });
            CheckText(node);
            return node;
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException)
        {
            // A string with a lone surrogate (`"\ud800"`) parses; reading it throws, in the duplicate check for a key and in the nodes for a value. Read them all now.
            // A real lone surrogate in the text (`substring('😀',0,1)`) is an ArgumentException from the parse itself.
            throw new JsonException(e.Message, e);
        }
    }

    private static void CheckText(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject o:
                foreach (var (_, value) in o)
                {
                    CheckText(value);
                }

                break;
            case JsonArray a:
                foreach (var item in a)
                {
                    CheckText(item);
                }

                break;
            case JsonValue v when v.GetValueKind() == JsonValueKind.String:
                _ = v.GetValue<string>();
                break;
        }
    }

    internal static bool TryLong(JsonNode? node, out long value)
    {
        value = 0;
        return node is JsonValue v && v.GetValueKind() == JsonValueKind.Number && long.TryParse(v.ToJsonString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static JsonNode?[] Count(string function, JsonNode?[] args, int min, int max)
    {
        if (args.Length < min || args.Length > max)
        {
            var expected = max == int.MaxValue ? $"at least {min}" : min == max ? $"{min}" : $"{min} to {max}";
            throw new ExpressionException($"{function}() takes {expected} argument(s), got {args.Length}.");
        }

        return args;
    }

    private static IExpressionContext NoArguments(string function, JsonNode?[] args, IExpressionContext context)
    {
        Count(function, args, 0);
        return context;
    }

    private static JsonNode?[] Count(string function, JsonNode?[] args, int count) => Count(function, args, count, count);

    private static bool IsString(JsonNode? node) => node is JsonValue v && v.GetValueKind() == JsonValueKind.String;

    private static bool IsNumber(JsonNode? node) => node is JsonValue v && v.GetValueKind() == JsonValueKind.Number;

    private static string Str(string function, JsonNode? node) =>
        IsString(node) ? node!.GetValue<string>() : throw new ExpressionException($"{function}() expects a string, got {Describe(node)}.");

    private static long Int(string function, JsonNode? node) =>
        TryLong(node, out var value) ? value : throw new ExpressionException($"{function}() expects an integer, got {Describe(node)}.");

    private static bool Bool(string function, JsonNode? node) =>
        node is JsonValue v && v.GetValueKind() is JsonValueKind.True or JsonValueKind.False
            ? v.GetValueKind() == JsonValueKind.True
            : throw new ExpressionException($"{function}() expects a boolean, got {Describe(node)}.");

    private static bool[] Bools(string function, JsonNode?[] args) => [.. Count(function, args, 1, int.MaxValue).Select(a => Bool(function, a))];

    private static double Double(JsonNode node) => node is JsonValue v && v.GetValueKind() == JsonValueKind.True ? 1
        : node is JsonValue f && f.GetValueKind() == JsonValueKind.False ? 0
        : double.Parse(node.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static int CompareNumbers(JsonNode x, JsonNode y) =>
        TryLong(x, out var lx) && TryLong(y, out var ly) ? lx.CompareTo(ly) : Double(x).CompareTo(Double(y));

    /// <summary>The capture run (M06, M07, M08): null equals only null, a boolean only a boolean, numbers by value; strings are compared ordinally (not on Learn, plan "Not on Learn").</summary>
    private static bool Equal(JsonNode? x, JsonNode? y)
    {
        if (x is null || y is null)
        {
            return x is null && y is null;
        }

        if (IsString(x) && IsString(y))
        {
            return x.GetValue<string>() == y.GetValue<string>();
        }

        if (IsNumber(x) && IsNumber(y))
        {
            return CompareNumbers(x, y) == 0;
        }

        if (x.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
        {
            return x.GetValueKind() == y.GetValueKind();
        }

        return (x is JsonObject && y is JsonObject || x is JsonArray && y is JsonArray) && JsonNode.DeepEquals(x, y);
    }

    private static int Compare(string function, JsonNode?[] args)
    {
        var x = args[0] ?? "";
        var y = args[1] ?? "";
        if (IsString(x) && IsString(y))
        {
            return string.CompareOrdinal(x.GetValue<string>(), y.GetValue<string>());
        }

        return IsNumber(x) && IsNumber(y)
            ? CompareNumbers(x, y)
            : throw new ExpressionException($"{function}() cannot compare {Describe(x)} with {Describe(y)}.");
    }

    private static JsonNode? Substring(JsonNode?[] a)
    {
        var text = Str("substring", a[0]);
        var start = Int("substring", a[1]);
        var length = a.Length == 3 ? Int("substring", a[2]) : text.Length - start;
        if (start < 0 || length < 0 || start > text.Length || length > text.Length - start)
        {
            // Learn: a start and length that reach past the end are an error, not a shorter result
            throw new ExpressionException($"substring() cannot take {length} characters from index {start} of a string of {text.Length}.");
        }

        return JsonValue.Create(text.Substring((int)start, (int)length));
    }

    private static JsonNode? Concat(JsonNode?[] a, IExpressionContext context)
    {
        var parts = a.Select(Expression.ToText).ToArray();
        var length = parts.Sum(p => (long)p.Length);
        CheckLength(length);
        context.ChargeCopy(length);
        return JsonValue.Create(string.Concat(parts));
    }

    // Non-overlapping, left to right, as string.Replace and string.Split find them.
    private static long CountMatches(string text, string find)
    {
        long count = 0;
        for (var at = text.IndexOf(find, StringComparison.Ordinal); at >= 0; at = text.IndexOf(find, at + find.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static JsonNode? Replace(JsonNode?[] a, IExpressionContext context)
    {
        var oldText = Str("replace", a[1]);
        if (oldText.Length == 0)
        {
            throw new ExpressionException("replace() cannot replace an empty string.");
        }

        var text = Str("replace", a[0]);
        var newText = Str("replace", a[2]);
        var length = text.Length + (CountMatches(text, oldText) * (newText.Length - oldText.Length));
        CheckLength(length);
        context.ChargeCopy(length);
        return JsonValue.Create(text.Replace(oldText, newText, StringComparison.Ordinal));
    }

    private static JsonNode? Split(JsonNode?[] a, IExpressionContext context)
    {
        var text = Str("split", a[0]);
        var separator = Str("split", a[1]);
        if (separator.Length > 0 && (CountMatches(text, separator) + 1) * NodeBytes > MaxRunBytes)
        {
            throw new UnsupportedException("split() would give more parts than a run can record.");
        }

        context.ChargeCopy(text.Length);
        return new JsonArray(text.Split(separator, StringSplitOptions.None).Select(part => (JsonNode?)JsonValue.Create(part)).ToArray());
    }

    private static JsonNode? Contains(JsonNode?[] a) => a[0] switch
    {
        JsonObject obj => JsonValue.Create(obj.ContainsKey(Str("contains", a[1]))),
        JsonArray arr => JsonValue.Create(arr.Any(item => Equal(item, a[1]))),
        var other when IsString(other) => JsonValue.Create(other!.GetValue<string>().Contains(Str("contains", a[1]), StringComparison.Ordinal)),
        var other => throw new ExpressionException($"contains() expects a string, an array or an object, got {Describe(other)}."),
    };

    // not on Learn, plan "Not on Learn": null counts as empty, like the empty string it converts to in comparisons
    private static JsonNode? Empty(JsonNode? a) => a switch
    {
        null => JsonValue.Create(true),
        JsonObject obj => JsonValue.Create(obj.Count == 0),
        JsonArray arr => JsonValue.Create(arr.Count == 0),
        _ when IsString(a) => JsonValue.Create(a.GetValue<string>().Length == 0),
        _ => throw new ExpressionException($"empty() expects a string, an array or an object, got {Describe(a)}."),
    };

    // not on Learn, plan "Not on Learn": first and last of an empty array are null (capture run M15); of an empty string they are "" (M16)
    private static JsonNode? Edge(string function, JsonNode? a, bool first) => a switch
    {
        JsonArray arr => arr.Count == 0 ? null : arr[first ? 0 : arr.Count - 1],
        _ when IsString(a) => a!.GetValue<string>() is { Length: > 0 } s ? JsonValue.Create(s[first ? 0 : s.Length - 1].ToString()) : JsonValue.Create(""),
        _ => throw new ExpressionException($"{function}() expects a string or an array, got {Describe(a)}."),
    };

    private static JsonNode? Length(JsonNode? a) => a switch
    {
        JsonArray arr => JsonValue.Create(arr.Count),
        _ when IsString(a) => JsonValue.Create(a!.GetValue<string>().Length),
        _ => throw new ExpressionException($"length() expects a string or an array, got {Describe(a)}."),
    };

    private static JsonNode? Json(JsonNode? a, IExpressionContext context)
    {
        if (a is null)
        {
            return new JsonObject(); // Learn: a null string gives an empty object
        }

        var text = Str("json", a);
        context.ChargeCopy(text.Length);
        try
        {
            return ParseJson(text);
        }
        catch (JsonException e)
        {
            throw new ExpressionException($"json() cannot read its argument: {e.Message}");
        }
    }

    private static JsonNode? DecodeBase64(string value)
    {
        try
        {
            return JsonValue.Create(new UTF8Encoding(false, true).GetString(Convert.FromBase64String(value)));
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            throw new ExpressionException($"decodeBase64() cannot decode its argument: {e.Message}");
        }
    }

    private static JsonNode? Min(JsonNode?[] a)
    {
        var numbers = a is [JsonArray array] ? array.ToArray() : Count("min", a, 2, int.MaxValue);
        if (numbers.Length == 0 || !numbers.All(IsNumber))
        {
            throw new ExpressionException("min() takes an array of numbers, or two or more numbers.");
        }

        return numbers.Aggregate((lowest, next) => CompareNumbers(next!, lowest!) < 0 ? next : lowest);
    }

    // Learn's forms: ISO 8601 with or without seconds or fraction, and MM/dd/yyyy; other text TryParse takes borrows parts from today's date
    private static readonly string[] TimestampForms = ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", "yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mmK", "yyyy-MM-dd", "MM/dd/yyyy", "MM/dd/yyyy HH:mm:ss"];

    private static void CheckForm(string function, string text)
    {
        if (!DateTimeOffset.TryParseExact(text, TimestampForms, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
        {
            throw DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _)
                ? new UnsupportedException($"{function}() of '{text}' is not supported: only full ISO 8601 timestamps and MM/dd/yyyy are read.")
                : new ExpressionException($"{function}() cannot read '{text}' as a timestamp.");
        }
    }

    private static DateTime Timestamp(string function, string text)
    {
        CheckForm(function, text);
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.UtcDateTime
            : throw new ExpressionException($"{function}() cannot read '{text}' as a timestamp.");
    }

    /// <summary>Learn's default format, "o": <c>2018-03-25T00:00:00.0000000Z</c> for UTC.</summary>
    private static string Format(DateTime utc) => utc.ToString("o", CultureInfo.InvariantCulture);

    // null when this .NET runs without culture data (invariant globalization)
    private static readonly CultureInfo? EnUs = Culture("en-us");

    private static CultureInfo? Culture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name, predefinedOnly: true);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static void NeedCultureData(string function)
    {
        if (EnUs is null)
        {
            throw new UnsupportedException($"{function}() needs culture data, and this .NET runs without it (invariant globalization).");
        }
    }

    private static bool Digits(string text) => text.Length > 0 && text.All(char.IsAsciiDigit);

    private static JsonNode? ToInt(JsonNode? a)
    {
        // not on Learn, plan "Not on Learn": an integer number within 32 bits gives itself
        if (TryLong(a, out var number))
        {
            return number is >= int.MinValue and <= int.MaxValue ? JsonValue.Create(number) : throw new UnsupportedException("int() of a number past 32 bits is not supported.");
        }

        if (!IsString(a))
        {
            throw new UnsupportedException($"int() of {Describe(a)} is not supported.");
        }

        var text = a!.GetValue<string>();
        var digits = text.StartsWith('-') ? text[1..] : text;
        if (Digits(digits) && (digits == "0" || digits[0] != '0') && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
        {
            return JsonValue.Create(value);
        }

        // Learn's cookbook: a decimal string is an error, not truncated
        var dot = digits.IndexOf('.');
        if ((dot > 0 && Digits(digits[..dot]) && Digits(digits[(dot + 1)..])) || !text.Any(char.IsDigit))
        {
            throw new ExpressionException($"int() cannot convert '{text}' to an integer.");
        }

        throw new UnsupportedException($"int() of '{text}' is not supported: Learn doesn't say how it reads a sign, spaces, leading zeros, an exponent or a number past 32 bits.");
    }

    private static JsonNode? FormatDateTime(JsonNode?[] a, IExpressionContext context)
    {
        NeedCultureData("formatDateTime");
        var text = Str("formatDateTime", a[0]);

        // Learn: '03/15/2018' gives '2018-03-15T00:00:00.0000000', no zone
        var time = ReadZoned("formatDateTime", text);
        if (time.Kind == DateTimeKind.Local)
        {
            throw new UnsupportedException("formatDateTime() of a timestamp with an offset is not supported: Learn doesn't say whether the offset is kept.");
        }

        var format = a.Length > 1 ? Str("formatDateTime", a[1]) : "o";
        var culture = a.Length > 2 ? Locale(Str("formatDateTime", a[2]), format) : EnUs!;
        return FormatAs("formatDateTime", time, format, culture, context);
    }

    // Kind: Utc for a Z, Local for an offset (+00:00 too), Unspecified for no zone.
    private static DateTime ReadZoned(string function, string text)
    {
        CheckForm(function, text);
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time)
            ? time
            : throw new ExpressionException($"{function}() cannot read '{text}' as a timestamp.");
    }

    // Learn shows fr-fr and es-es only with 'dddd MMMM d'; day periods, separators and standard patterns can differ between ICU and the service
    private static bool DayMonthYear(string format)
    {
        if (format.Length < 2)
        {
            return false;
        }

        for (var at = 0; at < format.Length;)
        {
            var end = at;
            while (end < format.Length && format[end] == format[at])
            {
                end++;
            }

            if (!(char.IsAsciiLetter(format[at]) ? format[at..end] is "d" or "dd" or "dddd" or "M" or "MM" or "MMMM" or "yy" or "yyyy" : format[at] is ' ' or '-' or ',' or '.'))
            {
                return false;
            }

            at = end;
        }

        return true;
    }

    private static CultureInfo Locale(string name, string format)
    {
        if (name.Length == 0)
        {
            throw new UnsupportedException("formatDateTime() with an empty locale is not supported.");
        }

        // Learn says an invalid locale is an error; the capture run F18 gave English text for 'xx-YY'
        var culture = Culture(name) ?? throw new UnsupportedException($"formatDateTime() with the locale '{name}' is not supported: .NET does not know it, and the service did not call it an error.");
        var lower = name.ToLowerInvariant();
        if (lower is not ("en-us" or "fr-fr" or "es-es"))
        {
            throw new UnsupportedException($"formatDateTime() with the locale '{name}' is not supported: Learn shows only en-us, fr-fr and es-es, and other cultures' data can differ between ICU and the service.");
        }

        return lower == "en-us" || DayMonthYear(format)
            ? culture
            : throw new UnsupportedException($"formatDateTime() with the locale '{name}' takes only day, month and year formats, as Learn shows: other parts of its culture data can differ between ICU and the service.");
    }

    /// <summary>Learn: a standard or custom .NET date format; for addDays an invalid one is an error.</summary>
    private static JsonNode? FormatAs(string function, DateTime time, string format, CultureInfo culture, IExpressionContext context)
    {
        if (format.Length == 0)
        {
            throw new UnsupportedException($"{function}() with an empty format is not supported.");
        }

        // .NET would take this computer's time zone for these on a timestamp with no zone
        if (time.Kind == DateTimeKind.Unspecified && (format is "U" or "u" or "r" or "R" || format.Contains('z') || format.Contains('K')))
        {
            throw new UnsupportedException($"{function}() with the format '{format}' needs a timestamp with a time zone.");
        }

        string text;
        try
        {
            // shortcut: ICU writes U+202F before AM/PM where Learn's examples have a space; a U+202F typed in a format becomes a space too, fine until a flow needs one
            text = time.ToString(format, culture).Replace('\u202F', ' ');
        }
        catch (FormatException) when (function == "addDays")
        {
            throw new ExpressionException($"{function}() cannot use the format '{format}'.");
        }
        catch (FormatException)
        {
            throw new UnsupportedException($"{function}() with the format '{format}' is not supported: Learn doesn't say an invalid format is an error.");
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new UnsupportedException($"{function}() gives a date outside the calendar of its locale.");
        }

        CheckLength(text.Length);
        context.ChargeCopy(text.Length);
        return JsonValue.Create(text);
    }

    private static JsonNode? AddDays(JsonNode?[] a, IExpressionContext context)
    {
        Count("addDays", a, 2, 3);
        DateTime start;
        var kind = DateTimeKind.Utc;
        if (a.Length == 3)
        {
            NeedCultureData("addDays");
            start = ReadZoned("addDays", Str("addDays", a[0]));
            if (start.Kind != DateTimeKind.Utc)
            {
                throw new UnsupportedException("addDays() with a format needs a timestamp in UTC, ending in Z.");
            }
        }
        else
        {
            var text = Str("addDays", a[0]);
            start = Timestamp("addDays", text);
            kind = ReadZoned("addDays", text).Kind;
        }

        DateTime end;
        try
        {
            end = start.AddDays(Int("addDays", a[1]));
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ExpressionException("addDays() gives a date outside the supported range.");
        }

        // not on Learn, plan "Not on Learn": the format uses en-us, as utcNow's format examples on Learn do
        return a.Length == 3 ? FormatAs("addDays", end, Str("addDays", a[2]), EnUs!, context) : JsonValue.Create(Labelled(end, kind));
    }

    // the capture runs A06 and A11: the result keeps no zone for a timestamp with none, and shows an offset as +00:00 once converted to UTC
    private static string Labelled(DateTime utc, DateTimeKind kind) => kind switch
    {
        DateTimeKind.Utc => Format(utc),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(utc, DateTimeKind.Unspecified).ToString("o", CultureInfo.InvariantCulture),
        _ => new DateTimeOffset(utc, TimeSpan.Zero).ToString("o", CultureInfo.InvariantCulture),
    };
}