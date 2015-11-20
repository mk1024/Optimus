﻿using System;
using System.IO;
using System.Net;
using System.Text;
using WebBrowser.ResourceProviders;
using WebBrowser.ScriptExecuting;

namespace WebBrowser.Dom
{
	[DomItem]
	public interface IXmlHttpRequest
	{
		void Open(string method, string url, bool? async, string username, string password);
		int ReadyState { get; }
		Document ResponseXML { get; }
		string ResponseText { get; }
		string StatusText { get; }
		string ResponseType { get; }
		int Status { get; }
		void SetRequestHeader(string name, string value);
		string GetAllResponseHeaders();
		event Action OnReadyStateChange;
		event Action OnLoad;
		event Action OnError;
	}

	/// <summary>
	/// https://xhr.spec.whatwg.org/
	/// </summary>
	public class XmlHttpRequest : IXmlHttpRequest
	{
		private readonly IResourceProvider _resourceProvider;
		private readonly Func<object> _syncObj;
		private HttpRequest _request;
		private bool _async;
		private HttpResponse _response;
		private string _data;
		private int _readyState;

		public event Action OnTimeout;

		public XmlHttpRequest(IResourceProvider resourceProvider, Func<object> syncObj)
		{
			_resourceProvider = resourceProvider;
			_syncObj = syncObj;
			ReadyState = UNSENT;
		}

		public void Open(string method, string url)
		{
			Open(method, url, true, null, null);
		}

		public void Open(string method, string url, bool? async)
		{
			Open(method, url, async, null, null);
		}

		public void Open(string method, string url, bool? async, string username, string password)
		{
			_request = (HttpRequest)_resourceProvider.CreateRequest(url);
			_request.Method = method;
			_async = async ?? true;
			//todo: username, password
			ReadyState = OPENED;
		}

		public const ushort UNSENT = 0;
		public const ushort OPENED = 1;
		public const ushort HEADERS_RECEIVED = 2;
		public const ushort LOADING = 3;
		public const ushort DONE = 4;

		public int ReadyState
		{
			get { return _readyState; }
			private set
			{
				lock (this)
				{
					_readyState = value;
					CallInContext(OnReadyStateChange);
				}
			}
		}

		public Document ResponseXML { get
		{
			var doc = new Document();
			DocumentBuilder.Build(doc, new MemoryStream(Encoding.UTF8.GetBytes(_data)));
			return doc;
		}}

		public string ResponseText { get { return _data; } }

		public void SetRequestHeader(string name, string value)
		{
			if(ReadyState != OPENED)
				throw new Exception("The object state must be OPENEND");

			_request.Headers.Add(name, value);
		}

		public string GetAllResponseHeaders()
		{
			if (_response.Headers == null)
				return "";
			
			return _response.Headers.ToString();
		}

		public string StatusText
		{
			get
			{
				if (ReadyState != DONE || _response == null)
					return null;
				return _response.StatusCode.ToString();
			}
		}
		
		public string ResponseType
		{
			get
			{
				if(ReadyState != DONE || _response == null)
					return null;
					//throw new DOMException(DOMException.Codes.InvalidStateError);
				return _response.Type;
			}
		}

		public int Status { get { return _response == null ? UNSENT : (int)_response.StatusCode; } }

		public event Action OnReadyStateChange;
		public event Action OnLoad;
		public event Action OnError;

		public async void Send(object data)
		{
			//todo: use specified encoding
			//todo: fix data using
			if (data != null)
			{
				_request.Data = Encoding.UTF8.GetBytes(data.ToString());
			}

			if (_async)
			{
				ReadyState = LOADING;
				try
				{
					_response = (HttpResponse) await _resourceProvider.GetResourceAsync(_request);
					_data = _response.Stream.ReadToEnd();
				}
				catch (WebException w)
				{
					if (w.Status == WebExceptionStatus.Timeout)
					{
						CallInContext(OnTimeout);
					}
					ReadyState = DONE;
				}
				catch (Exception e)
				{
					ReadyState = DONE;
					CallInContext(OnError);
					return;
				}
				ReadyState = DONE;
				CallInContext(OnLoad);
			}
			else
			{
				try
				{
					var t = _resourceProvider.GetResourceAsync(_request);
					t.Wait();
					_response = (HttpResponse)t.Result;
					_data = _response.Stream.ReadToEnd();
				}
				catch (Exception exception)
				{
					ReadyState = DONE;
					CallInContext(OnError);
				}
				ReadyState = DONE;
				CallInContext(OnLoad);
			}
		}

		public void Send()
		{
			Send(null);
		}

		private void CallInContext(Action action)
		{
			lock (_syncObj())
			{
				action.Fire();
			}
		}

		public int Timeout
		{
			get
			{
				if (_request == null)
					throw new InvalidOperationException("Not opened");

				return _request.Timeout;
			}
			set
			{
				if (_request == null)
					throw new InvalidOperationException("Not opened");
				_request.Timeout = value;
			}
		}
	}

	public static class ActionExtension
	{
		public static void Fire(this Action action)
		{
			if (action != null) action();
		}
	}

	public static class StreamExtension
	{
		public static string ReadToEnd(this Stream stream)
		{
			using (var reader = new StreamReader(stream))
				return reader.ReadToEnd();
		}
	}
} 