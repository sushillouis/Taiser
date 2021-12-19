using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class StartingPoint : PathNodeBase, SelectionManager.ISelectable {
	// List of starting points
	public static StartingPoint[] startingPoints = null;
	// Generator for IDS
	static uint nextID = 0;

	// Cache of the attached photon view
	private PhotonView pvCache;
	public PhotonView photonView
	{
		get
		{
			#if UNITY_EDITOR
			// In the editor we want to avoid caching this at design time, so changes in PV structure appear immediately.
			if (!Application.isPlaying || this.pvCache == null)
			{
				this.pvCache = PhotonView.Get(this);
			}
			#else
			if (this.pvCache == null)
			{
				this.pvCache = PhotonView.Get(this);
			}
			#endif
			return this.pvCache;
		}
	}

	// Whenever a new starting point is added update the list of starting points, and set its ID
	protected void Awake() { startingPoints = FindObjectsOfType<StartingPoint>(); SetID(); }


	// All of the likelihoods are added together, then the probability of spawning a packet at this point is <likelihood>/<totalLikelihood>
	public int packetSourceLikelihood = 0;

	// Variable used to uniquely identify a starting point
	[SerializeField] uint _ID;
	public uint ID {
		get => _ID;
		protected set => _ID = value;
	}


	// The malicious packet for this starting point (NetworkSynced)
	public PacketRule spawnedMaliciousPacketRules = System.ObjectExtensions.Copy(PacketRule.Default);
	// The likelihood that a packet coming from this starting point will be malicious (Network Synced)
	public float maliciousPacketProbability = .33333f;

	// Generates a random set of details, ensuring that the returned values aren't considered malicious
	public PacketRule.Details randomNonMaliciousPacketDetails() {
		PacketRule.Details details = new PacketRule.Details(Utilities.randomEnum<PacketRule.Color>(), Utilities.randomEnum<PacketRule.Size>(), Utilities.randomEnum<PacketRule.Shape>());
		// Keep generating new details until we get one which doesn't have an invalid property and isn't on the malicious list
		while(details.color == PacketRule.Color.Any || details.size == PacketRule.Size.Any || details.size == PacketRule.Size.Invalid || details.shape == PacketRule.Shape.Any || spawnedMaliciousPacketRules.Contains(details))
			details = randomNonMaliciousPacketDetails();
		return details;
	}


	// Function which synchronizes a destination's ID over the network
	void SetID(){ if(NetworkingManager.isHost) photonView.RPC("RPC_StartingPoint_SetID", RpcTarget.AllBuffered, (int) nextID++); }
	[PunRPC] void RPC_StartingPoint_SetID(int id){
		ID = (uint) id;
	}

	// Function which gets a destination from its ID
	public static StartingPoint GetFromID(uint id){
		foreach(StartingPoint s in startingPoints)
			if(s.ID == id)
				return s;
		return null;
	}


	// Update the starting point's malicious packet details (Network Synced)
	// Returns true if we successfully updated, returns false otherwise
	public bool SetMaliciousPacketRules(PacketRule rule) {
		photonView.RPC("RPC_StartingPoint_SetMaliciousPacketRules", RpcTarget.AllBuffered, rule.CompressedRuleString() );
		return true;
	}
	[PunRPC] void RPC_StartingPoint_SetMaliciousPacketRules(string rules){
		spawnedMaliciousPacketRules = PacketRule.Parse(rules);

		// Clear out each firewall's correct rule (we will need to rebuild it as needed)
		foreach(Firewall f in Firewall.firewalls)
			f.correctRule.Clear();
	}


	// Function which updates the probability of a spawned packet being malicious (Network Synced)
	// Returns true if we successfully updated, returns false otherwise
	public bool SetMaliciousPacketProbability(float probability) {
		photonView.RPC("RPC_StartingPoint_SetMaliciousPacketProbability", RpcTarget.AllBuffered, probability);
		return true;
	}
	[PunRPC] void RPC_StartingPoint_SetMaliciousPacketProbability(float probability){
		maliciousPacketProbability = probability;
	}

	// Function which returns a weighted list of starting points,
	public static StartingPoint[] getWeightedList(){
		List<StartingPoint> ret = new List<StartingPoint>();
		// For each starting point, add it to the list a number of times equal to its <packetSourceLikelihood>
		foreach(StartingPoint p in startingPoints)
			for(int i = 0; i < p.packetSourceLikelihood; i++)
				ret.Add(p);

		return ret.ToArray();
	}
}
