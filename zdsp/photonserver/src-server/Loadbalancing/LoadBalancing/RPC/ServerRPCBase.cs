//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18408
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Collections.Generic;
using Photon.SocketServer;
using Photon.LoadBalancing.GameServer;
using Zealot.Common.RPC;
using Zealot.Common.Actions;
using Photon.LoadBalancing.MasterServer.GameManager;
using Zealot.Common.Datablock;
using Zealot.Server.EventMessage;
using Zealot.Server.Counters;
using ExitGames.Logging;
using LogManager = ExitGames.Logging.LogManager;
using Photon.Hive;
using Photon.LoadBalancing.ClusterServer.GameServer;
using Photon.LoadBalancing.ServerToServer;
using Photon.LoadBalancing.MasterServer.Cluster;
using Photon.LoadBalancing.MasterServer.GameServer;

namespace Zealot.RPC
{    
    public class ServerRPCBase
    {        
        struct ServerStubInfo
        {
            public readonly byte MethodID;
            public bool UnsuspendRPC;

            public ServerStubInfo(byte id, bool unsuspend = false)
            {
                MethodID = id;
                UnsuspendRPC = unsuspend;
            }
        }

        struct RPCMethodInfo
        {
            public readonly string MethodName;
            public readonly bool SuspendRPC;

            public RPCMethodInfo(string methodname, bool suspend)
            {
                MethodName = methodname;
                SuspendRPC = suspend;
            }
        }

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();        
        System.Text.StringBuilder sbParamStr;        

        byte opcode;
        Dictionary<string, object> schemaregistry;
        Dictionary<string, object> stubregistry;
        Dictionary<string, ServerStubInfo> callerNameToStubInfo;
        Dictionary<byte, RPCMethodInfo> calleeIDToMethodInfo;        
        private RPCCategory mRPCCategory;
        public const byte INIT_PCODE = 0;
        private bool mbReliable = true;
        Type _subclass;
        Type mTmainContext;
        private Dictionary<byte, object> mPackedDic;
        private bool mbUsePackRPC = false;
        private byte mPackIdx = 0;

        public Action<object, bool> SetSuspended;
        public Func<bool> IsSuspended;

        private Profiler mProxyMethodProfiler;

        public ServerRPCBase(Type subclass, byte subopcode, bool reliable, object zrpc = null)
        {
            sbParamStr = new System.Text.StringBuilder();

            mbReliable = reliable;
            _subclass = subclass;
            schemaregistry = new Dictionary<string, object>();
            stubregistry = new Dictionary<string, object>();
            callerNameToStubInfo = new Dictionary<string, ServerStubInfo>();
            calleeIDToMethodInfo = new Dictionary<byte, RPCMethodInfo>();            

            MethodInfo[] methods = subclass.GetMethods();
            foreach (MethodInfo m in methods)
            {
                RPCMethodAttribute att = (RPCMethodAttribute)m.GetCustomAttribute(typeof(RPCMethodAttribute));
                if (att != null) //is RPC caller method
                {
                    byte pcode = INIT_PCODE;
                    Dictionary<byte, object> mdb = new Dictionary<byte, object>();
                    mdb.Add(pcode, m.Name);
                    pcode++;
                    ParameterInfo[] parameterlist = m.GetParameters();
                    foreach (ParameterInfo p in parameterlist)
                    {
                        mdb.Add(pcode, null);
                        pcode++;
                    }
                    schemaregistry.Add(m.Name, mdb);
                    stubregistry.Add(m.Name, m);

                    var stubinfo = new ServerStubInfo(att.MethodID);
                    var unsuspendatt = m.GetCustomAttributes(typeof(RPCUnsuspendAttribute), true);
                    if (unsuspendatt.Length > 0)
                    {
                        stubinfo.UnsuspendRPC = true;
                    }
                    callerNameToStubInfo.Add(m.Name, stubinfo);
                }                
            }
            mPackedDic = new Dictionary<byte, object>();
            opcode = subopcode;

            if (zrpc != null)
            {
                PropertyInfo propinfo = zrpc.GetType().GetProperty("Suspended");
                if (propinfo != null)
                {
                    SetSuspended = (Action<object, bool>)Delegate.CreateDelegate(typeof(Action<object, bool>), zrpc.GetType().GetMethod("SetSuspended"));
                    IsSuspended = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), zrpc, propinfo.GetGetMethod());
                }
            }

            if (IsSuspended == null)
            {
                SetSuspended = (obj, suspend) => { };
                IsSuspended = () => { return false; };
            }

