using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ZyGames.Framework.Common.Log;

namespace GameServer.CsScript.Logic
{
    public class GMService
    {
        #region 单例
        public static GMService Current;
        static GMService()
        {
            Current = new GMService();
        }
        private GMService()
        {
            httpListener = new HttpListener();
        }
        #endregion

        private HttpListener httpListener;

        /// <summary>
        /// 请求方式:http://127.0.0.1:8080/Service/?CMD=1001|1,2,3
        /// </summary>
        /// <param name="address">http://127.0.0.1</param>
        /// <param name="port">8080</param>
        /// <param name="httpName">Service</param>
        public void Start(string address, int port, string httpName)
        {
            try
            {
                string url = string.Format("{0}:{1}/{2}/", address, port, httpName);
                httpListener.Prefixes.Add(url);
                httpListener.Start();
                httpListener.BeginGetContext(OnHttpRequest, httpListener);
                TraceLog.WriteInfo("GM服务启动成功!");
            }
            catch(Exception ex)
            {
                TraceLog.WriteError("GM服务器启动失败,\n{0}\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());
            }
        }

        #region http server

        private void OnHttpRequest(IAsyncResult result)
        {
            string ErrorCode = "1";
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            listener.BeginGetContext(OnHttpRequest, listener);
            try
            {
                string address = context.Request.RemoteEndPoint.Address.ToString();
                AutoResetEvent waitHandle = new AutoResetEvent(false);
                int index = context.Request.RawUrl.IndexOf("?CMD=", StringComparison.OrdinalIgnoreCase);
               if(index != -1)
                {
                    string cmd = context.Request.RawUrl.Substring(index + 5);
                    cmd = HttpUtility.UrlDecode(cmd);
                    ErrorCode = DoExecCmd(address, cmd);
                }

            }
            catch (Exception ex)
            {
                TraceLog.WriteError("OnHttpRequest error:{0}", ex);
            }
            finally
            {
                context.Response.ContentType = "application/octet-stream";
                StreamWriter output = new StreamWriter(context.Response.OutputStream);
                output.Write(ErrorCode);
                output.Close();
                context.Response.Close();
            }
        }

        #endregion

        #region GM fun
        /// <summary>
        /// 执行指令 格式 "001|1,2,3"
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public string DoExecCmd(string address, string cmd)
        {
            TraceLog.WriteInfo("{0}执行指令:{1}", address, cmd);
            string errorCode = "1";
            string[] cmds = cmd.Split('|');
            switch (cmds[0])
            {
                case "0001":
                    if (cmds.Length != 2)
                        break;
                    break;
                case "0002":
                    if (cmds.Length != 3)
                        break;
                    break;
            }
            return errorCode;
        }
        #endregion
    }
}
