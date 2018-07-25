﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zealot.Common;
using Zealot.Repository;
using System.Collections;

public enum PiliClientState : byte
{
    Login,
    Lobby,
    Combat,
}

/// <summary>
/// This script automatically connects to Photon (using the settings file), 
/// tries to join a random room and creates one if none was found (which is ok).
/// </summary>
public class Login : Photon.MonoBehaviour
{
    static public List<string> GMMessages = new List<string>();
    public bool IsConnectingToGameServer { get; set; }
    public ServerInfo SelectedServerInfo { get; set; }

    void Awake()
    {
        GameInfo.gClientState = PiliClientState.Login;
        GameInfo.gLogin = this;
        IsConnectingToGameServer = false;
        SelectedServerInfo = null;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>if we don't want to connect in Start(), we have to "remember" if we called ConnectUsingSettings()</summary>
    public virtual void Start()
    {
        if (!GameRepo.IsLoaded)
        {
            GameRepo.InitClient(AssetManager.LoadPiliQGameData());
            EfxSystem.Instance.InitFromGameDB();
        }

        DisplayGMMessage();
    }

    void DisplayGMMessage()
    {
        if (GMMessages != null && GMMessages.Count > 0)
        {
            var message = GMMessages[0];

            UIManager.OpenOkDialog(message, DisplayGMMessage);

            GMMessages.RemoveAt(0);
        }
    }

    public virtual void ConnectToPhotonServer(string user, string token)
    {
        PhotonNetwork.AuthValues = new AuthenticationValues();
        PhotonNetwork.AuthValues.AddAuthParameter(user, token);
        GameVersion gameVersion = GameLoader.Instance.gameVersion;
        string versionNumber = (gameVersion != null) ? gameVersion.ServerWithPatchVersion : "";
        PhotonNetwork.ConnectUsingSettings(versionNumber.Trim());
    }

    public virtual void DefaultAuthWithServer(string user, string token, string extraParam = "", string appID = "", string userId = "")
    {
        // User is login type, token is token/loginId, extraParam is password
        AuthenticationValues authVal = new AuthenticationValues();
        if (string.IsNullOrEmpty(extraParam))
            authVal.AddAuthParameter(user, token);
        else
        {
            // Note: took out EscapeDataString for token, may screw up token
            authVal.AuthGetParameters = string.Format("username={0}&token={1}&extraparam={2}", Uri.EscapeDataString(user),
                                                   Uri.EscapeDataString(token), Uri.EscapeDataString(extraParam));
        }
        GameVersion gameVersion = GameLoader.Instance.gameVersion;
        string versionNumber = (gameVersion != null) ? gameVersion.ServerWithPatchVersion : "";
        PhotonNetwork.networkingPeer.OpAuthenticate(appID, versionNumber.Trim(), authVal, "");
        UIManager.StartHourglass(10.0f, GameInfo.gUILogin.SysOpAuthenticate);
    }

    public virtual void CustomAuthWithServer(byte opCode, string user, string token = "", string deviceId = "")
    {
        Dictionary<byte, object> parameters = new Dictionary<byte, object>();
        parameters.Add(ParameterCode.LoginType, user);
        if (!string.IsNullOrEmpty(token))
            parameters.Add(ParameterCode.LoginId, token);
        if (!string.IsNullOrEmpty(deviceId))
            parameters.Add(ParameterCode.DeviceId, deviceId);

        GameVersion gameVersion = Resources.Load<GameVersion>("GameVersion");
        string versionNumber = (gameVersion != null) ? gameVersion.ServerWithPatchVersion : "";
        parameters.Add(ParameterCode.AppVersion, versionNumber.Trim());
        parameters.Add(ParameterCode.ClientPlatform, GameVersion.GetClientPlatform());
        PhotonNetwork.networkingPeer.OpCustom(opCode, parameters, true, 0, PhotonNetwork.networkingPeer.IsEncryptionAvailable);
        UIManager.StartHourglass(10.0f, GameInfo.gUILogin.SysOpAuthenticate);
    }

    /*public void CustomAuthWithServerUIDShift(LoginType oldType, string oldLoginId, LoginType newType, string newLoginId, 
                                             string deviceId, string pass="", string email="")
    {
        if(string.IsNullOrEmpty(oldLoginId) || string.IsNullOrEmpty(newLoginId))
            return;
        Dictionary<byte, object> param = new Dictionary<byte, object>();
        param.Add(ParameterCode.LoginType, oldType.ToString());
        param.Add(ParameterCode.LoginId, oldLoginId);
        param.Add(ParameterCode.ExtraParam, newType.ToString());
        param.Add(ParameterCode.Username, newLoginId);
        if(!string.IsNullOrEmpty(deviceId))
            param.Add(ParameterCode.DeviceId, deviceId);
        if(!string.IsNullOrEmpty(pass))
            param.Add(ParameterCode.Password, pass);
        if(!string.IsNullOrEmpty(email))
            param.Add(ParameterCode.Email, email);

        bool isEncryptAvail = PhotonNetwork.networkingPeer.IsEncryptionAvailable;
        PhotonNetwork.networkingPeer.OpCustom(OperationCode.UIDShift, param, true, 0, isEncryptAvail);
        UIManager.StartHourglass(10.0f, GameInfo.gUILogin.SysOpAuthenticate);
    }*/

    public virtual bool ReconnectWhenDisconnected(string connectType)
    {
        if (!PhotonNetwork.connected) // If disconnected, try to reconnect
        {
            ConnectToPhotonServer(LoginType.EstablishConnection.ToString(), connectType);
            return true;
        }
        return false;
    }

    // To react to events "connected" and (expected) error "failed to join random room", we implement some methods. 
    // PhotonNetworkingMessage lists all available methods!
    public virtual void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN.");
        Debug.Log("Connected To Master, Populate server list, connect to game server");

        UI_Login uiLogin = GameInfo.gUILogin;
        if (SelectedServerInfo == null) // Game Server not selected yet
        {
            uiLogin.OnClickServerSelection();
            IsConnectingToGameServer = true;
        }
        else // Connect to game server after is cookie auth
        {
            ConnectToSelectedGameServerSetup();
        }
    }

