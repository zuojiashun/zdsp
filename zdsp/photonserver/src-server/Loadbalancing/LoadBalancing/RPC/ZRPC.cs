//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18408
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using Photon.LoadBalancing.GameServer;

namespace Zealot.RPC
{ 
    public class ZRPC
    {	
        //client and game
	    public CombatRPC CombatRPC;
        public NonCombatRPC NonCombatRPC;
        public ActionRPC ActionRPC;
        public LobbyRPC LobbyRPC;
        public LocalObjectRPC LocalObjectRPC;
        public UnreliableCombatRPC UnreliableCombatRPC;

        //server to server
        public MasterToGameRPC MasterToGameRPC;
        public GameToMasterRPC GameToMasterRPC;
        public ClusterToGameRPC ClusterToGameRPC;
        public GameToClusterRPC GameToClusterRPC;
        public ClusterToMasterRPC ClusterToMasterRPC;
        public MasterToClusterRPC MasterToClusterRPC;
        public MasterToGMRPC MasterToGMRPC;
        
        public bool Suspended
        {
            get; set;
        }

        public static void SetSuspended(object peer, bool suspend)
        {
            if(peer is GameClientPeer)
            {
                ((GameClientPeer)peer).ZRPC.Suspended = suspend;
            }
        }

        public ZRPC()
	    {
            CombatRPC = new CombatRPC(this);
            NonCombatRPC = new NonCombatRPC(this);     
            ActionRPC = new ActionRPC(this);
            LobbyRPC = new LobbyRPC(this);
            LocalObjectRPC = new LocalObjectRPC();
            UnreliableCombatRPC = new UnreliableCombatRPC(this);

            MasterToGameRPC = new MasterToGameRPC();
            GameToMasterRPC = new GameToMasterRPC();
            ClusterToGameRPC = new ClusterToGameRPC();
            GameToClusterRPC = new GameToClusterRPC();
            ClusterToMasterRPC = new ClusterToMasterRPC();
            MasterToClusterRPC = new MasterToClusterRPC();
            MasterToGMRPC = new MasterToGMRPC();
        }       
    }
}