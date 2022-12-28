using SimpleJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace GGFramework {
    public class RequestItem
    {
        public HttpNetworkSystem.RequestType requestType;

        public string httpAddress;

        public string command;

        public string paramData;

        public Encoding dataEncode;

        public HttpNetworkSystem.ExceptionAction exceptionAction;

        public Action<JsonObject> callback;

        public bool isRetry = false;//此请求是否在自动重试

        public int retryNum = 1;//重试次数

        public float retryTime = 1.0f;//重试间隔

        public RequestItem(HttpNetworkSystem.RequestType requestType, string httpAddress, string command, string paramData, Encoding dataEncode, HttpNetworkSystem.ExceptionAction exceptionAction, Action<JsonObject> callback)
        {
            this.requestType = requestType;
            this.httpAddress = httpAddress;
            this.command = command;
            this.paramData = paramData;
            this.dataEncode = dataEncode;
            this.exceptionAction = exceptionAction;
            this.callback = callback;
        }
    }

    /// <summary>
    /// //TODO: GL - 如果还要使用此方法， 网络IO的地方要改用异步方法！！！
    /// </summary>
    public class HttpThread : MonoBehaviour
    {

        public struct NetworkRequest
        {
            public int m_Dialogtype;
            public string m_Dialogcontext;
            public ShowDialogDelegate m_DialogcallBack;

            public RequestItem m_RequestItem;
            public RequestDelegate m_RequestcallBack;
            public HttpNetworkSystem.RequsetCallBackType m_RequestType;
            public string m_Command;
            public HttpNetworkSystem.ExceptionAction m_Exception;

            public Action<JsonObject> m_JsoncallBack;
            public JsonObject m_JsonObject;

            public void DoCallBack()
            {
                if (m_DialogcallBack != null)
                {
                    m_DialogcallBack(m_Dialogtype, m_Dialogcontext);
                    m_DialogcallBack = null;
                }

                if (m_RequestcallBack != null)
                {
                    m_RequestcallBack(m_RequestType, m_RequestItem);
                    m_RequestcallBack = null;
                }

                if (m_JsoncallBack != null)
                {
                    m_JsoncallBack(m_JsonObject);
                    m_JsoncallBack = null;
                }
            }
        }

        private Thread thread = null;

        public const int HTTP_REQUEST_TIME_OUT = 8000;

        private Queue<RequestItem> requestItemQueue = new Queue<RequestItem>();

        public delegate void RequestDelegate(HttpNetworkSystem.RequsetCallBackType type, RequestItem request);

        public delegate void ShowDialogDelegate(int type, string context);

        public ShowDialogDelegate ShowDialogCallBack = null;
        public RequestDelegate RequestCallBack = null;

        public Queue<NetworkRequest> callBackQue = new Queue<NetworkRequest>();

        private static bool IsStart = true;

        /// <summary>
        /// 停止线程
        /// </summary>
        public static void StopThread()
        {
            IsStart = false;
        }

        public static HttpThread Instance;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ClearQue();
            if (thread == null)
            {
                thread = new Thread(HttpRequestEvent);
                thread.Start();
            }
        }

        private void ClearQue()
        {
            requestItemQueue.Clear();
            callBackQue.Clear();
        }

        void Update()
        {
            if (callBackQue.Count > 0)
            {
                NetworkRequest item = callBackQue.Dequeue();
                item.DoCallBack();
                if (item.m_Exception != HttpNetworkSystem.ExceptionAction.Silence)
                {
                    //NetworkWaitingMask.ShowMask(false, item.m_Command);
                }
            }
        }

        private void HttpRequestEvent()
        {
            while (IsStart)
            {
                lock (requestItemQueue)
                {
                    lock (callBackQue)
                    {
                        if (requestItemQueue.Count > 0)
                        {
                            RequestItem requsetItem = requestItemQueue.Dequeue();
                            DoWebRequest(requsetItem);
                        }
                    }
                }
            }
        }

        //处理单条http请求
        public void DoWebRequest(RequestItem request)
        {
            if (LuaInject.HasPatch("HttpNetworkManager", "Request"))
            {
                object ret = LuaInject.CallPatch("HttpNetworkManager", "Request", new object[] {
                this
            });
                StopThread();
                ClearQue();
                return;
            }

            if (request == null)
            {
                return;
            }

            int code = 500;
            StreamReader reader = null;
            HttpWebResponse response = null;
            JsonObject data = null;

            try
            {
                Debug.Log(string.Format(request.httpAddress, request.command));
                Uri baseUri = new Uri(request.httpAddress);
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(baseUri, request.command));
                if (HttpNetworkSystem.Token != null)
                {
                    webReq.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + HttpNetworkSystem.Token);
                    //webReq.UseDefaultCredentials = true;
                    //webReq.PreAuthenticate = true;
                    //webReq.Credentials = CredentialCache.DefaultCredentials;
                }
                webReq.Timeout = HTTP_REQUEST_TIME_OUT;

                Stream newStream = null;
                if (request.requestType == HttpNetworkSystem.RequestType.Post)
                {
                    //GameDebugger.sPushLog(request.paramData);
                    webReq.Method = "POST";
                    //webReq.ContentType = "application/x-www-form-urlencoded";
                    webReq.ContentType = "application/json";
                    byte[] byteArray = request.dataEncode.GetBytes(request.paramData);
                    //转化
                    webReq.ContentLength = byteArray.Length;
                    newStream = webReq.GetRequestStream();
                    newStream.Write(byteArray, 0, byteArray.Length);    //TODO: GL - 如果还要使用此方法，这个地方要改用异步方法！！！
                                                                        //写入参数
                    newStream.Close();
                }
                else if (request.requestType == HttpNetworkSystem.RequestType.Get)
                {
                    webReq.Method = "GET";
                }
                response = (HttpWebResponse)webReq.GetResponse();
                reader = new StreamReader(response.GetResponseStream());
                string json = reader.ReadToEnd();       //TODO: GL - 如果还要使用此方法，这个地方要改用异步方法！！！
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                data = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(json);
                code = Convert.ToInt32(data["code"]);
                //GameDebugger.sPushLog(json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                //GameDebugger.sPushLog(e.ToString());
                if (request.exceptionAction == HttpNetworkSystem.ExceptionAction.ConfirmRetry)
                {

                }
                else if (request.exceptionAction == HttpNetworkSystem.ExceptionAction.AutoRetry)
                {
                    if (RequestCallBack != null)
                    {
                        NetworkRequest callBackItem = new NetworkRequest
                        {
                            m_RequestType = HttpNetworkSystem.RequsetCallBackType.RetryRequestCallback,
                            m_RequestItem = request,
                            m_Command = request.command,
                            m_Exception = HttpNetworkSystem.ExceptionAction.AutoRetry,
                            m_RequestcallBack = RequestCallBack
                        };

                        callBackQue.Enqueue(callBackItem);
                    }
                }
                else if (request.exceptionAction == HttpNetworkSystem.ExceptionAction.Silence)
                {

                }
                else if (request.exceptionAction == HttpNetworkSystem.ExceptionAction.Tips)
                {
                    if (ShowDialogCallBack != null)
                    {
                        NetworkRequest callBackItem = new NetworkRequest
                        {
                            m_DialogcallBack = ShowDialogCallBack,
                            m_Dialogcontext = request.command,
                            m_Dialogtype = 1
                        };
                        callBackQue.Enqueue(callBackItem);
                    }
                }
                return;
            }
            try
            {
                if (request.exceptionAction == HttpNetworkSystem.ExceptionAction.AutoRetry)
                {
                    if (RequestCallBack != null)
                    {
                        NetworkRequest callBackItem = new NetworkRequest
                        {
                            m_RequestcallBack = RequestCallBack,
                            m_RequestItem = request,
                            m_Command = request.command,
                            m_Exception = HttpNetworkSystem.ExceptionAction.AutoRetry,
                            m_RequestType = HttpNetworkSystem.RequsetCallBackType.NeedRemoveItemsAddCallBack
                        };
                        callBackQue.Enqueue(callBackItem);
                    }
                }

                if (NetworkConst.CODE_OK != code)
                {
                    if (ShowDialogCallBack != null)
                    {
                        NetworkRequest callBackItem = new NetworkRequest
                        {
                            m_Dialogtype = 2,
                            m_DialogcallBack = ShowDialogCallBack,
                            m_Dialogcontext = data["msg"].ToString()
                        };
                        callBackQue.Enqueue(callBackItem);
                    }
                    if (request.callback != null)
                    {
                        NetworkRequest callBackItem = new NetworkRequest
                        {
                            m_JsoncallBack = request.callback,
                            m_JsonObject = data
                        };
                        callBackQue.Enqueue(callBackItem);
                    }
                }
                else
                {
                    if (request.callback != null)
                    {
                        NetworkRequest callBackItem = new NetworkRequest
                        {
                            m_JsoncallBack = request.callback,
                            m_JsonObject = data
                        };
                        callBackQue.Enqueue(callBackItem);
                    }
                }
                reader.Close();
                response.Close();
            }
            catch (Exception e)
            {

            }
        }

        public void AddWebRequest(RequestItem request)
        {
            requestItemQueue.Enqueue(request);
        }

        void OnDestroy()
        {
            StopThread();
            ClearQue();
        }
    }
}
