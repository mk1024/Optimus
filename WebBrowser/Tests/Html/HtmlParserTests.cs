﻿#if NUNIT
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using WebBrowser.Dom;
using System.IO;
using WebBrowser.Html;
using Text = WebBrowser.Dom.Text;

namespace WebBrowser.Tests.Html
{
	[TestFixture]
	public class HtmlParserTests
	{
		IEnumerable<IHtmlNode> Parse(string str)
		{
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
			{
				return HtmlParser.Parse(stream).ToArray();
			}
		}

		[Test]
		public void SimpleElement()
		{
			var elem = Parse("<p id='8'>Text</p>").Cast<IHtmlElement>().Single();

			Assert.AreEqual("p", elem.Name);
			Assert.AreEqual("Text", ((IHtmlText)elem.Children.Single()).Value);
			Assert.AreEqual(1, elem.Attributes.Count);
		}

		[TestCase("<script>alert('1');</script>", "alert('1');")]
		[TestCase("<script>var html = '<div></div>';</script>", "var html = '<div></div>';")]
		[TestCase("<script>var html = '<div>';</script>", "var html = '<div>';")]
		[TestCase("<script>var html = '<div />';</script>", "var html = '<div />';")]
		[TestCase("<script>var html = '<script>console.log(1);</script>';</script>", "var html = '<script>console.log(1);</script>';")]
		//todo: escaped chars
		public void EmbeddedScript(string html, string scriptText)
		{
			var elem = Parse(html).Cast<IHtmlElement>().Single();

			Assert.AreEqual("script", elem.Name);
			Assert.AreEqual(scriptText, ((IHtmlText)elem.Children.Single()).Value);
			Assert.AreEqual(0, elem.Attributes.Count);
		}

		[Test]
		public void Text()
		{
			var elems = Parse("Hello").ToArray();

			Assert.AreEqual(1, elems.Length);
			var elem = elems[0];
			Assert.AreEqual("Hello", ((IHtmlText)elem).Value);
		}
	}
}
#endif