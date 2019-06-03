using System;
using System.Web;
using System.Diagnostics;

using BasicRouter;

namespace TestServer
{
	public class SimpleHandler : IHttpHandler
	{
		private static SimpleRouter router = null;

		#region IHttpHandler Members
		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext ctx)
		{
			if (!router.InvokeAction(ctx)) 
				router.InvokeAction(ctx, "error", "details"); 
		}
		#endregion

		public SimpleHandler()
		{
			if (router == null)
			{
				router = new SimpleRouter(IsReusable);
				router.AddNamespace("TestServer.Controllers");
				router.OnActionException += new SimpleRouter.ActionExceptionHandler(OnActionException);
			}
		}

		private void OnActionException(HttpContext ctx, string action, Exception ex)
		{
			Exception e = ex.InnerException ?? ex;
			StackFrame frame = new StackTrace(e, true).GetFrame(0);

			string source, fileName = frame.GetFileName();
			if(fileName == null)
				source = "Not Available";
			else
				source = String.Format("{0}, <b>Line {1}</b>", fileName, frame.GetFileLineNumber());

			ctx.Response.Write(String.Format("<h3>Exception was raised while calling an action</h3><ul><li><b>Action:</b> {0}</li><li><b>Source:</b> {1}</li><li><b>Message:</b> <span style=\"color:Red;\">{2}</span></li></ul>", action, source, e.Message));
		}

	}
}
