using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour {
// This class only functions in the editor
#if UNITY_EDITOR

	[SerializeField] Packet[] backgroundPackets;
	[SerializeField] Packet[] packets;
	[SerializeField] Firewall[] firewalls;
	[SerializeField] StartingPoint[] startingPoints;
	[SerializeField] Destination[] destinations;

	[SerializeField] GameObject backgroundPacketPool;
	[SerializeField] GameObject packetPool;

	void OnEnable(){
		startingPoints = StartingPoint.startingPoints;
		destinations = Destination.destinations;

		WhiteHatBaseManager.spawnFirewallEvent += OnFirewallSpawned;
	}
	void OnDisable(){
		WhiteHatBaseManager.spawnFirewallEvent -= OnFirewallSpawned;
	}

	void OnFirewallSpawned(Firewall _){
		firewalls = Firewall.firewalls;
	}

	void Update(){
		backgroundPackets = backgroundPacketPool.GetComponentsInChildren<Packet>();
		packets = packetPool.GetComponentsInChildren<Packet>();
	}

#endif
}
