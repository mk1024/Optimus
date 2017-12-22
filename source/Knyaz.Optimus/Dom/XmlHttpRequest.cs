﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Knyaz.Optimus.Dom.Events;
using Knyaz.Optimus.ResourceProviders;
using Knyaz.Optimus.ScriptExecuting;
using Knyaz.Optimus.Tools;

namespace Knyaz.Optimus.Dom
{
	/// <summary>
	/// https://xhr.spec.whatwg.org/
	/// </summary>
	[DomItem]
	public class XmlHttpRequest 
	{
		private readonly IResourceProvider _resourceProvider;
		private readonly Func<object> _syncObj;
		private HttpRequest _request;
		private bool _async;
		private HttpResponse _response;
		private string _data;
		private int _readyState;
		
		internal XmlHttpRequest(IResourceProvider resourceProvider, Func<object> syncObj)
		{
			_resourceProvider = resourceProvider;
			_syncObj = syncObj;
			ReadyState = UNSENT;
		}

		/// <summary>
		/// Called whenever the request times out.
		/// </summary>
		public event Action OnTimeout;
		
		/// <summary>
		///called whenever the readyState attribute changes. 
		/// </summary>
		public event Action OnReadyStateChange;
		public event Action<Event> OnLoad;
		public event Action OnError;

		/// <summary>
		/// initializes a request.
		/// </summary>
		/// <param name="method">The HTTP method to use, such as "GET", "POST", "PUT", "DELETE", etc.</param>
		/// <param name="url">The URL to send the request to.</param>
		/// <param name="async">If this value is false, the method does not return until the response is received</param>
		/// <param name="username">The user name to use for authentication purposes.</param>
		/// <param name="password">The password to use for authentication purposes.</param>
		public void Open(string method, string url, bool? async = null, string username = null, string password = null)
		{
			_request = (HttpRequest)_resourceProvider.CreateRequest(url);
			_request.Method = method;
			_async = async ?? true;
			//todo: username, password
			ReadyState = OPENED;
		}

		/// <summary>
		/// ReadyState = 0 - Client has been created. Open() not called yet.
		/// </summary>
		public const ushort UNSENT = 0;
		
		/// <summary>
		/// ReadyState = 1 - Open() has been called
		/// </summary>
		public const ushort OPENED = 1;
		
		/// <summary>
		/// ReadyState = 2 - Send() has been called, and headers and status are available.
		/// </summary>
		public const ushort HEADERS_RECEIVED = 2;
		
		/// <summary>
		/// ReadyState = 3 - ResponseText holds partial data
		/// </summary>
		public const ushort LOADING = 3;
		
		/// <summary>
		/// ReadyState = 4 - The operation is complete.
		/// </summary>
		public const ushort DONE = 4;

		/// <summary>
		/// Gets the state an XMLHttpRequest client is in. 
		/// </summary>
		public int ReadyState
		{
			get => _readyState;
			private set
			{
				lock (this)
				{
					_readyState = value;
					CallInContext(OnReadyStateChange);
				}
			}
		}

		/// <summary>
		/// Gets a Document containing the HTML or XML retrieved by the request.
		/// </summary>
		public Document ResponseXML 
		{ 
			get
			{
				if (ReadyState != DONE)
					return null;
				
				//todo: take into account content type.
				var doc = new Document();
				DocumentBuilder.Build(doc, new MemoryStream(Encoding.UTF8.GetBytes(_data)));
				return doc;
			}
		}

		/// <summary>
		/// Gets the response to the request as text, or null if the request was unsuccessful or has not yet been sent.
		/// </summary>
		public string ResponseText => _data;

		/// <summary>
		/// Sets the value of an HTTP request header. You must call SetRequestHeader() after Open(), but before Send().
		/// </summary>
		public void SetRequestHeader(string name, string value)
		{
			if(ReadyState != OPENED)
				throw new Exception("The object state must be OPENEND");

			_request.Headers.Add(name, value);
		}

		/// <summary>
		/// Returns all the response headers, separated by CRLF, as a string, or null if no response has been received.
		/// </summary>
		/// <returns></returns>
		public string GetAllResponseHeaders()
		{
			if (_response.Headers == null)
				return "";

			var headersString = _response.Headers.ToString();

			//todo: probably the hack below is no more required due to JINT's reges was fixed.
			//to fix jquery we should remove \r due to jquery uses .net regex where \r\n is not threated as end line
			headersString = headersString.Replace("\r", "");

			return headersString;
		}

		/// <summary>
		/// Returns a string containing the response string returned by the HTTP server. 
		/// </summary>
		public string StatusText
		{
			get
			{
				if (ReadyState != DONE || _response == null)
					return null;
				return _response.StatusCode.ToString();
			}
		}
		
		/// <summary>
		/// Gets the response type.
		/// </summary>
		public string ResponseType { get; }

		/// <summary>
		/// Gets the numerical standard HTTP status code of the response of the XMLHttpRequest.
		/// </summary>
		public int Status => _response == null ? UNSENT : (int)_response.StatusCode;

		/// <summary>
		/// Sends the request.
		/// </summary>
		/// <param name="data">A body of data to be sent in the XHR request.</param>
		public async void Send(object data = null)
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
					_response = (HttpResponse) await _resourceProvider.SendRequestAsync(_request);
					//todo: convert response according to specified responseType.
					_data = _response.Stream.ReadToEnd();
				}
				catch (AggregateException a)
				{
					var web = a.Flatten().InnerExceptions.OfType<WebException>().FirstOrDefault();
					if (web != null)
					{
						_data = web.Response.GetResponseStream().ReadToEnd();
					}
				}
				catch (WebException w)
				{
					if (w.Status == WebExceptionStatus.Timeout)
					{
						CallInContext(OnTimeout);
					}
				}
				catch
				{
					ReadyState = DONE;
					CallInContext(OnError);
					return;
				}
				ReadyState = DONE;
				FireOnLoad();
			}
			else
			{
				try
				{
					var t = _resourceProvider.SendRequestAsync(_request);
					t.Wait();
					_response = (HttpResponse)t.Result;
					_data = _response.Stream.ReadToEnd();
				}
				catch
				{
					ReadyState = DONE;
					CallInContext(OnError);
				}
				ReadyState = DONE;
				FireOnLoad();
			}
		}

		private void FireOnLoad()
		{
			if (OnLoad != null)
			{
				var evt = new ProgressEvent("load");
				evt.InitProgressEvent(true, (ulong) _data.Length, (ulong) _data.Length);
				lock (_syncObj())
					OnLoad(evt);
			}
		}

		private void CallInContext(Action action)
		{
			lock (_syncObj())
			{
				action?.Invoke();
			}
		}

		/// <summary>
		/// Gets or sets the number of milliseconds a request can take before automatically being terminated.
		/// </summary>
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
} 