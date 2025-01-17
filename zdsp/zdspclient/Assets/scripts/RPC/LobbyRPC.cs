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
//#define clientrpc 0
using ExitGames.Client.Photon;
using Zealot.Common.RPC;

public class LobbyRPC : RPCBase
{	
	public LobbyRPC() : 
		base(typeof(LobbyRPC), OperationCode.Lobby)
	{
	}

    [RPCMethod(RPCCategory.Lobby, (byte)ClientLobbyRPCMethods.GetCharacters)]
    public void GetCharacters(bool newcharacter)
    {
        ProxyMethod("GetCharacters", newcharacter);
    }

    [RPCMethod(RPCCategory.Lobby, (byte)ClientLobbyRPCMethods.InsertCharacter)]
    public void InsertCharacter(byte jobsect, byte style, byte faction, string charname)
    {
        ProxyMethod("InsertCharacter", jobsect, style, faction, charname);
    }

    [RPCMethod(RPCCategory.Lobby, (byte)ClientLobbyRPCMethods.EnterGame)]
    public void EnterGame(string charname)
    {
        ProxyMethod("EnterGame", charname);
    }

    [RPCMethod(RPCCategory.Lobby, (byte)ClientLobbyRPCMethods.CheckCharacterName)]
    public void CheckCharacterName(string charname)
    {
        ProxyMethod("CheckCharacterName", charname);
    }

    [RPCMethod(RPCCategory.Lobby, (byte)ClientLobbyRPCMethods.CreateCharacter)]
    public void CreateCharacter(string charname, byte gender, int hairstyle, int haircolor, int makeup, int skincolor)
    {
        ProxyMethod("CreateCharacter", charname, gender, hairstyle, haircolor, makeup, skincolor);
    }

    [RPCMethod(RPCCategory.Lobby, (byte)ClientLobbyRPCMethods.DeleteCharacter)]
    public void DeleteCharacter(string charname)
    {
        ProxyMethod("DeleteCharacter", charname);
    }

    [RPCMethod(RPCCategory.Lobby, (byte)ClientLobbyRPCMethods.CancelDeleteCharacter)]
    public void CancelDeleteCharacter(string charname)
    {
        ProxyMethod("CancelDeleteCharacter", charname);
    }
}
