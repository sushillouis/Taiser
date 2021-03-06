using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Packet : MonoBehaviourPun, SelectionManager.ISelectable {
	// This packet's mesh filter
	public MeshFilter filter;
	// This packet's mesh renderer
	new public MeshRenderer renderer;
	// This packet's rigidbody
	new public Rigidbody rigidbody;
	// This object's selection cylinder
	public GameObject selectionCylinder;

	// List of meshes which define this packet's shape
	public Mesh[] meshes;
	// The material that the rendering is based on
	public Material material;
	// List of colors for the packet to become
	public UnityEngine.Color[] colors;

	// Speed of the packets depending on difficulty
	public float[] speeds = new float[3] {/*easy*/.8f, /*medium*/1, /*hard*/1};


	// -- Properties --


	// Property defining the packet's details (color, size, shape) (automatically network synced)
	[SerializeField]
	PacketRule.Details _details;
	public PacketRule.Details details {
		get => _details;
		set => SetProperties(value, _isMalicious);
	}

	// Property defining if the packet is malicious (automatically network synced)
	[SerializeField]
	bool _isMalicious = false;
	public bool isMalicious {
		get => _isMalicious;
		set => SetProperties(_details, value);
	}


	// Nodes defining the start and end point of the packet's journey
	public StartingPoint startPoint;
	public Destination destination;
	// Path to get from the start point to the destination point
	public List<PathNodeBase> path = null;


	// Manages packet movement
	void Update() {
		// Packet movement is controlled by the host
		if(!NetworkingManager.isHost) return;

		// If we don't have a path, create one (network synced)
		if(path == null || path.Count == 0) setStartDestinationAndPath(startPoint, destination);

		// Follow the path
		FollowPath();
	}

	// Function called whenever the packet interacts with another trigger
	void OnTriggerEnter(Collider collider){
		if(!NetworkingManager.isHost) return;

		// If the trigger was a destination...
		if(collider.transform.tag == "Destination"){
			// Process scoring (if the collided destination isn't a honeypot)
			if(!collider.gameObject.GetComponent<Destination>().isHoneypot)
				ScoreManager.instance.ProcessScoreEvent(isMalicious ? ScoreManager.ScoreEvent.MaliciousSuccess : ScoreManager.ScoreEvent.GoodSuccess);
			// If the packet is malicious play a sound and particle effect
			if(isMalicious){
				AudioManager.instance.soundFXPlayer.PlayTrackImmediate("MaliciousSuccess");
				destination.PlayParticleSimulation();
			}

			// Destroy the packet after it has had a few seconds to enter the destination
			StartCoroutine(DestroyAfterSeconds(1));
		} else if(collider.transform.tag == "Firewall") {
			Firewall firewall = collider.gameObject.GetComponent<Firewall>();

			if(firewall.filterRules.Contains(details) ){
				// Process scoring
				ScoreManager.instance.ProcessScoreEvent(isMalicious ? ScoreManager.ScoreEvent.MaliciousDestroyed : ScoreManager.ScoreEvent.GoodDestroyed);
				// Play a sound depending on if the packet is malicious or not
				if(isMalicious)	AudioManager.instance.soundFXPlayer.PlayTrackImmediate("MaliciousDestroyed");
				else AudioManager.instance.soundFXPlayer.PlayTrackImmediate("MaliciousSuccess");

				StartCoroutine(DestroyAfterSeconds(.5f));
			}
		}
	}

	// Function which determines if a packet is malicious or not and then generates/loads the appropriate packet details
	public void initPacketDetails(){
		// Determine if this packet is malicious or not (not network synced, we will network sync when we set the details)
		_isMalicious = UnityEngine.Random.Range(0f, 1f) <= startPoint.maliciousPacketProbability;

		// If the packet is malicious, change its target to be based on the malicious weights
		if(isMalicious){
			Destination[] destinations = Destination.getMaliciousWeightedList();
			setStartDestinationAndPath(startPoint, destinations[UnityEngine.Random.Range(0, destinations.Length)]);
		}

		// Set the packet details (if the packed is malicious the network property synchronizer will load the correct settings from the starting point)
		details = startPoint.randomNonMaliciousPacketDetails();
	}


	// -- Movement Functions --


	// Function which moves the packet along the path
	int pathIndex = 1; // Variable defining the next waypoint in the path
	float lastDistance = Mathf.Infinity; // Variable defining how far this packet was from the next waypoint last frame
	void FollowPath(){
		// Determine the direction we should be heading in
		Vector3 direction = (Utilities.positionNoY(path[pathIndex].transform.position) - Utilities.positionNoY(path[pathIndex - 1].transform.position)).normalized;
		// Apply that direction to the rigidbody's velocity
		rigidbody.velocity = direction * speeds[(int)GameManager.difficulty];
		// Calculate the distance to the next waypoint
		float distance = Mathf.Abs((Utilities.positionNoY(path[pathIndex].transform.position) - Utilities.positionNoY(transform.position)).magnitude);

		// If we have started moving backwards...
		if(distance > lastDistance){
			// Snap to the current waypoint
			transform.position = Utilities.positionSetY(path[pathIndex].transform.position, transform.position.y);

			// Look at the next waypoint (if it exists)
			if(pathIndex + 1 < path.Count){
				++pathIndex; // Updates the current waypoint to the next waypoint
				StartCoroutine(GradualRotation(transform.rotation, Quaternion.LookRotation(Utilities.positionNoY(path[pathIndex].transform.position) - Utilities.positionNoY(path[pathIndex - 1].transform.position))));
			}

			// Reset previous distance
			lastDistance = Mathf.Infinity;
		// Otherwise... update the previous distance
		} else lastDistance = distance;
	}

	// Coroutine which gradually rotates the packet from the starting direction to the target direction
	public float timeToRotate = 1f/3f; // The amount of time that it should take to rotate
	float startTime; // Start time when the coroutine begins
	IEnumerator GradualRotation(Quaternion startingDirection, Quaternion targetDirection){
		// Save the start time
		startTime = Time.time;
		// While the scaled time is less than 110% of the total time (some extra buffer just in case)
		while((Time.time - startTime) / timeToRotate < 1.1){
			// Lerp from the starting direction to the target direction based on scaled time
			transform.rotation = Quaternion.Slerp(startingDirection, targetDirection, (Time.time - startTime) / timeToRotate);
			yield return null;
		}
	}


	// -- Network Synchronization Functions --


	// Wrapper function which calls all of the functions needed to setup this packet's path
	// NOTE: The network syncing relies on each starting point and destination having a unique name!
	public void setStartDestinationAndPath(StartingPoint startPoint, Destination destination){
		SetStartPoint(startPoint);
		SetDestination(destination);
		InitPath();
	}

	// Sets the start point (network synced)
	// NOTE: The network syncing relies on each starting point and destination having a unique name!
	public void SetStartPoint(StartingPoint startPoint){ photonView.RPC("RPC_Packet_SetStartPoint", RpcTarget.AllBuffered, startPoint.name); }
	[PunRPC] void RPC_Packet_SetStartPoint(string startPointName){
		startPoint = GameObject.Find(startPointName).GetComponent<StartingPoint>();

		// If we are the host make sure that the object is properly positioned
		if(NetworkingManager.isHost)
			transform.position = Utilities.positionSetY(startPoint.transform.position, .25f);
	}

	// Sets the destination (network synced)
	// NOTE: The network syncing relies on each starting point and destination having a unique name!
	public void SetDestination(Destination Destination){ photonView.RPC("RPC_Packet_SetDestination", RpcTarget.AllBuffered, Destination.name); }
	[PunRPC] void RPC_Packet_SetDestination(string DestinationName){
		destination = GameObject.Find(DestinationName).GetComponent<Destination>();

		// While the current destination is a honeypot and the packet is malicious, set the destination to the next index in the destinations array
		if(!isMalicious && NetworkingManager.isHost)
			while(destination.isHoneypot){
				int index = System.Array.FindIndex(Destination.destinations, d => d.isHoneypot);
				index = (index + 1) % Destination.destinations.Length; // Modulus ensures that we remain in the bounds of the array
				SetDestination(Destination.destinations[index]);
			}
	}

	// Generates a path from the start point to the destination (network synced)
	// NOTE: The network syncing relies on each starting point and destination having a unique name!
	public void InitPath(){ photonView.RPC("RPC_Packet_InitPath", RpcTarget.AllBuffered); }
	[PunRPC] void RPC_Packet_InitPath(){
		path = startPoint.findPathTo(destination);
	}


	// Synchronizes the properties across the network
	// NOTE: The starting point must be set before this function can properly do its job
	public void SetProperties(PacketRule.Color color, PacketRule.Size size, PacketRule.Shape shape, bool isMalicious){ SetProperties(new PacketRule.Details(color, size, shape), isMalicious); }
	public void SetProperties(PacketRule.Details details, bool isMalicious){ photonView.RPC("RPC_Packet_SetProperties", RpcTarget.AllBuffered, details.color, details.size, details.shape, isMalicious, UnityEngine.Random.Range(0, startPoint.spawnedMaliciousPacketRules.Count) ); }
	[PunRPC] void RPC_Packet_SetProperties(PacketRule.Color color, PacketRule.Size size, PacketRule.Shape shape, bool isMalicious, int maliciousPacketRuleIndex){
		// Ensure the local properties match the remote ones
		_isMalicious = isMalicious;
		if(!_isMalicious) _details = new PacketRule.Details(color, size, shape);
		else _details = startPoint.spawnedMaliciousPacketRules[maliciousPacketRuleIndex];

		// Set the mesh based on the shape
		filter.mesh = meshes[(int)details.shape];

		// Get the list of materials off the mesh
		Material[] mats = renderer.materials;
		// Replace the first one with a new instance of the packet material
		mats[0] = new Material(material);
		mats[0].SetColor( "_EmissionColor", colors[(int)details.color] * ((int)details.size) * .5f ); // Set the packet color and emmisive intensity
		// Copy the material changes back to the model
		renderer.materials = mats;

		// Set the size of the packet
		selectionCylinder.transform.parent = null;
		switch(details.size){
			case PacketRule.Size.Small: transform.localScale = Utilities.toVec(.1f); break;
			case PacketRule.Size.Medium: transform.localScale = Utilities.toVec(.2f); break;
			case PacketRule.Size.Large: transform.localScale = Utilities.toVec(.3f); break;
		}

		// Ensure that the malicious packet indicator is properly positioned
		selectionCylinder.transform.position = renderer.bounds.center;
		selectionCylinder.transform.parent = transform;

		// Display the malicious circle indicator if the packet is malicious and the game difficulty is easy
		if(isMalicious && GameManager.difficulty == GameManager.Difficulty.Easy) selectionCylinder.SetActive(true);
		else selectionCylinder.SetActive(false);
	}


	// Coroutine which destroys the packet after the specified number of seconds
	IEnumerator DestroyAfterSeconds(float seconds){
		yield return new WaitForSeconds(seconds);
		Destroy();
	}
	// Destroys the packet (network synced)
	public void Destroy(){ photonView.RPC("RPC_Packet_Destroy", RpcTarget.AllBuffered); }
	[PunRPC] void RPC_Packet_Destroy(){
		if(!NetworkingManager.isHost) return;

		PhotonNetwork.Destroy(gameObject);
	}
}
