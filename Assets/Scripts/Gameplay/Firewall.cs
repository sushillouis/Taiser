using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Firewall : DraggableSnappedToPath, SelectionManager.ISelectable {
	// List of all firewalls
	public static Firewall[] firewalls = null;
	// Generator for IDS
	static uint nextID = 0;

	// Reference to the attached mesh renderer
	new public MeshRenderer renderer;

	// The material which represents the firewall's gate
	public Material gateMaterial;
	// The colors that the firewall's gate can become
	public Color[] colors;

	// The details of packets that should be filtered
	public PacketRule filterRules = PacketRule.Default;
	// Variable tracking what the correct rule for this firewall is (updated whenever a malicious packet hits this firewall and cleared when starting point rules are updated)
	public PacketRule correctRule;

	// Variable used to uniquely identify a firewall
	[SerializeField] uint _ID;
	public uint ID {
		get => _ID;
		protected set => _ID = value;
	}


	// De/register the start function on wave ends
	void OnEnable(){ firewalls = FindObjectsOfType<Firewall>(); } // Update the list of firewalls
	void OnDisable(){ firewalls = FindObjectsOfType<Firewall>(); } // Update the list of firewalls

	// When we stop dragging forward the move to the player manager and reset our position if the move was invalid
	protected override void OnEndDrag(){
		base.OnEndDrag();

		if(WhiteHatPlayerManager.instance.MoveFirewall(this, SelectionManager.instance.hovered) != WhiteHatBaseManager.ErrorCodes.NoError){
			transform.position = savedDraggingPosition;
			transform.rotation = savedDraggingRotation;
		}
	}

	// When the firewall is created make sure its filter rules are defaulted
	void Awake(){
		SetID();
		// SetFilterRules(PacketRule.Default); // Make sure that the base filter rules are applied
		SetGateColor(filterRules[0].color);
	}

	// Function which synchronizes a firewall's ID over the network
	void SetID(){ if(NetworkingManager.isHost) photonView.RPC("RPC_Firewall_SetID", RpcTarget.AllBuffered, (int) nextID++); }
	[PunRPC] void RPC_Firewall_SetID(int id){
		ID = (uint) id;
	}

	// Function which gets a firewall from its ID
	public static Firewall GetFromID(uint id){
		foreach(Firewall f in firewalls)
			if(f.ID == id)
				return f;
		return null;
	}

	// Update the packet rules (Network Synced)
	// Returns true if we successfully updated, returns false otherwise
	public bool SetFilterRules(PacketRule rule){
		photonView.RPC("RPC_Firewall_SetFilterRules", RpcTarget.AllBuffered, rule.CompressedRuleString() );
		return true;
	}
	[PunRPC] void RPC_Firewall_SetFilterRules(string rules){
		filterRules = PacketRule.Parse(rules);

		// TODO: The logic for determining the gate color will need to change
		SetGateColor(filterRules[0].color);
	}

	// Function which sets the firewall's gate color (network synced)
	public void SetGateColor(PacketRule.Color color, bool _default = false){ photonView.RPC("RPC_Firewall_SetGateColor", RpcTarget.AllBuffered, color, _default); }
	[PunRPC] void RPC_Firewall_SetGateColor(PacketRule.Color color, bool _default){
		// Get the list of materials off the mesh
		Material[] mats = renderer.materials;
		// Replace the second one with a new instance of the gate material
		mats[1] = new Material(gateMaterial);
		if(!_default) mats[1].SetColor("_EmissionColor", colors[(int)color]); // Set the firewall color
		// Copy the material changes back to the model
		renderer.materials = mats;
	}


	// Function which starts the gradual move coroutine if it isn't already started
	public bool StartGradualMove(Vector3 targetPosition, Quaternion targetRotation){
		if(gradualMoveFirewallIsMoving) return false;

		StartCoroutine(gradualMoveFirewall(transform.position, transform.rotation, targetPosition, targetRotation));
		return true;
	}

	// Coroutine which gradually moves the firewall to the targeted position
	public float gradualMoveFirewallTimeTaken = 1f/3f; // The amount of time that it should take to rotate
	float gradualMoveFirewallStartTime; // Start time when the coroutine begins
	public bool gradualMoveFirewallIsMoving = false;
 	IEnumerator gradualMoveFirewall(Vector3 startingPosition, Quaternion startingRotation, Vector3 targetPosition, Quaternion targetRotation){
		// Save the start time
		gradualMoveFirewallStartTime = Time.time;
		gradualMoveFirewallIsMoving = true;
		// While the scaled time is less than 110% of the total time (some extra buffer just in case)
		while((Time.time - gradualMoveFirewallStartTime) / gradualMoveFirewallTimeTaken < 1.1){
			// Lerp from the starting direction to the target direction based on scaled time
			transform.position = Vector3.Lerp(startingPosition, targetPosition, (Time.time - gradualMoveFirewallStartTime) / gradualMoveFirewallTimeTaken);
			transform.rotation = Quaternion.Slerp(startingRotation, targetRotation, (Time.time - gradualMoveFirewallStartTime) / gradualMoveFirewallTimeTaken);
			yield return null;
		}

		gradualMoveFirewallIsMoving = false;
	}
}
