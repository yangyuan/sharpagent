using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace sharpagent
{
    public delegate void AsyncHttpListenerCallback(string path, ref byte[] body, ref string contenttype);

    public class AsyncHttpListener
    {

        private ManualResetEvent threadshutdown;
        private Thread thread;
        HttpListener listener;
        bool terminated;
        AsyncHttpListenerCallback callback;

        public AsyncHttpListener(uint port, AsyncHttpListenerCallback callback)
        {
            if (callback == null)
            {
                this.callback = new AsyncHttpListenerCallback(DefaultCallback);
            }
            else {
                this.callback = callback; 
            }
            
            terminated = false;
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + port + "/");

            threadshutdown = new ManualResetEvent(false);
            thread = new Thread(WorkerThreadFunc);
            thread.IsBackground = true;
            thread.Start();
        }
        public void Terminate(int millisecondsTimeout)
        {
            if (terminated) return;
            terminated = true;
            threadshutdown.Set();
            if (!thread.Join(millisecondsTimeout))
            {
                // thread failed to terminated itself
                thread.Abort();
                try
                {
                    listener.Abort();
                }
                catch { }
            }
        }

        private void WorkerThreadFunc()
        {
            listener.Start();
            while (!threadshutdown.WaitOne(0)) // 
            {
                IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), this);
                while (!result.AsyncWaitHandle.WaitOne(128))
                {
                    if (threadshutdown.WaitOne(0)) //
                    {
                        break;// force break, threadshutdown.WaitOne(0) will be called again and break outer loop
                    }
                }
            }
            listener.Stop();
        }

        private void ListenerCallback(IAsyncResult result)
        {
            try
            {
                HttpListenerContext context = listener.EndGetContext(result);
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                response.StatusCode = 200;
                byte[] body = new byte[0];
                string contenttype = "";
                callback(request.RawUrl, ref body, ref contenttype);
                response.ContentLength64 = body.Length;
                response.ContentType = contenttype;
                Stream output = response.OutputStream;
                output.Write(body, 0, body.Length);
                output.Flush();
                output.Close();
            }
            catch { }
        }

        private static void DefaultCallback(string path, ref byte[] body, ref string contenttype)
        {
            contenttype = "text/json";
            body = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
        }
    }
}
