﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebBrowser.Dom.Elements;
using WebBrowser.Html;

namespace WebBrowser.Dom
{
	internal class DocumentBuilder
	{
		public static void Build(Node parentNode, string htmlString)
		{
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(htmlString)))
			{
				Build(parentNode, stream);
			}
		}

		public static void Build(Node parentNode, Stream stream)
		{
			var html = HtmlParser.Parse(stream);
			Build(parentNode, html);
		}

		public static void Build(Document parentNode, Stream stream)
		{
			var html = ExpandHtmlTag(HtmlParser.Parse(stream));
			Build(parentNode.DocumentElement, html);
		}

		private static IEnumerable<IHtmlNode> ExpandHtmlTag(IEnumerable<IHtmlNode> parse)
		{
			foreach (var htmlNode in parse)
			{
				var tag = htmlNode as Html.HtmlElement;
				if (tag != null && tag.Name.ToLowerInvariant() == "html")
				{
					foreach (var child in tag.Children)
					{
						yield return child;
					}
				}
				else
				{
					yield return htmlNode;
				}
			}
		}

		public static void Build(Node parentNode, IEnumerable<IHtmlNode> htmlElements)
		{
			foreach (var htmlElement in htmlElements)
			{
				BuildElem(parentNode, htmlElement);
			}
		}

		private static Node BuildElem(Node node, IHtmlNode htmlNode)
		{
			var comment = htmlNode as HtmlComment;
			if (comment != null)
				return node.OwnerDocument.CreateComment(comment.Text);
			
			var txt = htmlNode as IHtmlText;
			if (txt != null)
				return node.OwnerDocument.CreateTextNode(txt.Value);

			var htmlElement = htmlNode as Html.IHtmlElement;
			if (htmlElement == null)
				return null;

			var elem = node.OwnerDocument.CreateElement(htmlElement.Name);

			if (elem is Script)
			{
				var htmlText = htmlElement.Children.FirstOrDefault() as IHtmlText;
				elem.InnerHTML = htmlText != null ? htmlText.Value : string.Empty;
			}
			
			foreach (var attribute in htmlElement.Attributes)
			{
				elem.SetAttribute(attribute.Key, attribute.Value);
			}

			node.AppendChild(elem);

			Build(elem, htmlElement.Children);

			return elem;
		}
	}
}
