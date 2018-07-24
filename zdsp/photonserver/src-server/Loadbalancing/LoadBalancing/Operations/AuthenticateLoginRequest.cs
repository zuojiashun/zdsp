﻿// --------------------------------------------------------------------------------------------------------------------
// chensheng 9/4/2018
// --------------------------------------------------------------------------------------------------------------------

namespace Photon.LoadBalancing.Operations
{
    #region using directives
    using Photon.SocketServer;
    using Photon.SocketServer.Rpc;
    #endregion

    public class AuthenticateLoginRequest : Operation
    {
        #region Constructors and Destructors

        public AuthenticateLoginRequest(IRpcProtocol protocol, OperationRequest operationRequest)
            : base(protocol, operationRequest)
        {
        }

        #endregion
        #region Serialized Properties
        [DataMember(Code = (byte)ParameterCode.AppVersion, IsOptional = false)]
        public string AppVersion { get; set; }

        [DataMember(Code = (byte)ParameterCode.ApplicationId, IsOptional = false)]
        public string ApplicationId { get; set; }

        [DataMember(Code = (byte)ParameterCode.ClientPlatform, IsOptional = false)]
        public byte ClientPlatform { get; set; }

        [DataMember(Code = (byte)ParameterCode.ClientAuthenticationParams, IsOptional = false)]
        public string ClientAuthenticationParams { get; set; }
        #endregion
    }
}