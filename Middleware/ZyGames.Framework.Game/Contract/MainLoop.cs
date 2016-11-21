using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZyGames.Framework.Common.Log;
using ZyGames.Framework.Game.Lang;
using ZyGames.Framework.Game.Runtime;
using ZyGames.Framework.Game.Service;
using ZyGames.Framework.RPC.IO;
using ZyGames.Framework.RPC.Sockets;
using ZyGames.Framework.Script;

namespace ZyGames.Framework.Game.Contract
{
    public class MainLoop
    {
        private static ConcurrentQueue<RequestPackage> _sendQueue;
        private static Thread _queueProcessThread;
        private static ManualResetEvent singal = new ManualResetEvent(false);
        private static int _runningQueue;
        private static GameSocketHost socketHost;
        public static Action<SocketAsyncResult> OnSendCompleted;
        public delegate void Requested(ActionGetter actionGetter, BaseGameResponse response);
        public static Requested OnRequested;

        static MainLoop()
        {
            _sendQueue = new ConcurrentQueue<RequestPackage>();

            Interlocked.Exchange(ref _runningQueue, 1);
            _queueProcessThread = new Thread(ProcessQueue);
            _queueProcessThread.Start();
        }

        public static void SetSocketHost(GameSocketHost host,
            Action<SocketAsyncResult> onSendCompleted, Requested onRequested)
        {
            socketHost = host;
            OnRequested = onRequested;
            OnSendCompleted = onSendCompleted;
        }


        /// <summary>
        /// Dispose
        /// </summary>
        public static void Dispose()
        {
            Interlocked.Exchange(ref _runningQueue, 0);
            try
            {
                singal.Set();
                singal.Dispose();
                _queueProcessThread.Abort();
            }
            catch
            {
            }
        }

        public static void TryEnqueue(RequestPackage package)
        {
            _sendQueue.Enqueue(package);
            singal.Set();
        }

        private static void ProcessQueue(object state)
        {
            while (_runningQueue == 1)
            {
                singal.WaitOne();
                if (_runningQueue == 1)
                {
                    Thread.Sleep(5);//Delay 5ms
                }
                while (_runningQueue == 1)
                {
                    RequestPackage package;
                    if (_sendQueue.TryDequeue(out package))
                    {
                        GameSession session = GameSession.Get(package.SessionId);
                        ProcessPackage(package, session).Wait();
                    }
                    else
                    {
                        break;
                    }
                }
                singal.Reset();
            }
        }

        private static bool CheckRemote(string route, ActionGetter actionGetter)
        {
            return actionGetter.CheckSign();
        }

        private static void OnCallRemote(string routePath, ActionGetter actionGetter, MessageStructure response)
        {
            try
            {
                string[] mapList = routePath.Split('.');
                string funcName = "";
                string routeName = routePath;
                if (mapList.Length > 1)
                {
                    funcName = mapList[mapList.Length - 1];
                    routeName = string.Join("/", mapList, 0, mapList.Length - 1);
                }
                string routeFile = "";
                int actionId = actionGetter.GetActionId();
                MessageHead head = new MessageHead(actionId);
                if (!ScriptEngines.SettupInfo.DisablePython)
                {
                    routeFile = string.Format("Remote.{0}", routeName);
                    dynamic scope = ScriptEngines.ExecutePython(routeFile);
                    if (scope != null)
                    {
                        var funcHandle = scope.GetVariable<RemoteHandle>(funcName);
                        if (funcHandle != null)
                        {
                            funcHandle(actionGetter, head, response);
                            response.WriteBuffer(head);
                            return;
                        }
                    }
                }
                string typeName = string.Format(GameEnvironment.Setting.RemoteTypeName, routeName);
                routeFile = string.Format("Remote.{0}", routeName);
                var args = new object[] { actionGetter, response };
                var instance = (object)ScriptEngines.Execute(routeFile, typeName, args);
                if (instance is RemoteStruct)
                {
                    var target = instance as RemoteStruct;
                    target.DoRemote();
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteError("OnCallRemote error:{0}", ex);
            }
        }

        protected static void DoAction(ActionGetter actionGetter, BaseGameResponse response)
        {
            if (GameEnvironment.IsRunning && !ScriptEngines.IsCompiling)
            {
                OnRequested(actionGetter, response);
                ActionFactory.Request(actionGetter, response);
            }
            else
            {
                response.WriteError(actionGetter, Language.Instance.MaintainCode, Language.Instance.ServerMaintain);
            }
        }

        private static async System.Threading.Tasks.Task ProcessPackage(RequestPackage package, GameSession session)
        {
            if (package == null) return;

            try
            {
                ActionGetter actionGetter;
                byte[] data = new byte[0];
                if (!string.IsNullOrEmpty(package.RouteName))
                {
                    actionGetter = socketHost.ActionDispatcher.GetActionGetter(package, session);
                    if (CheckRemote(package.RouteName, actionGetter))
                    {
                        MessageStructure response = new MessageStructure();
                        OnCallRemote(package.RouteName, actionGetter, response);
                        data = response.PopBuffer();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    SocketGameResponse response = new SocketGameResponse();
                    response.WriteErrorCallback += socketHost.ActionDispatcher.ResponseError;
                    actionGetter = socketHost.ActionDispatcher.GetActionGetter(package, session);
                    DoAction(actionGetter, response);
                    data = response.ReadByte();
                }
                try
                {
                    if (session != null && data.Length > 0)
                    {
                        await session.SendAsync(actionGetter.OpCode, data, 0, data.Length, OnSendCompleted);
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.WriteError("PostSend error:{0}", ex);
                }

            }
            catch (Exception ex)
            {
                TraceLog.WriteError("Task error:{0}", ex);
            }
            finally
            {
                if (session != null) session.ExitSession();
            }
        }
    }
}