    public void ConnectToSelectedGameServerSetup()
    {
        if (SelectedServerInfo == null)
            return;
        if (SelectedServerInfo.serverLoad == ServerLoad.Full)
        {
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("sys_ServerFull"));
            return;
        }

        int serverid = SelectedServerInfo.id;
        Debug.LogFormat("Tell master I want to connect to GameServer with id: {0}", serverid);
        Dictionary<byte, object> parameters = new Dictionary<byte, object>();
        parameters.Add(ParameterCode.ServerID, serverid);
        PhotonNetwork.networkingPeer.OpCustom(OperationCode.ConnectGameSetup, parameters, true, 0, PhotonNetwork.networkingPeer.IsEncryptionAvailable);
        UIManager.StartHourglass(10.0f, GameInfo.gUILogin.SysConnectingGameServer);
    }

    public void OnConnectGameSetup(short errorcode)
    {
        UIManager.StopHourglass();
        if (SelectedServerInfo == null)
            return;
        if (errorcode == ErrorCode.ServerOffline)
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("sys_ServerOffline"));
        else if (errorcode == ErrorCode.ServerFull)
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("sys_ServerFull"));
        else if (errorcode == ErrorCode.DuplicateLogin)
            UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("sys_DuplicateLogin"));
        else if (errorcode == ErrorCode.Ok)
            StartCoroutine(ConnectToGameServer(SelectedServerInfo.ipAddr));
    }

    private IEnumerator ConnectToGameServer(string ipAddress)
    {
        UIManager.StartHourglass(10.0f, GameInfo.gUILogin.SysConnectingGameServer);
        yield return new WaitForSeconds(2.0f);
        Debug.LogFormat("Connecting to Gameserver: {0}", ipAddress);
        PhotonNetwork.ConnectGameServer(ipAddress);
    }

    public virtual void OnConnectedToGameServer()
    {
        IsConnectingToGameServer = false;
        PhotonNetwork.AuthenticateCookie(LoginData.Instance.userId.ToString(), LoginData.Instance.cookieId.ToString(), SelectedServerInfo.id);
        UIManager.StartHourglass(10.0f, GUILocalizationRepo.GetLocalizedSysMsgByName("sys_Login_ConnectingGameServer", null));
    }

    // The following methods are implemented to give you some context. re-implement them as needed.
    public virtual void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        UIManager.StopHourglass();
        Debug.LogFormat("Cause: {0}", cause);
        UIManager.ShowSystemMessage(GUILocalizationRepo.GetLocalizedSysMsgByName("sys_FailedToConnectToPhoton", null));
    }

    public virtual void OnConnectionFail(DisconnectCause cause)
    {
        if (cause == DisconnectCause.DisconnectByClientTimeout || cause == DisconnectCause.DisconnectByServerTimeout)
            GameInfo.OnDisconnect();
    }

    public void OnDisconnectedFromPhoton()
    {
        if (GameInfo.DCReconnectingGameServer)
        {
            StartCoroutine(ReconnectToGameServer());
            return;
        }

        GameInfo.DCReconnectingGameServer = false;
        GameInfo.TransferingServer = false;
        UIManager.ShowLoadingScreen(false);
        UIManager.StopHourglass();
        if (SceneManager.GetActiveScene().name.Equals("UI_LoginHierarchy"))
            return;

        GameInfo.gClientState = PiliClientState.Login;
        GameInfo.OnQuitGame();
        DestroyAllOnDC();
        if (!PhotonHandler.AppQuits)
            PhotonNetwork.LoadLevel("UI_LoginHierarchy");
        Debug.Log("You are disconnected. Please login.");
    }

    private IEnumerator ReconnectToGameServer()
    {
        if (!GameInfo.TransferingServer)
        {
            int reconnectInSeconds = 5;
            while (reconnectInSeconds > 0)
            {
                UIManager.StartHourglass(reconnectInSeconds, string.Format("Reconnecting in {0} seconds", reconnectInSeconds));
                yield return new WaitForSeconds(1.0f);
                reconnectInSeconds--;
            }
        }
        PhotonNetwork.networkingPeer.ReconnectToGameServer();
    }

    void DestroyAllOnDC()
    {
        UIManager.DestroyLoadingScreen();
        Destroy(this.gameObject);
    }

    public bool GetLoginData()
    {
        if (LoginData.Instance.DeserializeLoginData())
            if (!string.IsNullOrEmpty(LoginData.Instance.DeviceId) && !string.IsNullOrEmpty(LoginData.Instance.LoginId))
                return true;

        // Get device ID, also send to server
        LoginData.Instance.DeviceId = (Application.platform != RuntimePlatform.WindowsEditor)
            ? SystemInfo.deviceUniqueIdentifier
            : string.Format("{0}E:{1}", SystemInfo.deviceUniqueIdentifier, Environment.MachineName);

        return false;
    }

    public void OnLogin(LoginType loginType, string loginId, string password)
    {
        string loginTypeStr = loginType.ToString();
        if (ReconnectWhenDisconnected(loginTypeStr))
        {
            UIManager.StartHourglass(10.0f, GameInfo.gUILogin.SysOpAuthenticate);
            return;
        }

        if (string.IsNullOrEmpty(loginId) || string.IsNullOrEmpty(password))
            return;

        DefaultAuthWithServer(loginTypeStr, loginId, password); // Auth with server    
    }

    public void UserFreezed()
    {
        UIManager.StopHourglass();
        UIManager.OpenOkDialog(GameInfo.gUILogin.RetUserFreezed, null);
    }

    #region Photon MonoMessages

    public void OnAuthenticatedCookie(short returnCode, string appId)
    {
        UIManager.StopHourglass();

        switch (returnCode)
        {
            case ErrorCode.Ok:
                Debug.Log("Authenticate cookie success!");
                if (string.IsNullOrEmpty(appId))
                {
                    ServerInfo serverInfo = SelectedServerInfo;
                    if (serverInfo != null && serverInfo.id != LoginData.Instance.ServerId)
                    {
                        LoginData.Instance.ServerId = serverInfo.id;
                        LoginData.Instance.SerializeLoginData();
                    }

                    if (GameInfo.gClientState == PiliClientState.Login)
                        UIManager.StartHourglass(10.0f, GUILocalizationRepo.GetLocalizedSysMsgByName("sys_Login_LoadingLobby", null));
                    if (GameInfo.gLobby != null)
                        GameInfo.gLobby.JoinLobby();
                }
                break;
            case ErrorCode.InvalidCookie:
                GameInfo.DCReconnectingGameServer = false; //not allow reconnecting anymore.
                PhotonNetwork.networkingPeer.Disconnect();
                break;
            case ErrorCode.UserBlocked:
                UIManager.OpenOkDialog(GameInfo.gUILogin.RetUserFreezed, null);
                PhotonNetwork.networkingPeer.Disconnect();
                break;
            case ErrorCode.GameFull:
                UIManager.OpenOkDialog(GameInfo.gUILogin.RetGameServerFullStr, null);
                PhotonNetwork.networkingPeer.Disconnect();
                break;
        }
    }

    public void OnEstablishedConnection(string connectTypeStr, string serversInfoStr)
    {
        UIManager.StopHourglass();
        UI_Login uiLogin = GameInfo.gUILogin;
        uiLogin.ParseServersInfoStr(serversInfoStr);
        // Generate data file to store device ID
        if (!string.IsNullOrEmpty(connectTypeStr))
        {
            if (Enum.IsDefined(typeof(LoginType), connectTypeStr))
            {
                LoginType loginType = (LoginType)Enum.Parse(typeof(LoginType), connectTypeStr);
                string loginId = "";
                switch (loginType)
                {
                    case LoginType.Device:
                        loginId = LoginData.Instance.LoginId = LoginData.Instance.DeviceId;
                        GameInfo.gLogin.OnLogin(loginType, loginId, loginId);
                        break;
                    case LoginType.Username:
                        GameObject gameObj = UIManager.GetWindowGameObject(WindowType.DialogUsernamePassword);
                        string password = "";
                        if (gameObj.GetComponent<Dialog_UsernamePassword>().TryGetInputfieldSignIn(out loginId, out password))
                            GameInfo.gLogin.OnLogin(loginType, loginId, password);
                        else if (uiLogin.TryGetLoginDataPass(out password))
                            GameInfo.gLogin.OnLogin(loginType, LoginData.Instance.LoginId, password);
                        else
                        {
                            UIManager.OpenDialog(WindowType.DialogUsernamePassword, (window) => {
                                window.GetComponent<Dialog_UsernamePassword>().OnClickOpenUsernameSignIn();
                            });
                        }
                        break;
                    case LoginType.Facebook:
                        GetComponent<FBLogin>().OnFBLoggedIn();
                        break;
                    case LoginType.Google:
                        GetComponent<GoogleLogin>().OnGoogleLoggedIn();
                        break;
                }
            }
            else if (connectTypeStr.Equals("ServerList"))
            {
                uiLogin.OpenDialogServerSelection(false);
            }
            else if (connectTypeStr.Equals("Register"))
            {
                GameObject gameObj = UIManager.GetWindowGameObject(WindowType.DialogUsernamePassword);
                gameObj.GetComponent<Dialog_UsernamePassword>().OnClickUsernameSignUp();
            }
            else if (connectTypeStr.Equals("VerifyLoginIdRegister"))
            {
                GameObject gameObj = UIManager.GetWindowGameObject(WindowType.DialogUsernamePassword);
                gameObj.GetComponent<Dialog_UsernamePassword>().OnClickUsernameVerify();
            }
            /*else if(connectTypeStr.Equals("VerifyLoginIdUIDShift"))
                uiLogin.OnSetUIDShiftVerifyButton();
            else if(connectTypeStr.Equals("PasswordModify"))
                uiLogin.OnPasswordModifyOKButton();
            else if(connectTypeStr.StartsWith("UIDShift_"))
            {
                string[] bindTypeSplit = connectTypeStr.Split('_');
                string bindTypeStr = bindTypeSplit[1];
                if(Enum.IsDefined(typeof(LoginType), bindTypeStr)) // Is a valid type
                {
                    LoginType loginType = (LoginType)Enum.Parse(typeof(LoginType), bindTypeStr);
                    switch(loginType)
                    {
                        case LoginType.Username:
                            uiLogin.OnUIDCheckDataOKButton();
                            break;
                        case LoginType.Facebook:
                            GetComponent<FBLogin>().OnFBUIDShift();
                            break;
                        case LoginType.Google:
                            GetComponent<GoogleLogin>().OnClickedGoogleUIDShift();
                            break;
                    }
                }
            }*/
            else // Generate new client data
                LoginData.Instance.SerializeLoginData();
        }
    }

    public void OnGetServerList(string serversInfoStr)
    {
        UIManager.StopHourglass();

        UI_Login uiLogin = GameInfo.gUILogin;
        uiLogin.ParseServersInfoStr(serversInfoStr);
        uiLogin.OpenDialogServerSelection(false);
    }

    public void OnRegisterResult(bool registerSuccess, string loginId)
    {
        UIManager.StopHourglass();
        UI_Login uiLogin = GameInfo.gUILogin;
        if (registerSuccess) // Proceed to login is success
        {
            //GameObject gameObj = UIManager.GetWindowGameObject(WindowType.DialogUsernamePassword);
            //Dialog_UsernamePassword dialogUserPass = gameObj.GetComponent<Dialog_UsernamePassword>();
            //dialogUserPass.InitInputfieldSignIn(loginId, uiLogin.CachedPass);
            //dialogUserPass.OnClickOpenUsernameSignIn();
            //OnLogin(LoginType.Username, loginId, uiLogin.CachedPass);
            UIManager.CloseDialog(WindowType.DialogUsernamePassword);
            UIManager.OpenOkDialog(GameInfo.gUILogin.RetSignUpSuccessStr, null);

            LoginData.Instance.LoginType = (short)LoginType.Username; // Set to username login type
            LoginData.Instance.LoginId = loginId; // Set to new login ID
            uiLogin.SetAccountName(loginId);
            uiLogin.SetLoginDataPass(LoginType.Username, uiLogin.CachedPass);
        }
        else
            UIManager.OpenOkDialog(uiLogin.RetUserAlreadyExistStr, null);

        uiLogin.CachedPass = "";
    }

    public void OnLoginSuccess(string loginTypeStr, string loginId, string cookie, string userid, string serversInfoStr, string password)
    {
        UIManager.StopHourglass();
        UI_Login uiLogin = GameInfo.gUILogin;
        uiLogin.ParseServersInfoStr(serversInfoStr);
        Debug.LogFormat("{0} Login success...Login ID: {1}", loginTypeStr, loginId);
        if (Enum.IsDefined(typeof(LoginType), loginTypeStr))
        {
            short loginType = (short)Enum.Parse(typeof(LoginType), loginTypeStr);
            LoginData.Instance.LoginType = loginType; // Set to new login type
            LoginData.Instance.LoginId = loginId;     // Set to new login ID
            uiLogin.SetAccountName(loginId);
            uiLogin.SetLoginDataPass((LoginType)loginType, password);
        }
        LoginData.Instance.SerializeLoginData();
        LoginData.Instance.cookieId = new Guid(cookie);
        Debug.LogFormat("Cookie Id recieved: {0}", LoginData.Instance.cookieId);
        LoginData.Instance.userId = new Guid(userid);
    }

    public void OnLoginFailed(bool isUserNotExist)
    {
        UIManager.StopHourglass();
        GameInfo.gUILogin.OpenOkDialogLoginFailed(isUserNotExist);
    }

    public void InvalidClientVer(bool val)
    {
        UIManager.StopHourglass();
        UIManager.OpenOkDialog(GameInfo.gUILogin.SysInvalidClientVer, null);
    }

    public void OnVerifiedLoginId(bool isLoginExist, string loginId, string requestType)
    {
        UIManager.StopHourglass();
        GameObject gameObj = UIManager.GetWindowGameObject(WindowType.DialogUsernamePassword);
        gameObj.GetComponent<Dialog_UsernamePassword>().SetVerifyCheck(!isLoginExist);
    }

    //public void OnPasswordModifyResult(short errorCode, string password)
    //{
    //    UIManager.StopHourglass();
    //    GameInfo.gUILogin.ShowPasswordModifyResultDialog(errorCode, password);
    //}

    //public void OnUIDShiftResult(short errorCode, LoginType loginType, string loginId, string pass)
    //{
    //    UIManager.StopHourglass();
    //    GameInfo.gUILogin.ShowUIDShiftResultDialog(errorCode, loginType, loginId, pass);
    //}

    #endregion

    public static string EncryptTxt(string Data, string Key, byte[] IV, int iterations = 1000)
    {
        try
        {
            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(Key, IV, iterations); // Regenerate a key
            byte[] key = rfc2898DeriveBytes.GetBytes(8);
            // Create a CryptoStream using the MemoryStream and the passed key and initialization vector (IV).
            MemoryStream mStream = new MemoryStream(); // Create a MemoryStream.
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            CryptoStream cStream = new CryptoStream(mStream, des.CreateEncryptor(key, IV), CryptoStreamMode.Write);
            byte[] toEncrypt = new ASCIIEncoding().GetBytes(Data); // Convert the passed string to a byte array.

            // Write the byte array to the crypto stream and flush it.
            cStream.Write(toEncrypt, 0, toEncrypt.Length);
            cStream.FlushFinalBlock();
            // Get an array of bytes from the MemoryStream that holds the encrypted data.
            byte[] ret = mStream.ToArray();
            cStream.Close();
            mStream.Close();
            return Convert.ToBase64String(ret); // Return the encrypted buffer.
        }
        catch (CryptographicException e)
        {
            Debug.LogErrorFormat("A Cryptographic error occurred: {0}", e.Message);
            return null;
        }
    }

    public static string DecryptTxt(string Data, string Key, byte[] IV, int iterations = 1000)
    {
        try
        {
            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(Key, IV, iterations); // Regenerate a key
            byte[] key = rfc2898DeriveBytes.GetBytes(8);
            // Create a CryptoStream using the MemoryStream and the passed key and initialization vector (IV).
            byte[] data = Convert.FromBase64String(Data);
            MemoryStream msDecrypt = new MemoryStream(data);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            CryptoStream csDecrypt = new CryptoStream(msDecrypt, des.CreateDecryptor(key, IV), CryptoStreamMode.Read);
            //byte[] fromEncrypt = new byte[data.Length]; // Create buffer to hold the decrypted data.
            // Read the decrypted data out of the crypto stream and place it into the temporary buffer.
            //csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);
            var streamReader = new StreamReader(csDecrypt);
            // Convert the buffer into a string and return it. (Encoding.UTF8.GetBytes(fromEncrypt))
            return streamReader.ReadToEnd();
        }
        catch (CryptographicException e)
        {
            Debug.LogErrorFormat("A Cryptographic error occurred: {0}", e.Message);
            return null;
        }
    }
}