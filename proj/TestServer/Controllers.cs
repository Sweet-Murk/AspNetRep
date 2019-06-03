using System;
using System.IO;
using BasicRouter;

namespace TestServer.Controllers
{
	public class ParkOwnerController : BaseController
	{
        public void setWork(string worker,string work)
        {
            Time();
            Write(String.Format("<h1> {0}, today you must {1}!</h1>", worker,work));

        }

		public void Time()
		{
			Write(DateTime.Now.ToString("MMM dd, yyyy; HH:mm:ss.fff"));
		}

		public void Exception(string msg)
		{
			throw new Exception(msg);
		}

        public void WorkerProccess()
        { }
	}


	public class ParkWorkerController : BaseController
	{

		public void workproccess(string proccess)
		{
            if (proccess == "done" || proccess == "not")
            {
                if (proccess == "done")
                    Write(String.Format("You done all work"));
                else Write(String.Format("Return back when you would done it!"));
            }
            else throw new Exception("You just need write only done\not");
		}

		public void Add(double [] values, string units = null)
		{
			double total = 0;
			foreach (double d in values)
				total += d;
			Write(String.Format("Total: {0} {1}", total, units));
		}

		public void Text(string[] values, string color = "Green")
		{
			string result = String.Format("<p style=\"color:{0};\">", color);
			foreach(string s in values)
				result += s + "<br/>";
			result += "</p>";
			Write(result);
		}

		public void Any(object[] values, string desc = null)
		{
			string s = (desc ?? "") + "<ol>";
			foreach (object obj in values)
				s += "<li>" + obj.ToString() + "</li>";
			Write(s + "</ol>");
		}
	}


	public class ImageController : BaseController
	{
		public void Diagram()
		{
			if (image == null)
				image = FileToByteArray(ctx.Server.MapPath("~/Routing.jpg"));

			ctx.Response.ContentType = "image/jpeg";
			ctx.Response.BinaryWrite(image);
		}
		private static byte[] FileToByteArray(string fileName)
		{
			FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
			long nBytes = new FileInfo(fileName).Length;
			return new BinaryReader(fs).ReadBytes((int)nBytes);
		}

		private byte[] image = null; 
	}
	public class ErrorController:BaseController
	{
		public void Details()
		{
			string path = GetQueryPath();
			if (string.IsNullOrEmpty(path))
				path = "<span style=\"color:Red;\">Empty</span>";

			string msg = String.Format("<p>Failed to process request: <b>{0}</b></p>", path);
			msg += "<p>Passed Parameters:";
			if (ctx.Request.QueryString.Count > 0)
			{
				msg += "</p><ol>";
				foreach (string s in ctx.Request.QueryString)
					msg += String.Format("<li>{0} = {1}</li>", s, ctx.Request.QueryString[s]);
				msg += "</ol>";
			}
			else
				msg += " <b>None</b></p>";

			Write(msg);
		}
	}
}
