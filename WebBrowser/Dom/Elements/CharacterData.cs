﻿namespace WebBrowser.Dom
{
	public abstract class CharacterData : Node
	{
		public string Data;

		public string NodeValue { get { return Data; } set { Data = value; } }

	}

	public class Text : CharacterData
	{
		public Text()
		{
			NodeType = TEXT_NODE;
		}

		public override INode CloneNode()
		{
			//todo: attributes
			return new Text() { Data = Data };
		}

		public override string ToString()
		{
			return Data;
		}

		public override string NodeName
		{
			get { return "#text"; }
		}
	}

	public class Comment : CharacterData
	{
		public Comment()
		{
			NodeType = COMMENT_NODE;
		}

		public string Text { get { return Data; } }

		public override INode CloneNode()
		{
			//todo: attributes
			return new Comment() { Data = Data };
		}

		public override string NodeName
		{
			get { return "#comment"; }
		}
	}
}