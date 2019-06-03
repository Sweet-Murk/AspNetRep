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
