using System;
using System.Web;
using System.Reflection;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Timers;

namespace BasicRouter
{
	public class BaseController
	{
		protected HttpContext ctx { get; set; }
		protected string[] prefix { get; set; }
		protected void Write(string txt)
		{
			ctx.Response.Write(txt);
		}
		protected string GetQueryPath()
		{
			return ctx.Request.AppRelativeCurrentExecutionFilePath.Replace("~/", "").ToLower();
		}
	}
	public class SimpleRouter
	{
		public char arraySeparator = ',';
		public bool ReuseControllers
		{
			get
			{
				lock (safe)
					return _reuseControllers;
			}
			set
			{
				lock (safe)
					_reuseControllers = value;
			}
		}
		public SimpleRouter(bool reuseControllers = true)
		{
			ReuseControllers = reuseControllers;
		}
		public void AddNamespace(string ns, Assembly asm)
		{
			namespaces.Add(new Namespace(ns, asm));
		}
		public void AddNamespace(string ns, string asm)
		{
			namespaces.Add(new Namespace(ns, asm));
		}
		public void AddNamespace(string ns)
		{
			namespaces.Add(new Namespace(ns, Assembly.GetCallingAssembly()));
		}
		public void SetTimeout(ushort life)
		{
			TimeOut = life;
			if (TimeOut == 0)
			{
				if (timer != null)
				{
					timer.Stop();
					timer = null;
				}
				return;
			}
			double frequency = TimeOut * 100; 
			if (timer == null)
			{
				timer = new Timer(frequency);
				timer.Elapsed += new ElapsedEventHandler(OnCheckExpiration);
				timer.Start();
			}
			else
				timer.Interval = frequency;
		}
		public bool InvokeAction(HttpContext ctx)
		{
			string[] segments = GetSegments(ctx.Request);
			if (segments == null)
				return false; 

			int nps = segments.Length - 2; 

			// Extracting {controller/action} segments:
			string controller = segments[nps];
			string action = segments[nps + 1];
			// Copying prefix segments, if available:
			string[] prefix = new string[nps];
			Array.Copy(segments, prefix, nps);
			return InvokeActionInternal(ctx, controller, action, null, prefix);
            
		}
		public bool InvokeAction(HttpContext ctx, string controller, string action, object[] parameters = null, string[] prefix = null)
		{
			string ctrl = CleanSegment(controller);
			string act = CleanSegment(action);

			if (string.IsNullOrEmpty(ctrl) || string.IsNullOrEmpty(act))
				return false; // Cannot process empty controller or action;

			return InvokeActionInternal(ctx, ctrl, act, parameters, prefix);
		}
		protected virtual bool OnValidatePrefix(string[] prefix, ref Namespace nsOverride)
		{
			return true;
		}
		private bool InvokeActionInternal(HttpContext ctx, string controller, string action, object[] parameters, string[] prefix)
		{
			string[] pref = prefix;
			if (pref == null)
			{
				string[] segments = GetSegments(ctx.Request);
				if (segments == null)
					pref = new string[0];
				else
				{
					int n = segments.Length - 2; // Number of prefix segments;
					pref = new string[n];
					Array.Copy(segments, pref, n);
				}
			}

			Namespace nsOverride = null;
			if (!OnValidatePrefix(pref, ref nsOverride))
				return false; // Prefix segment validation failed;

			Type t = FindControllerType(controller, nsOverride); // Locating the controller's Type;
			if (t == null)
				return false; // Controller's Type not found;

			MethodInfo info = t.GetMethod(action, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.InvokeMethod);
			if (info == null)
				return false; // Action not found;

			object[] prm = parameters;
			if (prm == null)
			{
				prm = PrepareActionParameters(info.GetParameters(), ctx.Request.QueryString);
				if (prm == null)
					return false;
			}

			string signature = t.Module.Name + "/" + t.FullName; // Unique signature of assembly+namespace+controller;

			CacheHolder ch = GetFromCache(signature); 
			if (ch == null)
				ch = new CacheHolder();

			if (ch.instance == null)
				ch.instance = Activator.CreateInstance(t); 

			ch.lastUse = DateTime.Now; 

			BindingFlags flags = BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance;
			t.InvokeMember("ctx", flags, null, ch.instance, new[] { ctx });
			t.InvokeMember("prefix", flags, null, ch.instance, new[] { pref });

			try
			{
				info.Invoke(ch.instance, prm); 
			}
			catch (Exception ex)
			{
				if (OnActionException != null)
				{
					string a = info.ReflectedType.FullName + "." + info.Name; 
					OnActionException(ctx, a, ex); 
				}
			}

			AddToCache(signature, ch); 

			return true; 
		}

