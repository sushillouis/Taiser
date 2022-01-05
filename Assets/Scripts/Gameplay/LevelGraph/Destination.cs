using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Destination : PathNodeBase, SelectionManager.ISelectable {
	// List of destinations
	public static Destination[] destinations = null;
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

	// Whenever a new destination is added update the list of destinations, and set its ID
	protected void Awake() { destinations = FindObjectsOfType<Destination>(); SetID(); }

	// Reference to the rendered mesh
	public MeshRenderer mesh;
	// References to the materials used when the destination is and isn't a honeypot
	public Material destinationMaterial, honeypotMaterial;
	// The particle system to spawn when a malicious packet hits us
	public GameObject particleSystemPrefab;

	// All of the likelihoods are added together, then the probability of this point being a packet's destination is <likelihood>/<totalLikelihood>
	// The likelihood of a packet targeting a HoneyPot is 0
	[SerializeField] int _packetDestinationLikelihood = 1;
	public int packetDestinationLikelihood {
		get {
			if(isHoneypot) return 0; // The likelihood of a packet targeting a HoneyPot is 0
			return _packetDestinationLikelihood;
		}
		set => _packetDestinationLikelihood = value;
	}
	// Likelihood for malicious packets to target this destination
	[SerializeField] int _maliciousPacketDestinationLikelihood = 1;
	public int maliciousPacketDestinationLikelihood {
		get => _maliciousPacketDestinationLikelihood;
		set => SetMaliciousPacketDestinationLikelihood(value);
	}
	// Weather or not this destination is a honeypot
	[SerializeField] bool _isHoneypot = false;
	public bool isHoneypot {
		get => _isHoneypot;
		set => SetIsHoneypot(value);
	}


	// TODO: Remove
	// The details of packets that should be filtered
	public PacketRule filterRules = PacketRule.Default;
	// Variable tracking what the correct rule for this firewall is (updated whenever a malicious packet hits this firewall and cleared when starting point rules are updated)
	public PacketRule correctRule = PacketRule.Default;

	// TODO: also remove

	public static int maxRecords = 16;
	public Queue<PacketRule.Details> packetLog = new Queue<PacketRule.Details>();

	public void LogPacket(PacketRule.Details prd) {
		//Debug.Log("Intercepted packet with details");
		//Debug.Log(prd);
		packetLog.Enqueue(prd);
		while (packetLog.Count > maxRecords)
			packetLog.Dequeue();

	}
    public int DestinationId;
    public UnityEngine.UI.Button DestinationButton;
    public Vector2 sizeDelta = new Vector2(5, 5);
    public Vector2 minSize = new Vector2(1, 1);
    public Vector2 maxSize = new Vector2(200, 200);
    public bool shouldGrow = false;
    public void AnimateDestinationButton(bool isMalicious)
    {
        RectTransform rt = DestinationButton.GetComponent<RectTransform>();
        Vector2 size = rt.sizeDelta;
        //Debug.Log("AnimateDestinationButton: size: " + size);
        if(size.x < maxSize.x) {
            rt.sizeDelta += sizeDelta;
        }

    }

	// Variable used to uniquely identify a destination
	[SerializeField] uint _ID;
	public uint ID {
		get => _ID;
		protected set => _ID = value;
	}

	// Function which synchronizes a destination's ID over the network
	void SetID(){ if(NetworkingManager.isHost) photonView.RPC("RPC_Destination_SetID", RpcTarget.AllBuffered, (int) nextID++); }
	[PunRPC] void RPC_Destination_SetID(int id){
		ID = (uint) id;
	}

	// Function which gets a destination from its ID
	public static Destination GetFromID(uint id){
		foreach(Destination d in destinations)
			if(d.ID == id)
				return d;
		return null;
	}

	// Function which updates the likelihood of a malicious packet targeting this destination (Network Synced)
	// Returns true if we successfully updated, returns false otherwise
	public bool SetMaliciousPacketDestinationLikelihood(int likelihood) {
		photonView.RPC("RPC_Destination_SetMaliciousPacketDestinationLikelihood", RpcTarget.AllBuffered, likelihood);
		return true;
	}
	[PunRPC] void RPC_Destination_SetMaliciousPacketDestinationLikelihood(int likelihood){
		_maliciousPacketDestinationLikelihood = likelihood;
	}

	// Function which updates if this is a honeypot or not (also ensures that none of the other destinations are marked as honey pots)
	// Returns true if we successfully updated, returns false otherwise
	public bool SetIsHoneypot(bool isHoneypot) {
		photonView.RPC("RPC_Destination_SetIsHoneypot", RpcTarget.AllBuffered, isHoneypot);
		return true;
	}
	[PunRPC] void RPC_Destination_SetIsHoneypot(bool isHoneypot){
		// Clear all of the honeypots (if we are setting a new one)
		if(isHoneypot)
			foreach(Destination d in destinations)
				d._isHoneypot = false;

		// Mark this destination as a honeypot
		this._isHoneypot = isHoneypot;

		// Update the material of all the destinations (if we are a whitehat)
		if(NetworkingManager.isWhiteHat)
			foreach(Destination d in destinations){
				var mats = d.mesh.materials;
				mats[0] = d.isHoneypot ? honeypotMaterial : destinationMaterial;
				d.mesh.materials = mats;
			}

	}

	// Function which plays the malicious packet particle simulation (Network Synced)
	public void PlayParticleSimulation() { photonView.RPC("RPC_Destination_PlayParticleSimulation", RpcTarget.AllBuffered); }
	[PunRPC] void RPC_Destination_PlayParticleSimulation(){
		// Spawn and play the particle simulation
		ParticleSystem ps = Instantiate(particleSystemPrefab, Utilities.positionSetY(transform.position, .25f), transform.rotation * Quaternion.Euler(0, -90, 0)).GetComponent<ParticleSystem>();
		ps.Play();
		// Destroy the particle simulation once it is done playing
		StartCoroutine(destroyWhenDonePlaying(ps));
	}

	// Coroutine which destroys a particle simulation once it is done playing
	IEnumerator destroyWhenDonePlaying(ParticleSystem toDestroy){
		// While the particle simulation is playing, wait...
		while(toDestroy.isPlaying)
			yield return null;

		// Destroy it
		Destroy(toDestroy.gameObject);
	}

	// Function which returns a weighted list of destinations
	public static Destination[] getWeightedList(){
		List<Destination> ret = new List<Destination>();
		// For each starting point, add it to the list a number of times equal to its <packetSourceLikelihood>
		foreach(Destination d in destinations)
			for(int i = 0; i < d.packetDestinationLikelihood; i++)
				ret.Add(d);

		return ret.ToArray();
	}

	// Function which returns a weighted list of destinations for malicious packets
	public static Destination[] getMaliciousWeightedList(){
		List<Destination> ret = new List<Destination>();
		// For each starting point, add it to the list a number of times equal to its <packetSourceLikelihood>
		foreach(Destination d in destinations)
			for(int i = 0; i < d.maliciousPacketDestinationLikelihood; i++)
				ret.Add(d);

		return ret.ToArray();
	}
}
