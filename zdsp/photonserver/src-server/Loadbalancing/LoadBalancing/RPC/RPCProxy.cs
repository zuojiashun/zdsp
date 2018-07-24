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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace Zealot.RPC
{
    public class RPCProxy : RealProxy
    {
        readonly object target;
        List<string> mDefMethods;

        public RPCProxy(object target)
            : base(target.GetType())
        {
            this.target = target;
            mDefMethods = new List<string>();
            mDefMethods.Add("OnCommand");
            mDefMethods.Add("OnCommandServer");
            mDefMethods.Add("OnAction");
            mDefMethods.Add("SetMainContext");
            mDefMethods.Add("BeginRPC");
            mDefMethods.Add("EndRPC");
            mDefMethods.Add("GetSerializedRPC");
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;

            if (methodCall != null)
            {
                return HandleMethodCall(methodCall); // <- see further
            }

            return null;
        }

        public IMessage HandleMethodCall(IMethodCallMessage methodCall)
        {
            Console.WriteLine("Calling method {0}...", methodCall.MethodName);

            try
            {
                object result = null;
                if (mDefMethods.Contains(methodCall.MethodName))
                    result = methodCall.MethodBase.Invoke(target, methodCall.InArgs);
                else
                    ((ServerRPCBase)target).ProxyMethod(methodCall.MethodName, methodCall.InArgs);
                Console.WriteLine("Calling {0}... OK", methodCall.MethodName);
                return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (TargetInvocationException invocationException)
            {
                var exception = invocationException.InnerException;
                Console.WriteLine("Calling {0}... {1}", methodCall.MethodName, exception.GetType());
                return new ReturnMessage(exception, methodCall);
            }
        }
    }
}