//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18408
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#define PROFILE_RPC
using System;
//#define clientrpc 0
using ExitGames.Client.Photon;
using UnityEngine;


using Zealot.Common;
using Zealot.Common.Entities;
using Zealot.Client;
using Zealot.Common.Actions;
using System.Reflection;
using System.Collections.Generic;

public class ActionRPC : RPCBase
{
	public ActionRPC() : 
		base(typeof(ActionRPC), OperationCode.Action)
	{
	}

	public void SendAction(Dictionary<byte, object> actiondic)
	{
        ProxyMethod("SendAction", actiondic);
	}


	public void OnAction(object obj, EventData eventdata)
	{
        #if PROFILE_RPC
        bytesReceivedThisFrame += ComputeDataSize(eventdata.Parameters);
        #endif
        byte code = INIT_PCODE;
        int cmdMode = (int)eventdata.Parameters[code++];
        if (cmdMode == 0)
        {
            int persid = (int)eventdata.Parameters[code++];
            ACTIONTYPE actiontype = (ACTIONTYPE)eventdata.Parameters[code++];
            Type cmdType = ActionManager.GetActionCommandType(actiontype);
            ActionCommand cmd = (ActionCommand) Activator.CreateInstance(cmdType);
            cmd.Deserialize(eventdata.Parameters, ref code);

            Type action = ActionManager.GetAction(actiontype);

            Type t = typeof(ClientMain);
            MethodInfo m = (MethodInfo)t.GetMethod("OnActionCommand");
            if (m == null)
            {
                Console.WriteLine("OnCommand failed: method : [OnActionCommand] not found");
                return;
            }
            ParameterInfo[] pInfos = m.GetParameters();
            object[] args = new object[pInfos.Length];
            args[0] = persid;
            args[1] = cmd;
            args[2] = action;

            m.Invoke(obj, args);
        }
        else
        {
            while (eventdata.Parameters.ContainsKey(code))
            {
                int persid = (int)eventdata.Parameters[code++];
                ACTIONTYPE actiontype = (ACTIONTYPE)eventdata.Parameters[code++];
                Type cmdType = ActionManager.GetActionCommandType(actiontype);                
                ActionCommand cmd = (ActionCommand)Activator.CreateInstance(cmdType);
                cmd.Deserialize(eventdata.Parameters, ref code);

                Type action = ActionManager.GetAction(actiontype);

                Type t = typeof(ClientMain);
                MethodInfo m = (MethodInfo)t.GetMethod("OnActionCommand");
                if (m == null)
                {
                    Console.WriteLine("OnCommand failed: method : [OnActionCommand] not found");
                    return;
                }
                ParameterInfo[] pInfos = m.GetParameters();
                object[] args = new object[pInfos.Length];
                args[0] = persid;
                args[1] = cmd;
                args[2] = action;

                m.Invoke(obj, args);
            }
        }
	}
}
