using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class WhiteHatBaseManager : BaseSharedBetweenHats {
	// Callbacks
	public delegate void FirewallEventCallback(Firewall spawned);
	public delegate void MovedFirewallEventCallback(Firewall moved, Vector3 targetPosition, Quaternion targetRotation);
	public delegate void FirewallRulesEventCallback(Firewall toModify, PacketRule rules);
	public delegate void HoneypotEventCallback(Destination toModify);
	public delegate void SuggestedFirewallEventCallback(SuggestedFirewall moved, Vector3 targetPosition, Quaternion targetRotation);
	// Events (NOTE: Events are fired before the action occures, in the callback the old state can be acquired from the object)
	public static FirewallEventCallback spawnFirewallEvent;
	public static FirewallEventCallback destroyFirewallEvent;
	public static MovedFirewallEventCallback moveFirewallEvent;
	public static FirewallRulesEventCallback firewallUpdateEvent;
	public static FirewallRulesEventCallback firewallProposeEvent;
	public static HoneypotEventCallback honeypotUpdateEvent;
	public static HoneypotEventCallback honeypotProposeEvent;
	public static SuggestedFirewallEventCallback suggestedFirewallEvent;


	// Error codes used by the error handling system
	new public class ErrorCodes : BaseSharedBetweenHats.ErrorCodes {
		public static readonly int FirewallIsMoving = 5;		// Error code stating that the firewall is still moving
		public static readonly int FirewallNotSelected = 6;		// Error code stating that no firewall has been selected
		public static readonly int DestinationNotSelected = 7;	// Error code stating that no destination has been selected
		public static readonly int TooManyFirewalls = 8;		// Error code stating there are too many firewalls to place another
		public static readonly int TargetNotSelected = 9;		// Error code stating that no target has been selected

		// Required function to get the class up to par
		public ErrorCodes() {}
		public ErrorCodes(int _value) : base(_value) {}
		public static implicit operator int(ErrorCodes e) => e.value;
		public static implicit operator ErrorCodes(int value) => new ErrorCodes(value);
		// Implicit conversion to a bool (true if an error occurred, false otherwise)
		public static implicit operator bool(ErrorCodes e) => e != NoError;
	}

	// Override instance to represent the Whitehat type
	new static public WhiteHatBaseManager instance {
		get => BaseSharedBetweenHats.instance as WhiteHatBaseManager;
	}


	// A string referencing the firewall prefab path
	public string firewallPrefabPath;
	// A string referencing the firewall cursor prefab path
	public string suggestedFirewallPrefabPath;
	// The number of firewalls that can exist at any given time
	public int maximumPlaceableFirewalls = 2;

	public SuggestedFirewall suggestedFirewall = null;

	// When we awake perform all of the code for a singleton and also ensure that the prefab paths are good to be used (removes extra stuff unity's copy path feature gives us)
	override protected void Awake(){
		base.Awake();
		Utilities.PreparePrefabPath(ref firewallPrefabPath);
		Utilities.PreparePrefabPath(ref suggestedFirewallPrefabPath);
	}


	// -- GameState Manipulators


	// Function which spawns and returns a firewall on the given path piece (network synced)
	protected Firewall SpawnFirewall(GameObject targetPathPiece){
		// Error on invalid path piece
		if(targetPathPiece is null){
			ErrorHandler(ErrorCodes.TargetNotSelected, "A location to place the firewall at must be selected!");
			return null;
		}
		// Error if the path piece can't have firewalls on it
		if(targetPathPiece.tag != "FirewallTarget"){
			ErrorHandler(ErrorCodes.InvalidTarget, "Firewalls can't be placed on the selected location!");
			return null;
		}
		// Error if there are already too many firewalls
		if(Firewall.firewalls != null && Firewall.firewalls.Length >= maximumPlaceableFirewalls){
			ErrorHandler(ErrorCodes.TooManyFirewalls, "Only " + maximumPlaceableFirewalls + " Firewalls can be placed at a time!");
			return null;
		}

		// Spawn the new firewall over the network
		Firewall spawned = PhotonNetwork.Instantiate(firewallPrefabPath, new Vector3(0, 100, 0), Quaternion.identity).GetComponent<Firewall>();
		spawnFirewallEvent?.Invoke(spawned);
		// Move it to its proper position
		MoveFirewall(spawned, targetPathPiece, /*not animated*/ false);

		return spawned;
	}

	// Function which moves a firewall to the targetedPathPiece
	// This function returns true if the move was successful, and false if any errors occurred
	// The function by default causes the firewall to be smoothly moved to its new location over the course of half a second (this behavior can be disabled by passing false to animated)
	public virtual ErrorCodes MoveFirewall(Firewall toMove, GameObject targetPathPiece, bool animated = true){
		// Error if the firewall to move is null
		if(toMove is null){
			ErrorHandler(ErrorCodes.FirewallNotSelected, "A Firewall to move must be selected!");
			return ErrorCodes.FirewallNotSelected;
		}
		// Error if we don't own the firewall
		if(NetworkingManager.isPrimary && toMove.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't move firewalls you don't own!");
			return ErrorCodes.WrongPlayer;
		}
		// Error if the path piece to move too is null
		if(targetPathPiece is null){
			ErrorHandler(ErrorCodes.TargetNotSelected, "A location to move to must be selected!");
			return ErrorCodes.TargetNotSelected;
		}
		// Error if the path piece can't have firewalls on it
		if(targetPathPiece.tag != "FirewallTarget"){
			ErrorHandler(ErrorCodes.InvalidTarget, "Firewalls can't be moved to the selected location!");
			return ErrorCodes.InvalidTarget;
		}

		Vector3 position;
		Quaternion rotation;
		Firewall.PathToPositionRotation(targetPathPiece, out position, out rotation);

		// If the firewall is already in the correct position, don't bother moving it
		if(position == toMove.transform.position && rotation == toMove.transform.rotation)
			return ErrorCodes.NoError;

		// Fire the event
		moveFirewallEvent?.Invoke(toMove, position, rotation);
		// If we should be animating the movement...
		if(animated){
			// Try to start the movement and return an error if it is already moving
			if(!toMove.StartGradualMove(position, rotation)){
				ErrorHandler(ErrorCodes.FirewallIsMoving, "Wait until it is done moving!");
				return ErrorCodes.FirewallIsMoving;
			}
		// If we shouldn't be animating, simply snap the path piece to its destination
		} else {
			toMove.transform.position = position;
			toMove.transform.rotation = rotation;
		}

		// We have successfully moved the path piece, so return true
		return ErrorCodes.NoError;
	}

	// Function which destroys the given firewall
	protected virtual ErrorCodes DestroyFirewall(Firewall toDestroy){
		// Error if the firewall to destroy is null
		if(toDestroy is null){
			ErrorHandler(ErrorCodes.FirewallNotSelected, "A Firewall to destroy must be selected!");
			return ErrorCodes.FirewallNotSelected;
		}
		// Error if we don't own the firewall
		if(NetworkingManager.isPrimary && toDestroy.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't destroy firewalls you don't own!");
			return ErrorCodes.WrongPlayer;
		}

		destroyFirewallEvent?.Invoke(toDestroy);
		// Network destroy the firewall
		PhotonNetwork.Destroy(toDestroy.gameObject);
		return ErrorCodes.NoError;
	}

	// Function which updates the settings of the given firewall
	protected virtual ErrorCodes SetFirewallFilterRules(Firewall toModify, PacketRule filterRules){
		// Error if the firewall to destroy is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.FirewallNotSelected, "A Firewall to modify must be selected!");
			return ErrorCodes.FirewallNotSelected;
		}
		// Error if we don't own the firewall
		if(NetworkingManager.isPrimary && toModify.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't modify firewalls you don't own!");
			return ErrorCodes.WrongPlayer;
		}

		firewallUpdateEvent?.Invoke(toModify, filterRules);
		if(toModify.SetFilterRules(filterRules))
			FirewallSettingsUpdated(toModify);
		return ErrorCodes.NoError;
	}

	// Function which proposes new settings for the given firewall
	protected virtual ErrorCodes ProposeNewFirewallFilterRules(Firewall toModify, PacketRule filterRules){
		// Error if the firewall to destroy is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.FirewallNotSelected, "A Firewall to modify must be selected!");
			return ErrorCodes.FirewallNotSelected;
		}
		// Error if we aren't an advisor
		if(NetworkingManager.isPrimary || NetworkingManager.isObserver){
			ErrorHandler(ErrorCodes.WrongPlayer, "Only Advisors can propose changes to the Primary Player.");
			return ErrorCodes.WrongPlayer;
		}

		firewallProposeEvent?.Invoke(toModify, filterRules);
		// Synchronize the call through the game manager
		GameManager.instance.photonView.RPC("RPC_WhiteHatBaseManager_ProposeNewFirewallFilterRules", RpcTarget.AllBuffered, (int) toModify.ID, filterRules.CompressedRuleString());
		return ErrorCodes.NoError;
	}

	// Callback when we are proposed new firewall filter ules
	public virtual void OnProposedNewFirewallFilterRules(Firewall toModify, PacketRule filterRules) {}

	// Function which marks the specified destination as a honeypot
	public ErrorCodes MakeDestinationHoneypot(Destination toModify){
		// Error if the destination to modify is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.DestinationNotSelected, "A Destination to modify must be selected!");
			return ErrorCodes.DestinationNotSelected;
		}

		honeypotUpdateEvent?.Invoke(toModify);
		if(toModify.SetIsHoneypot(true))
			DestinationSettingsUpdated(toModify);
		return ErrorCodes.NoError;
	}

	// Function which proposes that we mark the specified destination as a honeypot
	public ErrorCodes ProposeMakeDestinationHoneypot(Destination toModify){
		// Error if the destination to modify is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.DestinationNotSelected, "A Destination to modify must be selected!");
			return ErrorCodes.DestinationNotSelected;
		}

		honeypotProposeEvent?.Invoke(toModify);
		// Synchronize the call through the game manager
		GameManager.instance.photonView.RPC("RPC_WhiteHatBaseManager_ProposeMakeDestinationHoneypot", RpcTarget.AllBuffered, (int) toModify.ID);
		return ErrorCodes.NoError;
	}

	// Callback when we are proposed that we should make a destination a honeypot
	public virtual void OnProposedMakeDestinationHoneypot(Destination toModify) {}


	// -- Suggested Firewall --


	// Function which spawns or moves the suggested firewall to the selected path piece
	protected ErrorCodes SpawnSuggestedFirewall(GameObject targetPathPiece){
		// Error on invalid path piece
		if(targetPathPiece is null){
			ErrorHandler(ErrorCodes.TargetNotSelected, "A location to place the suggested firewall at must be selected!");
			return ErrorCodes.TargetNotSelected;
		}
		// Error if the path piece can't have firewalls on it
		if(targetPathPiece.tag != "FirewallTarget"){
			ErrorHandler(ErrorCodes.InvalidTarget, "Suggested firewalls can't be placed on the selected location!");
			return ErrorCodes.InvalidTarget;
		}

		Vector3 position;
		Quaternion rotation;
		Firewall.PathToPositionRotation(targetPathPiece, out position, out rotation);

		// If the suggested firewall doesn't exist spawn it (and start a timer which will delete it after 5 seconds)
		if(suggestedFirewall is null){
			suggestedFirewall = PhotonNetwork.Instantiate(suggestedFirewallPrefabPath, new Vector3(0, 100, 0), Quaternion.identity).GetComponent<SuggestedFirewall>();
			suggestedFirewall.transform.position = position;
			suggestedFirewall.transform.rotation = rotation;
		}

		suggestedFirewallEvent?.Invoke(suggestedFirewall, position, rotation);
		// Reset the deletion timer of the selected firewall and gradually move it to its new location
		suggestedFirewall.ResetDeleteTimer();
		suggestedFirewall.StartGradualMove(targetPathPiece.transform.position, targetPathPiece.transform.rotation);

		// TODO: should we return firewall already moving if the start gradual move returns false?
		return ErrorCodes.NoError;
	}


	// -- Derived Class Callbacks --


	// Function called whenever a firewall's settings are meaninfully updated (updated and actually changed)
	protected virtual void FirewallSettingsUpdated(Firewall updated){ }
	protected virtual void DestinationSettingsUpdated(Destination updated){ }
}
