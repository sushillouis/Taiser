using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class StartingPoint : PathNodeBase, SelectionManager.ISelectable {
	public static StartingPoint[] startingPoints = null;

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

	// Whenever a new starting point is added update the list of starting points
	protected void Awake() { startingPoints = FindObjectsOfType<StartingPoint>(); }

	// The number of updates gained after each wave (based on difficulty)
	public int[] updatesGrantedPerWave = new int[3] {/*easy*/5, /*medium*/5, /*hard*/5};
	// Property representing the number of updates currently available
	public int updatesRemaining = 1; // Starts at 1 to account for initial settings

	// All of the likelihoods are added together, then the probability of spawning a packet at this point is <likelihood>/<totalLikelihood>
	public int packetSourceLikelihood = 0;


	// De/register the start function on wave ends
	void OnEnable(){ GameManager.waveEndEvent += Start; }
	void OnDisable(){ GameManager.waveEndEvent -= Start; }

	// When the this is created or a wave starts grant its updates per wave
	void Start(){
		updatesRemaining += updatesGrantedPerWave[(int)GameManager.difficulty];
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


	// Update the starting point's malicious packet details (Network Synced)
	// Returns true if we successfully updated, returns false otherwise
	public bool SetMaliciousPacketRules(PacketRule rule) {
		// Only update the settings if we have updates remaining
		if(updatesRemaining > 0){
			// Take away an update if something actually changed
			if(rule != spawnedMaliciousPacketRules)
				updatesRemaining--;
			photonView.RPC("RPC_StartingPoint_SetMaliciousPacketRules", RpcTarget.AllBuffered, rule.CompressedRuleString() );
			return true;
		} else return false;
	}
	[PunRPC] void RPC_StartingPoint_SetMaliciousPacketRules(string rules){
		spawnedMaliciousPacketRules = PacketRule.Parse(rules);

		Debug.Log(spawnedMaliciousPacketRules.treeRoot.RuleString());
	}


	// Function which updates the probability of a spawned packet being malicious (Network Synced)
	// Returns true if we successfully updated, returns false otherwise
	public bool SetMaliciousPacketProbability(float probability) {
		// Only update the settings if we have updates remaining
		if(updatesRemaining > 0){
			// Take away an update if something actually changed
			if(maliciousPacketProbability != probability)
				updatesRemaining--;
			photonView.RPC("RPC_StartingPoint_SetMaliciousPacketProbability", RpcTarget.AllBuffered, probability);
			return true;
		} else return false;
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
