using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.Web.Utilities;
using System.Text.RegularExpressions;

namespace HtmlSanitizerTests
{
  [TestClass]
  public class HtmlSanitizerTests
  {

    #region "Style Attribute Sanitization"

    [TestMethod]
    [Description("Ensures that style attributes that contain CSS expressions are sanitized.")]
    public void StyleAttributeRemoveExpression()
    {
      var html = "<div style=\"color: expression(alert('xss'))\" ></div>";
      string result = HtmlSanitizer.SanitizeHtml(html);

      // Style attribute should be removed
      Assert.IsFalse(result.Contains("style="));
    }

    [TestMethod]
    [Description("Ensures that style attributes that contain @import directives are sanitized.")]
    public void StyleAttributeRemoveImport()
    {
      var html = "<div style=\"@import url('external-stylesheet.css')\" ></div>";
      string result = HtmlSanitizer.SanitizeHtml(html);

      // Style attribute should be removed
      Assert.IsFalse(result.Contains("style="));
    }

    #endregion

    #region "Style Element Sanitization"

    [TestMethod]
    [Description("Ensures that style elements that contain CSS expressions are sanitized.")]
    public void StyleElementRemoveExpression()
    {
      var html = "<style type=\"text/css\">" +
        "div { color: expression(alert('xss')) }" +
        "</style>";
      string result = HtmlSanitizer.SanitizeHtml(html);

      // Style element should be removed
      Assert.IsFalse(result.Contains("<style"));
    }

    [TestMethod]
    [Description("Ensures that style elements that contain @import directives are sanitized.")]
    public void StyleElementRemoveImport()
    {
      var html = "<style type=\"text/css\">" +
        "@import url('external-stylesheet.css')" +
        "</style>";
      string result = HtmlSanitizer.SanitizeHtml(html);

      // Style element should be removed
      Assert.IsFalse(result.Contains("<style"));
    }

    #endregion

    #region "Forbidden Tag Sanitization (script, iframe, etc.)"

    [TestMethod]
    [Description("Ensures that script tags are removed entirely.")]
    public void ScriptTagRemoval()
    {
      var html = "Should remove Script:" +
                 "<script>alert('hello');</script>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsFalse(result.Contains("<script>") || result.Contains("</script>"));
      Console.WriteLine(result);
    }

    [TestMethod]
    [Description("Ensures that iframe tags are removed entirely.")]
    public void IFrameRemoval()
    {
      var html = "<iframe src=\"http://example.com\" class='iframeclass'></iframe> <div></div>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      // iframe should be removed
      Assert.IsFalse(result.Contains("<iframe>") || result.Contains("</iframe>"));
    }

    #endregion

    #region "Anchor Tag Sanitization"

    [TestMethod]
    [Description("Ensures that an anchor tag with a JavaScript href is sanitized.")]
    public void AnchorTagRemoveJavascriptHref()
    {
      var html = "Should remove href (javascript:) " +
                 "<a href=\"javascript:alert('xss');\" />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      // HREF attr should be removed (a link stays)
      Assert.IsFalse(result.Contains("href="));
    }

    #endregion

    #region "Image Tag Sanitization"

    [TestMethod]
    [Description("Ensures that a valid src attribute of an image is preserved.")]
    public void ImageTagValid()
    {
      var html = "<img src=\"image.jpg\" />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsTrue(result.Contains("src="));
      Console.WriteLine(result);
    }

    [TestMethod]
    [Description("Ensures that a src attribute of an image that contains a JavaScript expression is sanitized.")]
    public void ImageTagRemoveJavascriptSrc()
    {
      var html = "Should remove src (javascript:) " +
                 "<img src=\"javascript:alert('xss');\" />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsFalse(result.Contains("src="));
      Console.WriteLine(result);
    }

    [TestMethod]
    [Description("Ensures that a src attribute of an image that contains a JavaScript expression (using illegal quotes) is sanitized.")]
    public void ImageTagRemoveJavascriptSrcIllegalQuotes()
    {
      var html = "Should remove src (javascript:) " +
                 "<img src=`javascript:alert('xss');` />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsFalse(result.Contains("src="));
      Console.WriteLine(result);
    }

    #endregion

    #region "JavaScript Event Sanitization"

    [TestMethod]
    [Description("Tests that JavaScript events such as onclick and onmouseover are sanitized.")]
    public void JavaScriptEventSanitization()
    {
      var html = "<div onclick=\"alert('xss')\" onmouseover=\"alert('xss')\" class='test'>text</div>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsFalse(result.Contains("onclick=") || result.Contains("onmouseover="));
    }

    [TestMethod]
    [Description("Tests that JavaScript events such as onclick and onmouseover that are missing quote delimiters are sanitized.")]
    public void JavaScriptEventSanitizationMissingQuotes()
    {
      var html = "<div onclick=alert('xss') onmouseover=alert('xss') class='test'>text</div>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsFalse(result.Contains("onclick=") || result.Contains("onmouseover="));
    }

    #endregion

  }

}