		private Type FindControllerType(string ctrlName, Namespace nsOverride)
		{
			if (nsOverride != null && nsOverride.IsValid)
				return GetControllerType(ctrlName, nsOverride);

			foreach (Namespace ns in namespaces)
			{
				Type t = GetControllerType(ctrlName, ns);
				if (t != null)
					return t;
			}
			return null;
		}

		private Type GetControllerType(string ctrlName, Namespace ns)
		{
			Type t = null;
			string name = ctrlName; 
			if (!string.IsNullOrEmpty(ns.name))
				name = ns.name + "." + ctrlName; 

			t = ns.asm.GetType(name, false, true);
			if (t == null)
				t = ns.asm.GetType(name + "Controller", false, true); 
			if (t != null && (!t.IsClass || t.BaseType != typeof(BaseController)))
				return null; 
			return t;
		}

		private object[] PrepareActionParameters(ParameterInfo[] pi, NameValueCollection nvc)
		{
			List<object> parameters = new List<object>(); 
			foreach (ParameterInfo p in pi)
			{
				object obj = nvc.Get(p.Name); 
				if (string.IsNullOrEmpty((string)obj))
				{
					if (!p.IsOptional)
						return null; 

					parameters.Add(p.DefaultValue);
					continue;
				}
				if (p.ParameterType != typeof(string))
				{
					try
					{
						if (p.ParameterType.IsArray)
						{
							string[] str = ((string)obj).Split(arraySeparator);
							Type baseType = p.ParameterType.GetElementType();
							Array arr = Array.CreateInstance(baseType, str.Length);
							int idx = 0;
							foreach (string s in str)
								arr.SetValue(Convert.ChangeType(s, baseType), idx++);
							obj = arr;
						}
						else
						{
							Type t = p.ParameterType;
							if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) // If parameter is nullable;
								t = Nullable.GetUnderlyingType(t); 

							obj = Convert.ChangeType(obj, t); 
						}
					}
					catch (Exception)
					{
						return null; 
					}
				}
				parameters.Add(obj);
			}
			return parameters.ToArray(); 
		}
		private void OnCheckExpiration(object sender, ElapsedEventArgs e)
		{
			lock (safe)
			{
				if (ctrlCache == null)
					return;
				List<string> expired = new List<string>();
				foreach (string s in ctrlCache.Keys)
				{
					if (DateTime.Now - ctrlCache[s].lastUse > TimeSpan.FromSeconds(TimeOut))
						expired.Add(s);
				}
				foreach (string s in expired)
					ctrlCache.Remove(s);
			}
		}

		private bool AddToCache(string key, CacheHolder ch)
		{
			lock (safe)
			{
				if (ReuseControllers)
				{
					if (ctrlCache == null)
						ctrlCache = new Dictionary<string, CacheHolder>();

					if (!ctrlCache.ContainsKey(key))
					{
						ctrlCache.Add(key, ch);
						return true; 
					}
				}
			}
			return false; 
		}

		private CacheHolder GetFromCache(string key)
		{
			lock (safe)
			{
				if (ReuseControllers && ctrlCache != null && ctrlCache.ContainsKey(key))
					return ctrlCache[key];
			}
			return null;
		}

		private static string[] GetSegments(HttpRequest request)
		{
			string[] segments = request.AppRelativeCurrentExecutionFilePath.Replace("~/", "").Split('/');
            if (segments.Length < 2)
				return null;

			for (int i = 0; i < segments.Length; i++)
				segments[i] = CleanSegment(segments[i]);

			return segments;
		}

		private static string CleanSegment(string segment)
		{
			if (segment == null)
				return null;
			
			return segment.Replace(" ", "").ToLower();
		}

		private List<Namespace> namespaces = new List<Namespace>();

		private Dictionary<string, CacheHolder> ctrlCache = null;

		private Timer timer = null;

		private Object safe = new Object();

		private bool _reuseControllers;

		private ushort TimeOut = 0;

		public delegate void ActionExceptionHandler(HttpContext ctx, string action, Exception ex);

		public event ActionExceptionHandler OnActionException;

		internal class CacheHolder
		{
			public object instance = null;

			public DateTime ? lastUse = null;
		}
	}

	public class Namespace
	{
		public Namespace(string ns, Assembly asm)
		{
			this.name = ns;
			this.asm = asm;
		}

		public Namespace(string ns, string asm)
		{
			this.name = ns;
			this.asm = Assembly.Load(asm);
		}

		public Namespace(string ns)
		{
			this.name = ns;
			this.asm = Assembly.GetCallingAssembly();
		}

		public bool IsValid
		{
			get
			{
				return !string.IsNullOrEmpty(name) && asm != null;
			}
		}

		public string name;

		public Assembly asm;
	}

}