            mProxyMethodProfiler = new Profiler();
        }

        public Dictionary<byte, object> GetMethod(string name)
        {
            return (Dictionary<byte, object>)schemaregistry[name];
        }

        public void SetMainContext(Type t, RPCCategory cat)
        {
            mTmainContext = t;
            mRPCCategory = cat;
            calleeIDToMethodInfo.Clear();

            //Search for callee/receiving methods that are used for RPC
            MethodInfo[] methods = mTmainContext.GetMethods();
            foreach (MethodInfo m in methods)
            {
                RPCMethodAttribute att = (RPCMethodAttribute) m.GetCustomAttribute(typeof(RPCMethodAttribute));
                if (att != null && att.Category == cat) //is RPC callee method
                {
                    var minfo = new RPCMethodInfo(m.Name, att.SuspendRPC);
                    calleeIDToMethodInfo.Add(att.MethodID, minfo);
                }               
            }
        }        

        public void ProxyMethod(string methodname, params object[] args)
        {
            mProxyMethodProfiler.Start();            
            object trg = null;
            Dictionary<byte, object> dic;            
            if (mbUsePackRPC)
            {
                if (methodname == "SendAction")
                {
                    ActionCommand cmd = args[1] as ActionCommand;
                    bool ret = cmd.SerializeStream((int)args[0], ref mPackIdx, ref mPackedDic);
                    if (!ret)
                    {
                        ResetAndSendRPC(args[2], mPackedDic);
                        BeginRPC(null);                        
                        cmd.SerializeStream((int)args[0], ref mPackIdx, ref mPackedDic);    // shouldn't exceed the limit here
                    }
                }
                else if (methodname == "UpdateLocalObject" || methodname == "AddLocalObject")
                {
                    LocalObject cmd = args[2] as LocalObject;
                    bool createnew = (methodname == "AddLocalObject");
                    bool ret = cmd.SerializeStream((byte)args[0], (int)args[1], createnew, ref mPackedDic);
                    if (!ret)
                    {
                        ResetAndSendRPC(args[3], mPackedDic);
                        BeginRPC(null);
                        cmd.SerializeStream((byte)args[0], (int)args[1], createnew, ref mPackedDic);    // shouldn't exceed the limit here
                    }     
                }
                else
                {
                    int sizeRequired = 0;
                    foreach (object param in args) //count actual size required in dictonary
                    {
                        if (param != null && param.GetType() == typeof(RPCPosition))
                            sizeRequired += 3;
                        else
                            sizeRequired++;
                    }
                    
                    if (mPackIdx + sizeRequired > 255)
                    {
                        ResetAndSendRPC(args[args.Length - 1], mPackedDic);
                        BeginRPC(null);
                    }

                    var stubinfo = callerNameToStubInfo[methodname];
                    byte methodid = stubinfo.MethodID;
                    mPackedDic.Add(mPackIdx++, methodid);
                    int i = 0;
                    foreach (object param in args)
                    {
                        if (i == (args.Length - 1))
                        {
                            trg = param;
                            break;
                        }

                        //Handle complicated Types here:                        
                        Type paramType = param == null ? null : param.GetType();
                        if (paramType == typeof(RPCPosition)) 
                        {
                            RPCPosition rpcpos = (RPCPosition)param;
                            mPackedDic.Add(mPackIdx++, rpcpos.X);
                            mPackedDic.Add(mPackIdx++, rpcpos.Y);
                            mPackedDic.Add(mPackIdx++, rpcpos.Z);
                        }
                        else if (paramType == typeof(RPCDirection))
                        {
                            mPackedDic.Add(mPackIdx++, ((RPCDirection)param).YawEncodedPhoton);
                        }
                        else
                            mPackedDic.Add(mPackIdx++, param);
                        i++;
                    }

                    if (stubinfo.UnsuspendRPC)
                        SetSuspended(trg, false);
                }               
            }
            else
            {                
                if (methodname == "SendAction")
                {
                    ActionCommand cmd = args[1] as ActionCommand;
                    dic = new Dictionary<byte, object>(cmd.Serialize((int)args[0]));                    
                    trg = args[2];
                }
                else if (methodname == "UpdateLocalObject" || methodname == "AddLocalObject")
                {
                    LocalObject cmd = args[2] as LocalObject;
                    bool createnew = (methodname == "AddLocalObject");
                    dic = new Dictionary<byte, object>(cmd.Serialize((byte)args[0], (int)args[1], createnew));
                    trg = args[3];
                }
                else
                {
                    //dic = new Dictionary<byte, object>(GetMethod(methodname));
                    dic = new Dictionary<byte, object>();
                    byte pcode = INIT_PCODE;
                    dic.Add(pcode++, 0);        // single command

                    var stubinfo = callerNameToStubInfo[methodname];
                    byte methodid = stubinfo.MethodID;
                    dic.Add(pcode++, methodid);
                    
                    int i = 0;
                    foreach (object param in args)
                    {
                        if (i == (args.Length - 1))
                        {
                            trg = param;
                            break;
                        }

                        //Handle complicated Types here:
                        Type paramType = param == null ? null : param.GetType();
                        if (paramType == typeof(RPCPosition))
                        {
                            RPCPosition rpcpos = (RPCPosition)param;
                            dic.Add(pcode++, rpcpos.X);
                            dic.Add(pcode++, rpcpos.Y);
                            dic.Add(pcode++, rpcpos.Z);
                        }
                        else if (paramType == typeof(RPCDirection))
                        {
                            dic.Add(pcode++, ((RPCDirection)param).YawEncodedPhoton);
                        }
                        else
                            dic.Add(pcode++, param);                       
                        i++;
                    }

                    if (stubinfo.UnsuspendRPC)
                        SetSuspended(trg, false);
                }

                ResetAndSendRPC(trg, dic);
            }
            long elapsed = (long) (mProxyMethodProfiler.StopAndGetElapsed() * 1000000); //msec x 1000 (Scaled up)
            GameCounters.ProxyMethod.IncrementBy(elapsed);
        }        

        public object OnProxyMethodController(Dictionary<byte, object> param, ref byte opcd, object controller, object peer)
        {
            sbParamStr.Clear();
            object retval = null;
            int totalParams = param.Count;
            byte methodid = 0;
            string methodname = string.Empty;
            int pInfosLength = 0;
            byte startOpcd = opcd;
            //try
            {                
                methodid = (byte)param[opcd++];
                var minfo = calleeIDToMethodInfo[methodid];

                methodname = minfo.MethodName;
                sbParamStr.AppendFormat("M:{0}", methodname);

                MethodInfo m = mTmainContext.GetMethod(methodname);
                ParameterInfo[] pInfos = m.GetParameters();
                pInfosLength = pInfos.Length;
                object[] args = new object[pInfos.Length];
                int i = 0;
                
                foreach (ParameterInfo p in pInfos)
                {
                    if (i == (pInfos.Length - 1))
                    {
                        args[i] = peer;
                        break;
                    }

                    //Handle complicated types here:                
                    Type paramType = p.ParameterType;
                    if (paramType == typeof(RPCPosition))
                    {
                        RPCPosition rpcpos = new RPCPosition((short)param[opcd++], (short)param[opcd++], (short)param[opcd++]);
                        args[i] = rpcpos;
                    }
                    else if (paramType == typeof(RPCDirection))
                    {
                        args[i] = new RPCDirection((short)param[opcd++]);
                    }
                    else
                        args[i] = param[opcd++];

                    sbParamStr.AppendFormat(", P:{0}={1}", p.Name, args[i]);
                    i++;
                }

                printRPC(sbParamStr.ToString(), peer);

                if (IsSuspended())
                    return null;

                if (minfo.SuspendRPC)
                    SetSuspended(peer, true);
                
                //Zealot.Server.Counters.Profiler profiler = new Zealot.Server.Counters.Profiler();
                //if (mRPCCategory == RPCCategory.Combat && methodid == 0)                
                //    profiler.Start();
                
                GameLogic gamelogic = controller as GameLogic;
                if (gamelogic == null || !gamelogic.RPCCallee(mRPCCategory, methodid, args))
                    retval = m.Invoke(controller, args);

                //if (mRPCCategory == RPCCategory.Combat && methodid == 0)
                //{
                //    long time = (long) (profiler.StopAndGetElapsed() *1000000); 
                //    log.InfoFormat("Process rpc callee time = {0} microsec", time);
                //}
            }  
            //catch(Exception ex)
            //{
            //    Exception error = ex;
            //    while (error.InnerException != null)
            //        error = error.InnerException;

            //    GameClientPeer temppeer = peer as GameClientPeer;
            //    if (temppeer != null && temppeer.mPlayer != null && temppeer.RoomReference != null && temppeer.RoomReference.Room != null)
            //        log.ErrorFormat("ExceptionType: {0},OnProxyMethodController {1}:{2}:{3} totalParams: {4} methodid: {5} methodname: {6} rpcCategory: {7} pInfosLength: {8} startOpcd: {9} opcd: {10} PlayerName: {11} RoomName: {12}",
            //                     error.GetType(), sbParamStr.ToString(), error.StackTrace, error.Message, totalParams, methodid, methodname, mRPCCategory.ToString(), pInfosLength,
            //                     startOpcd, opcd, temppeer.mPlayer.Name, temppeer.RoomReference.Room.Name);
            //    else
            //    {
            //        log.ErrorFormat("ExceptionType: {0},OnProxyMethodController {1}:{2}:{3} totalParams: {4} methodid: {5} methodname: {6} rpcCategory: {7} pInfosLength: {8} startOpcd: {9} opcd: {10}",
            //                     error.GetType(), sbParamStr.ToString(), error.StackTrace, error.Message, totalParams, methodid, methodname, mRPCCategory.ToString(), pInfosLength,
            //                     startOpcd, opcd);
            //    }
            //}
            
            return retval;
        }

        void printRPC(string s, object peer)
        {
            int conId = -1;
            if (peer is HivePeer)
                conId = (peer as HivePeer).ConnectionId;
            else if (peer is PeerBase)
                conId = (peer as PeerBase).ConnectionId;
            log.InfoFormat("[{0}] {1}", conId, s);
        }

        public object OnProxyMethodControllerList(Dictionary<byte, object> param, object controller, object peer)
        {
            byte opcd = INIT_PCODE + 1;
            while(param.ContainsKey(opcd))
            {
                OnProxyMethodController(param, ref opcd, controller, peer);
            }
            return null;      
        }

        public byte GetOpCode()
        {
            return opcode;
        }

        // to override
        public void OnCommand(GameLogic controller, HivePeer peer, OperationRequest operationRequest, SendParameters sendParameters)        
        {
            GameCounters.RPCReceivedPerSec.Increment();
            byte pcode = INIT_PCODE;
            int cmdMode = (int)operationRequest.Parameters[pcode++];
            if (cmdMode == 0)
            {                    
                object retval = OnProxyMethodController(operationRequest.Parameters, ref pcode, controller, peer);
            }
            else
            {
                OnProxyMethodControllerList(operationRequest.Parameters, controller, peer);                
            }                    
        }

        public void OnCommandServer(object controller, PeerBase peer, OperationRequest operationRequest, SendParameters sendParameters)
        {
            byte pcode = INIT_PCODE;
            int cmdMode = (int)operationRequest.Parameters[pcode++];
            if (cmdMode == 0)
            {                
                object retval = OnProxyMethodController(operationRequest.Parameters, ref pcode, controller, peer);
            }
            else
            {
                OnProxyMethodControllerList(operationRequest.Parameters, controller, peer);
            }
        }

        public void BeginRPC(HivePeer peer)
        {
            mPackIdx = 0;
            mbUsePackRPC = true;
            mPackedDic.Clear();
            mPackedDic.Add(mPackIdx++, 1);      // multi commands
        }

        public void EndRPC(HivePeer peer)
        {
            mPackIdx = 0;
            mbUsePackRPC = false;

            if (mPackedDic.ContainsKey(INIT_PCODE + 1))
                ResetAndSendRPC(peer, mPackedDic);
        }

        private void ResetAndSendRPC(object trg, Dictionary<byte, object> dic)
        {
            GameCounters.RPCSentPerSec.Increment();
            mPackIdx = 0;
            
            var eventData = new EventData(GetOpCode(), dic);
            if (trg is Game)
            {
                Game room = (Game)trg;               
                int total = room.SendEventToAllActors(eventData, mbReliable);
                GameCounters.RPCSentPerSec.IncrementBy(total - 1);
            }
            else if (trg is HivePeer)
            {
                HivePeer peer = (HivePeer)trg;
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                peer.SendEvent(eventData, para);
            }
            else if (trg is IncomingGMPeer)
            {
                IncomingGMPeer peer = (IncomingGMPeer)trg;
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                peer.SendEvent(eventData, para);
            }

            else if (trg is IncomingGameServerPeer)
            {
                IncomingGameServerPeer gs = trg as IncomingGameServerPeer;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                gs.SendOperationRequest(op, para);
            }
            else if (trg is Dictionary<int, IncomingGameServerPeer>)
            {
                Dictionary<int, IncomingGameServerPeer> gslist = trg as Dictionary<int, IncomingGameServerPeer>;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                foreach (var gs in gslist.Values)
                    gs.SendOperationRequest(op, para);
            }
            else if (trg is List<IncomingGameServerPeer>)
            {
                List<IncomingGameServerPeer> gslist = trg as List<IncomingGameServerPeer>;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                for (int index = 0; index < gslist.Count; index++)
                    gslist[index].SendOperationRequest(op, para);
            }

            else if (trg is IncomingGamePeer)
            {
                IncomingGamePeer gs = trg as IncomingGamePeer;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                gs.SendOperationRequest(op, para);
            }
            else if (trg is List<IncomingGamePeer>)
            {
                List<IncomingGamePeer> gslist = trg as List<IncomingGamePeer>;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                for (int index = 0; index < gslist.Count; index++)
                    gslist[index].SendOperationRequest(op, para);
            }
            else if (trg is Dictionary<int, IncomingGamePeer>)
            {
                Dictionary<int, IncomingGamePeer> gslist = trg as Dictionary<int, IncomingGamePeer>;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                foreach (var gs in gslist.Values)
                    gs.SendOperationRequest(op, para);
            }

            else if (trg is IncomingClusterServerPeer)
            {
                IncomingClusterServerPeer gs = trg as IncomingClusterServerPeer;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                gs.SendOperationRequest(op, para);
            }
            else if (trg is List<IncomingClusterServerPeer>)
            {
                List<IncomingClusterServerPeer> gslist = trg as List<IncomingClusterServerPeer>;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                for (int index = 0; index < gslist.Count; index++)
                    gslist[index].SendOperationRequest(op, para);
            }
            else if (trg is Dictionary<string, IncomingClusterServerPeer>)
            {
                Dictionary<string, IncomingClusterServerPeer> gslist = trg as Dictionary<string, IncomingClusterServerPeer>;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                foreach (var gs in gslist.Values)
                    gs.SendOperationRequest(op, para);
            }           

            else if (trg is OutgoingClusterServerPeer)
            {
                OutgoingClusterServerPeer gs = trg as OutgoingClusterServerPeer;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                gs.SendOperationRequest(op, para);
            }
            else if (trg is OutgoingMasterServerPeer)
            {
                OutgoingMasterServerPeer gs = trg as OutgoingMasterServerPeer;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                gs.SendOperationRequest(op, para);
            }
            else if (trg is OutgoingGameToMasterPeer)
            {
                OutgoingGameToMasterPeer gs = trg as OutgoingGameToMasterPeer;
                OperationRequest op = new OperationRequest(GetOpCode(), dic);
                SendParameters para = new SendParameters();
                para.Unreliable = !mbReliable;
                gs.SendOperationRequest(op, para);
            }
        }
     
        public void OnAction(GameLogic controller, HivePeer peer, OperationRequest operationRequest, SendParameters sendParameters)
        {
            GameCounters.RPCReceivedPerSec.Increment();
            byte code = INIT_PCODE;
            int cmdMode = (int)operationRequest.Parameters[code++];
            int persid = (int)operationRequest.Parameters[code++];
            ACTIONTYPE actiontype = (ACTIONTYPE)operationRequest.Parameters[code++];
            Type cmdType = ActionManager.GetActionCommandType(actiontype);
            ActionCommand cmd = ActionManager.CreateNewActionCmd(actiontype);
            try
            {
                cmd.Deserialize(operationRequest.Parameters, ref code);
                controller.OnActionCommand(persid, cmd, peer);
            }
            catch
            {
                log.ErrorFormat("ExceptionType: OnAction {0}", actiontype.ToString());
            }
        }

        protected RPCBroadcastData GetSerializedRPC(byte rpcMethodId, params object[] args)
        {
            if (calleeIDToMethodInfo.ContainsKey(rpcMethodId))
            {
                var dic = new Dictionary<byte, object>();
                byte pcode = INIT_PCODE;
                dic.Add(pcode++, 0);

                dic.Add(pcode++, rpcMethodId);

                int i = 0;
                foreach (object param in args)
                {
                    Type paramType = param == null ? null : param.GetType();
                    if (paramType == typeof(RPCPosition))
                    {
                        RPCPosition rpcpos = (RPCPosition)param;
                        dic.Add(pcode++, rpcpos.X);
                        dic.Add(pcode++, rpcpos.Y);
                        dic.Add(pcode++, rpcpos.Z);
                    }
                    else if (paramType == typeof(RPCDirection))
                    {
                        dic.Add(pcode++, ((RPCDirection)param).YawEncodedPhoton);
                    }
                    else
                        dic.Add(pcode++, param);
                    i++;
                }

                var eventData = new EventData(GetOpCode(), dic);
                return new RPCBroadcastData(eventData, new SendParameters() { Unreliable = mbReliable });
            }
            return null;
        }
    }
}