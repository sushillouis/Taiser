using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class NewNetworkMgr : MonoBehaviourPunCallbacks
{
    public static NewNetworkMgr inst;
    private void Awake()
    {
        inst = this;
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        Connect();   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public string gameVersion = "1.0";
    void Connect()
    {
        if(!PhotonNetwork.IsConnected) {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }

    //MonoBehaviorPunCallbacks
    public override void OnConnectedToMaster()
    {
        Debug.Log("Taiser Lobby: OnConnectedToMaster");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        Debug.LogWarningFormat("Taiser Lobby: On disconnected due to: {0}", cause);
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.LogWarning("Joined a Lobby for this Taiser");
    }

    //-------------------------------------------------------
    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        Debug.Log("Taiser: OnCreatedRoom: " + PhotonNetwork.CurrentRoom.Name + 
                           " Max players: " + PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.LogErrorFormat("CreateRoom Failed: with return code: {0} and message: {1}", returnCode, message);
    }

    public void CreateTaiserRoom(string roomName, int maxPlayersPerRoom)
    {
        Photon.Realtime.RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte) maxPlayersPerRoom; // Photon likes bytes
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    //-------------------------------------------------------

    public void JoinTaiserRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
        Debug.LogWarning(PhotonNetwork.NickName + "did Join(existing)TaiserRoom:  " + roomName);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.LogErrorFormat("On JoinRoomFailed for: {0} with return code: {1} and message: {2]",
            PhotonNetwork.NickName, returnCode, message);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        NewLobbyMgr.inst.UpdateRoom();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        Debug.Log(PhotonNetwork.NickName + " got OnRoomListUpdate called");
    }
}
