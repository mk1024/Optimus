﻿namespace Knyaz.Optimus.Dom.Elements
{
	/// <summary>
	/// <seealso cref="https://www.w3.org/2003/01/dom2-javadoc/org/w3c/dom/html2/HTMLBRElement.html"/>
	/// </summary>
	public sealed class HtmlBrElement : HtmlElement
	{
		internal HtmlBrElement(Document ownerDocument) : base(ownerDocument, TagsNames.Br){}

		/// <summary>
		/// Gets or sets the 'clear' attribute value that specifies where the next line should appear in a 
		/// visual browser after the line break caused by this element. 
		/// </summary>
		/// <remarks>Values: none|left|right|all</remarks>
		public string Clear
		{
			get => GetAttribute("clear", string.Empty);
			set => SetAttribute("clear", value);
		}
	}
}
