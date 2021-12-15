using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TestBench : MonoBehaviour {
	void Awake(){
		if(!PhotonNetwork.IsConnected){
			PhotonNetwork.OfflineMode = true;
			PhotonNetwork.JoinRandomRoom();
		}
	}

	void Start() {
		// Don't run this function if there are already players
		if(NetworkingManager.instance.debuggingPlayers.Count != 0) return;

		// Create a new list to hold the room's players
		NetworkingManager.instance.debuggingPlayers = NetworkingManager.players = new List<Networking.Player>();
		// Create a Networking.Player representing ourselves
		Networking.Player me = new Networking.Player();
		me.photonPlayer = PhotonNetwork.LocalPlayer;
		me.debugPhotonPlayer = me.photonPlayer.ActorNumber; // TODO: Remove
		// Add it to the list
		NetworkingManager.players.Add(me);
		Networking.Player.localPlayer = me;

		// me.role = Networking.Player.Role.Advisor;
		// me.side = Networking.Player.Side.BlackHat;

		// (WhiteHatPlayerManager.instance as WhiteHatPlayerManager).OnEnable();
	}

}
