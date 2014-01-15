using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions; // here be dragons

namespace Westwind.Web.Utilities
{
  public class HtmlSanitizer
  {
    /// <summary>
    /// Prevents a default instance of the <see cref="HtmlSanitizer"/> class from being created.
    /// </summary>
    private HtmlSanitizer()
    {
    }

    /// <summary>
    /// A regex for detecting expressions within values for CSS properties.
    /// </summary>
    /// <remarks>
    /// I wish this wasn't needed, but right now the CSS parsing library
    /// we're using doesn't even recognize expressions as valid.
    /// If only browsers worked the same way.
    /// </remarks>
    private static readonly Regex _cssExpressionRegex =
      new Regex(@":\s*expression\s*\(", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary>
    /// The set of HTML tags that are blacklisted.
    /// </summary>
    private static readonly HashSet<string> _blacklistedTags = new HashSet<string>(System.StringComparer.InvariantCultureIgnoreCase) 
    {
      // Head elements and other such nonsense
      { "head" },
      { "link" }, 
      { "meta" },
      { "base" },
      { "form" },
      { "script" },
      // Embeddables
      { "applet" },
      { "object" },
      { "embed" },
      /// Frames
      { "frameset" },
      { "frame" },
      { "iframe" },
      // Layers
      { "layer" },
      { "ilayer" }
    };

    /// <summary>
    /// The set of MIME types that are blacklisted.
    /// </summary>
    private static readonly HashSet<string> _blacklistedMimeTypes = new HashSet<string>(System.StringComparer.InvariantCultureIgnoreCase)
    {
      { "text/javascript" },
      { "text/ecmascript" },
      { "application/ecmascript" },
      { "application/javascript" },
      { "text/vbscript" },
      { "javascript" }
    };

    /// <summary>
    /// Cleans up an HTML string by removing elements on the blacklist and all elements that start
    /// with onXXX.
    /// </summary>
    /// <param name="html">The HTML string to sanitize.</param>
    /// <returns>The sanitized version of the HTML.</returns>
    public static string SanitizeHtml(string html)
    {
      // Load in the HTML string into an HTML document
      var doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(html);

      // Sanitize the root document node
      SanitizeHtmlNode(doc.DocumentNode);

      string output = null;

      // Use an XmlTextWriter to create self-closing tags
      using (StringWriter sw = new StringWriter())
      {
        XmlWriter writer = new XmlTextWriter(sw);
        doc.DocumentNode.WriteTo(writer);
        output = sw.ToString();

        // strip off XML doc header
        if (!string.IsNullOrEmpty(output))
        {
          int at = output.IndexOf("?>");
          output = output.Substring(at + 2);
        }

        writer.Close();
      }
      doc = null;

      return output;
    }

    private static void SanitizeHtmlNode(HtmlAgilityPack.HtmlNode node)
    {
      if (node.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
      {
        // Remove elements if they're blacklisted
        if (_blacklistedTags.Contains(node.Name))
        {
          node.Remove();
          return;
        }

        // For style tags, don't allow remote-linked stylesheets or
        // items with CSS expressions/import directives
        if (node.Name == "style")
        {
          // Remove remote-linked stylesheets
          if (string.IsNullOrWhiteSpace(node.InnerText))
          {
            node.Remove();
            return;
          }

          // HACK: Look for CSS expressions, because
          // ExCSS doesn't successfully parse them out.
          if (_cssExpressionRegex.IsMatch(node.InnerText))
          {
            node.Remove();
            return;
          }

          // Next, try to parse out the CSS using ExCSS.
          // If it fails, we'll remove the node too.
          try
          {
            var stylesheetParser = new ExCSS.Parser();
            var stylesheet = stylesheetParser.Parse(node.InnerText);

            // Make sure we have no import directives.
            if (stylesheet.ImportDirectives.Count > 0)
            {
              node.Remove();
              return;
            }

            // Note: this is disabled because ExCSS doesn't even recognize
            // CSS expressions at the moment.
            //// Make sure the value of no property declaration starts with
            //// expression(
            //foreach (ExCSS.StyleRule ruleset in stylesheet.Rulesets)
            //{
            //  foreach (ExCSS.Property propDecl in ruleset.Declarations)
            //  {
            //    if (propDecl.Term.ToString().Trim().ToLower().StartsWith("expression("))
            //    {
            //      node.Remove();
            //      return;
            //    }
            //  }
            //}
          }
          catch
          {
            // Some sort of parsing error, which means this style
            // declaration is invalid.
            node.Remove();
            return;
          }
        }

        // remove script attributes
        if (node.HasAttributes)
        {
          for (int i = node.Attributes.Count - 1; i >= 0; i--)
          {
            HtmlAgilityPack.HtmlAttribute currentAttribute = node.Attributes[i];

            // Lowercase the attribute and its value for ease of use.
            var attr = currentAttribute.Name.ToLower();
            var val = currentAttribute.Value.ToLower();

            // Remove event handlers
            if (attr.StartsWith("on"))
            {
              node.Attributes.Remove(currentAttribute);
            }
            // Remove stuff with a forbidden MIME type - note that this removes
            // the entire node, not just the attribute.
            else if (attr == "type" && val != null && IsBlacklistedMimeType(val))
            {
              node.Remove();
              return;
            }
            // Remove CSS Expressions
            else if (attr == "style" && val != null && (HasExpressionOrImportLinks(val) || HasScriptLinks(val)))
            {
              node.Attributes.Remove(currentAttribute);
            }
            // Remove script links from all attributes
            else if (val != null && HasScriptLinks(val))
            {
              node.Attributes.Remove(currentAttribute);
            }
          }
        }
      }

      // Look through child nodes recursively
      if (node.HasChildNodes)
      {
        for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
        {
          SanitizeHtmlNode(node.ChildNodes[i]);
        }
      }
    }

    /// <summary>
    /// Determines whether the specified attribute value contains script links.
    /// </summary>
    /// <param name="value">The attribute value. It is assumed that this value
    /// has already been normalized as lowercase.</param>
    /// <returns><c>true</c> if the specified attribute value contains script links;
    /// otherwise, <c>false</c>.</returns>
    private static bool HasScriptLinks(string value)
    {
      return value.Contains("javascript:") || value.Contains("vbscript:");
    }

    /// <summary>
    /// Determines whether the specified attribute value contains 
    /// a CSS &quot;expression&quot; or &quot;@import&quot; rule.
    /// </summary>
    /// <param name="value">The attribute value. It is assumed that this value
    /// has already been normalized as lowercase.</param>
    /// <returns><c>true</c> if the specified attribute value contains a CSS expression
    /// or import; otherwise, <c>false</c>.</returns>
    private static bool HasExpressionOrImportLinks(string value)
    {
      return _cssExpressionRegex.IsMatch(value) || value.Contains("@import");
    }

    /// <summary>
    /// Determines whether the specified attribute value is a blacklisted MIME type.
    /// </summary>
    /// <param name="value">The attribute value. It is assumed that this value
    /// has already been normalized as lowercase.</param>
    /// <returns><c>true</c> if the specified attribute value corresponds with
    /// a blacklisted MIME type; otherwise, <c>false</c>.</returns>
    private static bool IsBlacklistedMimeType(string value)
    {
      return _blacklistedMimeTypes.Contains(value.Trim());
    }

  }
}