using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BackgroundPacketEntityPoolManager : Core.Utilities.Singleton<BackgroundPacketEntityPoolManager> {

	// Path to the packet prefab we should spawn
	public GameObject backgroundPacketPrefabPath;

	// The number of packets that should be spawned
	public int packetCount = 500;


	// Spawn all of the background packets
	protected void Start(){	
		for(int i = 0; i < packetCount; i++){
			Packet spawned = Instantiate(backgroundPacketPrefabPath, Vector3.zero, Quaternion.identity, transform).GetComponent<Packet>();
			spawned.reinitBackgroundPacketDetails();	
		}
	}
}
