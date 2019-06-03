using System;
using System.Collections.Generic;
using System.Web;
using System.IO;

namespace TestServer
{
	public enum ProcessFileResult
	{
		Sucess,

		Failed,
		
		NotFile
	}

	public class HttpFileCache
	{
		private Dictionary<string, FileCache> cache = new Dictionary<string, FileCache>();

		public ProcessFileResult ProcessFileRequest(HttpContext ctx)
		{
			string path = ctx.Request.PhysicalPath;
			FileInfo info = new FileInfo(path);
			if(!info.Exists)
				return ProcessFileResult.NotFile; 
			lock (cache)
			{
				FileCache fc;
				if (cache.ContainsKey(path))
				{
					fc = cache[path];
					if (fc.LastRead < info.LastWriteTime)
					{
						fc.Data = ReadFile(info);
						if (fc.Data == null)
						{
							cache.Remove(path);
							return ProcessFileResult.Failed;
						}
					}
				}
				else
				{
					fc = new FileCache()
					{
						Data = ReadFile(info),
						LastRead = DateTime.Now
					};
					if (fc.Data == null)
						return ProcessFileResult.Failed;
					cache.Add(path, fc); 
				}
				ctx.Response.OutputStream.Write(fc.Data, 0, fc.Data.Length); 
			}
			return ProcessFileResult.Sucess;
		}

		public void Reset()
		{
			lock (cache)
			{
				cache.Clear();
			}
		}

		private static byte[] ReadFile(FileInfo info)
		{
			try
			{
				FileStream fs = info.OpenRead();
				if (fs != null)
				{
					BinaryReader r = new BinaryReader(fs);
					byte[] data = r.ReadBytes((int)info.Length);
					r.Close();
					return data;
				}
			}
			catch (Exception)
			{
			}
			return null;
		}

		internal class FileCache
		{
			public byte[] Data;

			public DateTime LastRead;
		}
	}
}
