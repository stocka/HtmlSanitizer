using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.Web.Utilities;

namespace HtmlSanitizerTests
{
  [TestClass]
  public class HtmlSanitizerTests
  {

    #region "Style Attribute Sanitization"

    [TestMethod]
    [Description("Ensures that style attributes that contain valid CSS are preserved.")]
    public void StyleAttributeValid()
    {
      var html = "<div style=\"color: red;\" ></div>";
      string result = HtmlSanitizer.SanitizeHtml(html);

      // Style attribute should be preserved
      Assert.IsTrue(result.Contains("style="));
    }

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
    [Description("Ensures that style elements that contain valid CSS are preserved.")]
    public void StyleElementValid()
    {
      var html = "<style type=\"text/css\">" +
        "div { color: red; background-image: url('data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAABGdBTUEAAK/INwWK6QAAABl0RVh0U29mdHdhcmUAQWRvYmUgSW1hZ2VSZWFkeXHJZTwAAADXSURBVHjaYmCgEDDjkTMAYj8gVgfiX0D8llhDtYD4BhD/R8OHgZgfXTETFgPu4HDZFyD+SKwrvNFs/wP1EklgC5IBS3EpYsRjgAo0EDmBeBkQ38emiAWPAaCweARl3yfkApBNEkjioMDaAY06UICKAbETWiy8AOJNMM4utED7BlWcD8X8UDFkNbuQXTINS7wvQpJfikV+GrIBTliizRhJ3hgqhqzGCT08NiJJTsASXpOR5DdiC1AhID4GxE+AmAeLPA9U7hhULVbABs08+PIJGwM1AUCAAQBwgESf0bWQYwAAAABJRU5ErkJggg==') }" +
        "</style>";
      string result = HtmlSanitizer.SanitizeHtml(html);

      // Style element should be preseved
      Assert.IsTrue(result.Contains("<style"));
    }

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
    [Description("Ensures that head tags are removed entirely.")]
    public void HeadTagRemoval()
    {
      var html = "<head><title>Hello, is it me you're looking for?</title></head>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      // Head element should be removed
      Assert.IsFalse(result.Contains("<head"));
    }

    [TestMethod]
    [Description("Ensures that link tags are removed entirely.")]
    public void LinkTagRemoval()
    {
      var html = "<link rel=\"stylesheet\" href=\"http://example.com/stylesheet.css\" type=\"text/css\" />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      // Link element should be removed
      Assert.IsFalse(result.Contains("<link "));
    }

    [TestMethod]
    [Description("Ensures that meta tags are removed entirely.")]
    public void MetaTagRemoval()
    {
      var html = "<meta name=\"GENERATOR\" content=\"Microsoft FrontPage 4.0\" />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      // Head element should be removed
      Assert.IsFalse(result.Contains("<meta "));
    }

    [TestMethod]
    [Description("Ensures that script tags are removed entirely.")]
    public void ScriptTagRemoval()
    {
      var html = "<script>alert('hello');</script>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsFalse(result.Contains("<script>") || result.Contains("</script>"));
    }

    [TestMethod]
    [Description("Ensures that frameset and frame tags are removed entirely.")]
    public void FramesetAndFrameRemoval()
    {
      var html = "<frameset>" +
          "<frame src=\"http://example.com/frame1.html\" />" +
          "<frame src=\"http://example.com/frame2.html\" />" +
          "</frameset>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsFalse(result.Contains("<frameset>") || result.Contains("</frameset>"));
      Assert.IsFalse(result.Contains("<frame ") || result.Contains("</frame>"));
    }

    [TestMethod]
    [Description("Ensures that iframe tags are removed entirely.")]
    public void IFrameRemoval()
    {
      var html = "<iframe src=\"http://example.com\"></iframe>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      // iframe should be removed
      Assert.IsFalse(result.Contains("<iframe>") || result.Contains("</iframe>"));
    }

    [TestMethod()]
    [Description("Ensures that embeddable tags (embed, object, applet) are removed entirely.")]
    public void EmbeddableTagsRemoval()
    {
      var html = "<embed type=\"video/quicktime\" src=\"movie.mov\" width=\"640\" height=\"480\">" +
        "<object data=\"move.swf\" type=\"application/x-shockwave-flash\"></object>" +
        "<applet code=\"game.class\" archive=\"game.zip\" height=\"250\" width=\"350\"></applet>";

      string result = HtmlSanitizer.SanitizeHtml(html);

      // Embeds, applets, and objects should be removed.
      Assert.IsFalse(result.Contains("<embed ") || result.Contains("</embed>"));
      Assert.IsFalse(result.Contains("<object ") || result.Contains("</object>"));
      Assert.IsFalse(result.Contains("<applet ") || result.Contains("</applet>"));
    }

    #endregion

    #region "Anchor Tag Sanitization"

    [TestMethod]
    [Description("Ensures that an anchor tag with a valid href is preserved.")]
    public void AnchorTagValid()
    {
      var html = "<a href=\"http://example.com/\" />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsTrue(result.Contains("href="));
    }

    [TestMethod]
    [Description("Ensures that an anchor tag with a JavaScript href is sanitized.")]
    public void AnchorTagRemoveJavascriptHref()
    {
      var html = "<a href=\"javascript:alert('xss');\" />";

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
    [Description("Ensures that a valid src attribute, using a data URI, of an image is preserved.")]
    public void ImageTagValidDataUri()
    {
      var html = "<img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAABGdBTUEAAK/INwWK6QAAABl0RVh0U29mdHdhcmUAQWRvYmUgSW1hZ2VSZWFkeXHJZTwAAADXSURBVHjaYmCgEDDjkTMAYj8gVgfiX0D8llhDtYD4BhD/R8OHgZgfXTETFgPu4HDZFyD+SKwrvNFs/wP1EklgC5IBS3EpYsRjgAo0EDmBeBkQ38emiAWPAaCweARl3yfkApBNEkjioMDaAY06UICKAbETWiy8AOJNMM4utED7BlWcD8X8UDFkNbuQXTINS7wvQpJfikV+GrIBTliizRhJ3hgqhqzGCT08NiJJTsASXpOR5DdiC1AhID4GxE+AmAeLPA9U7hhULVbABs08+PIJGwM1AUCAAQBwgESf0bWQYwAAAABJRU5ErkJggg==\" />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsTrue(result.Contains("src="));
      Console.WriteLine(result);
    }

    [TestMethod]
    [Description("Ensures that a src attribute of an image that contains a JavaScript expression is sanitized.")]
    public void ImageTagRemoveJavascriptSrc()
    {
      var html = "<img src=\"javascript:alert('xss');\" />";

      string result = HtmlSanitizer.SanitizeHtml(html);

      Assert.IsFalse(result.Contains("src="));
      Console.WriteLine(result);
    }

    [TestMethod]
    [Description("Ensures that a src attribute of an image that contains a JavaScript expression (using illegal quotes) is sanitized.")]
    public void ImageTagRemoveJavascriptSrcIllegalQuotes()
    {
      var html = "<img src=`javascript:alert('xss');` />";

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
