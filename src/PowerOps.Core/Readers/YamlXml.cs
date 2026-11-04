using System.Xml;
using System.Xml.Linq;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace PowerOps.Core.Readers;

/// <summary>Valid YAML whose keys the converter can't map. InvalidDataException is sealed, so <see cref="YamlXml.Convert"/> turns this into one for the YAML layout; <see cref="YamlXml.Read"/> lets an agent file see it.</summary>
internal sealed class YamlShapeException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Turns the YAML layout back into the XML shape of the solution files: `'@name'` is an attribute, `'#text'` the element text,
/// a list is one element per item. Read as an event stream so anchors and aliases can be refused instead of expanded.
/// </summary>
internal static class YamlXml
{
    /// <param name="count">Charges that many nodes to the solution's budget before they are created (it throws when the budget is spent).</param>
    /// <returns>A `yaml` element whose children are the document's top-level keys.</returns>
    /// <exception cref="InvalidDataException">The YAML is malformed, uses anchors or aliases, is nested too deep, or has more nodes than the budget has left.</exception>
    public static XElement Convert(string yaml, Action<int> count)
    {
        try
        {
            return Read(yaml, count);
        }
        catch (YamlShapeException e)
        {
            throw new InvalidDataException(e.Message, e.InnerException);
        }
    }

    /// <summary>Like <see cref="Convert"/>, but valid YAML whose keys can't be mapped throws <see cref="YamlShapeException"/>.</summary>
    public static XElement Read(string yaml, Action<int> count)
    {
        count(1);
        var root = new XElement("yaml");
        try
        {
            var parser = new Parser(new StringReader(yaml));
            Next(parser); // StreamStart
            if (Next(parser) is DocumentStart && Next(parser) is MappingStart)
            {
                Mapping(parser, root, 1, count);
            }
        }
        // XAttribute and XElement throw for reserved names such as `'@xmlns'`.
        catch (YamlException e)
        {
            throw new InvalidDataException(e.Message, e);
        }
        catch (Exception e) when (e is ArgumentException or XmlException)
        {
            throw new YamlShapeException(e.Message, e);
        }

        return root;
    }

    private static void Mapping(Parser parser, XElement element, int depth, Action<int> count)
    {
        for (var ev = Next(parser); ev is not MappingEnd; ev = Next(parser))
        {
            if (ev is not Scalar key)
            {
                throw new YamlShapeException("YAML keys must be plain text.");
            }

            var value = Next(parser);
            if (key.Value.StartsWith('@') || key.Value == "#text")
            {
                if (value is not Scalar text)
                {
                    throw new YamlShapeException($"YAML key '{key.Value}' must have a plain text value.");
                }

                if (key.Value == "#text")
                {
                    count(1);
                    element.Add(text.Value);
                }
                else if (!key.Value.Contains(':'))
                {
                    count(1);
                    element.SetAttributeValue(Name(key.Value[1..]), text.Value);
                }
            }
            else
            {
                Node(parser, element, Name(key.Value), value, depth, count);
            }
        }
    }

    private static void Node(Parser parser, XElement parent, string name, ParsingEvent ev, int depth, Action<int> count)
    {
        if (depth > SolutionParser.MaxXmlDepth)
        {
            throw new InvalidDataException($"YAML is nested deeper than {SolutionParser.MaxXmlDepth} levels.");
        }

        switch (ev)
        {
            case Scalar s:
                count(2); // the element and its text
                parent.Add(new XElement(name, s.Value));
                break;
            case MappingStart:
                count(1);
                var child = new XElement(name);
                parent.Add(child);
                Mapping(parser, child, depth + 1, count);
                break;
            case SequenceStart:
                for (var item = Next(parser); item is not SequenceEnd; item = Next(parser))
                {
                    Node(parser, parent, name, item, depth + 1, count);
                }

                break;
            default:
                throw new InvalidDataException("Unexpected YAML content.");
        }
    }

    private static ParsingEvent Next(Parser parser)
    {
        if (!parser.MoveNext())
        {
            throw new InvalidDataException("Unexpected end of YAML.");
        }

        var ev = parser.Current!;
        if (ev is AnchorAlias || (ev is NodeEvent node && !node.Anchor.IsEmpty))
        {
            throw new InvalidDataException("YAML anchors and aliases are not supported.");
        }

        return ev;
    }

    private static string Name(string key) =>
        key.Length == 0 ? throw new YamlShapeException("YAML keys must not be empty.") : XmlConvert.EncodeLocalName(key);
}