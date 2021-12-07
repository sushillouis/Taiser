using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerminalNode : StartingPoint {
	public static TerminalNode[] terminals = null;

	new protected void Awake() {
		terminals = FindObjectsOfType<TerminalNode>();
		
		// TODO: Is this nessicary if StartingPoint's awake is hidden?
		try{
			// Remove all terminal nodes from the starting point list
			List<StartingPoint> startingPoints = new List<StartingPoint>(StartingPoint.startingPoints);
			startingPoints.RemoveAll(i => i is TerminalNode); 
			StartingPoint.startingPoints = startingPoints.ToArray();
		} catch (System.ArgumentNullException) {} // Do nothing on a null reference
		
	}

	// Once the game gets started, start up the co-routine to periodically spawn packets
	protected void Start(){ spawnPacketsCoroutine = StartCoroutine(SpawnPackets()); }

	// The object to parent packets to
	public GameObject backgroundPacketPool;
	// The Packet Prefab to spawn
	public GameObject networklessPacketPrefab;
	// The number of seconds to wait before spawning another packet
	public float secondsBetweenPackets = 1;


	// Function which spawns packets every few moments
	Coroutine spawnPacketsCoroutine; // Variable tracking this node's running coroutine (can be used to stop the coroutine)
	IEnumerator SpawnPackets(){

		// For each packet we should spawn...
		while(true){
			// Create a new packet parented to the background pool
			Packet spawned = Instantiate(networklessPacketPrefab, new Vector3(0, 100, 0), Quaternion.identity, backgroundPacketPool.transform).GetComponent<Packet>();

			// Pick unique (if possible) start and end points for the packet to travel between
			TerminalNode start = terminals[Random.Range(0, terminals.Length)];
			TerminalNode end = terminals[Random.Range(0, terminals.Length)];
			while(start == end && terminals.Length > 2) end = terminals[Random.Range(0, terminals.Length)];

			// Pick a random starting point and destination for it (network synced)
			spawned.setStartDestinationAndPath(start as StartingPoint, end);
			spawned.transform.rotation = spawned.startPoint.transform.rotation; // Ensure that the packets have the same orientation as their spawners

			// Setup the packet's appearance and make sure the packets are never malicious (network synced)
			spawned.initPacketDetails(false);

			// Wait for the configured time between packets
			yield return new WaitForSeconds(secondsBetweenPackets);
		}
	}


}
