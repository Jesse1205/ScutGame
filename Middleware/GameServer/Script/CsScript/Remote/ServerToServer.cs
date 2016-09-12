using System.Threading;
using ZyGames.Framework.Game.Contract;
using ZyGames.Framework.RPC.IO;

namespace Game.Remote
{
    class ServerToServer
    {
        private readonly static object syncObject;
        private readonly static RemoteService tcpRemote;
        static ServerToServer()
        {
            tcpRemote = RemoteService.CreateTcpProxy("0", "192.168.0.100", 9001, 30 * 1000);
            syncObject = new object();
        }

        public static int Action1000(int userId)
        {
            var result = 0;
            var param = new RequestParam();
            param.Add("UserId", userId);
            AutoResetEvent waitHandle = new AutoResetEvent(false);
            lock (syncObject)
            {
                tcpRemote.Call("Battle1000", param, package =>
                  {
                      var reader = new MessageStructure(package.Message as byte[]);
                      result = reader.ReadInt();
                      waitHandle.Set();
                  });
            }
            waitHandle.WaitOne(5000);
            return result;
        }
    }
}
