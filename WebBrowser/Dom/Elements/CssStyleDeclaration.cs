﻿using System.Collections.Specialized;
using WebBrowser.ScriptExecuting;

namespace WebBrowser.Dom.Elements
{
	[DomItem]
	public interface ICssStyleDeclaration
	{
		string this[string name] { get; }
		string this[int idx] { get; }
		string GetPropertyValue(string propertyName);
	}

	/// <summary>
	/// http://www.w3.org/2003/01/dom2-javadoc/org/w3c/dom/css/CSSStyleDeclaration.html
	/// </summary>
	public class CssStyleDeclaration : ICssStyleDeclaration
	{
		public CssStyleDeclaration()
		{
			Properties = new NameValueCollection();
		}

		public NameValueCollection Properties { get; private set; }

		public string this[string name]
		{
			get
			{
				int number;
				if (int.TryParse(name, out number))
					return Properties.AllKeys[number]; //return key
				return Properties[name]; //return value
			}
			set
			{
				int number;
				if (int.TryParse(name, out number))
					Properties.AllKeys[number] = value;//todo: check behavior
				Properties[name] = value;
			}
		}

		public string this[int idx]
		{
			get { return Properties.AllKeys[idx]; }
		}

		public string GetPropertyValue(string propertyName)
		{
			return Properties[propertyName];
		}
	}
}