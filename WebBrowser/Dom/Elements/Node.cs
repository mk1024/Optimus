﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WebBrowser.Dom
{
	public abstract class Node : INode
	{
		public INode AppendChild(INode node)
		{
			if (node is DocumentFragment)
			{
				foreach (var child in node.ChildNodes)
				{
					AppendChild(child);
				}
			}
			else
			{
				ChildNodes.Add(node);
				node.Parent = this;
			}
			return node;
		}

		protected Node()
		{
			InternalId = Guid.NewGuid().ToString();
			ChildNodes = new List<INode>();
		}

		public IList<INode> ChildNodes { get; protected set; }
		public string InternalId { get; private set; }
		public string Id { get; set; }

		public Node RemoveChild(Node node)
		{
			ChildNodes.Remove(node);
			return node;
		}

		public Node InsertBefore(Node newChild, Node refNode)
		{
			ChildNodes.Insert(ChildNodes.IndexOf(refNode), newChild);
			newChild.Parent = this;
			return newChild;
		}

		public bool HasChildNodes { get { return ChildNodes.Count > 0; } }

		public Node ReplaceChild(Node newChild, Node oldChild)
		{
			InsertBefore(newChild, oldChild);
			RemoveChild(oldChild);
			return newChild;
		}

		public INode FirstChild { get { return ChildNodes.FirstOrDefault(); } }
		public INode LastChild { get { return ChildNodes.LastOrDefault(); } }
		public INode NextSibling { get
		{
			if (Parent == null)
				return null;
			
			var idx = Parent.ChildNodes.IndexOf(this);
			if (idx == Parent.ChildNodes.Count - 1)
				return null;
			return Parent.ChildNodes[idx + 1];} }

		public INode PreviousSibling
		{
			get
			{
				var idx = Parent.ChildNodes.IndexOf(this);
				if (idx == 0)
					return null;
				return Parent.ChildNodes[idx- 1];
			}
		}

		public INode Parent { get; set; }
		public abstract INode CloneNode();

		public int NodeType { get; protected set; }
		public abstract string NodeName { get; }

		public const ushort ELEMENT_NODE = 1;
		public const ushort _NODE = 2;
		public const ushort TEXT_NODE = 3;
		public const ushort CDATA_SECTION_NODE = 4;
		public const ushort ENTITY_REFERENCE_NODE = 5;
		public const ushort ENTITY_NODE = 6;
		public const ushort PROCESSING_INSTRUCTION_NODE = 7;
		public const ushort COMMENT_NODE = 8;
		public const ushort DOCUMENT_NODE = 9;
		public const ushort DOCUMENT_TYPE_NODE = 10;
		public const ushort DOCUMENT_FRAGMENT_NODE = 11;
		public const ushort NOTATION_NODE = 12;


		public void AddEventListener(string type, Action<Event> listener, bool useCapture)
		{
			throw new NotImplementedException();
		}
		public void RemoveEventListener(string type, Action<Event> listener, bool useCapture)
		{
			throw new NotImplementedException();
		}

		public bool DispatchEvent(Event evt)
		{
			if (OnEvent != null)
				OnEvent(evt);
			return true;//todo: what we should return?
		}

		public event Action<Event> OnEvent;
	}

	public class Event
	{
		public string Type;
		public Node Target;
		public ushort EventPhase;
		public bool Bubbles;
		public bool Cancellable;
		public void StopPropagation()
		{
			throw new NotImplementedException();
		}

		public void PreventDefault()
		{
			throw new NotImplementedException();
		}

	}

	
}