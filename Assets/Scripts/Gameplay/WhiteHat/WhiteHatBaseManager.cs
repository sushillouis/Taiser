using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class WhiteHatBaseManager : BaseSharedBetweenHats {
	// Error codes used by the error handling system
	new public class ErrorCodes : BaseSharedBetweenHats.ErrorCodes {
		public static readonly int FirewallIsMoving = 4;		// Error code stating that the firewall is still moving
		public static readonly int FirewallNotSelected = 5;		// Error code stating that no firewall has been selected
		public static readonly int DestinationNotSelected = 6;	// Error code stating that no destination has been selected
		public static readonly int TooManyFirewalls = 7;		// Error code stating there are too many firewalls to place another
		public static readonly int TargetNotSelected = 8;		// Error code stating that no target has been selected

		// Required function to get the class up to par
		public ErrorCodes() {}
		public ErrorCodes(int _value) : base(_value) {}
		public static implicit operator int(ErrorCodes e) => e.value;
		public static implicit operator ErrorCodes(int value) => new ErrorCodes(value);
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
		if(targetPathPiece == null){
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
		// Move it to its proper position
		MoveFirewall(spawned, targetPathPiece, /*not animated*/ false);

		return spawned;
	}

	// Function which moves a firewall to the targetedPathPiece
	// This function returns true if the move was successful, and false if any errors occurred
	// The function by default causes the firewall to be smoothly moved to its new location over the course of half a second (this behavior can be disabled by passing false to animated)
	protected virtual bool MoveFirewall(Firewall toMove, GameObject targetPathPiece, bool animated = true){
		// Error if the firewall to move is null
		if(toMove == null){
			ErrorHandler(ErrorCodes.FirewallNotSelected, "A Firewall to move must be selected!");
			return false;
		}
		// Error if we don't own the firewall
		if(NetworkingManager.isPrimary && toMove.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't move firewalls you don't own!");
			return false;
		}
		// Error if the path piece to move too is null
		if(targetPathPiece == null){
			ErrorHandler(ErrorCodes.TargetNotSelected, "A location to move to must be selected!");
			return false;
		}
		// Error if the path piece can't have firewalls on it
		if(targetPathPiece.tag != "FirewallTarget"){
			ErrorHandler(ErrorCodes.InvalidTarget, "Firewalls can't be moved to the selected location!");
			return false;
		}

		// If we should be animating the movement...
		if(animated){
			// Try to start the movement and return an error if it is already moving
			if(!toMove.StartGradualMove(targetPathPiece.transform.position, targetPathPiece.transform.rotation)){
				ErrorHandler(ErrorCodes.FirewallIsMoving, "Wait until it is done moving!");
				return false;
			}
		// If we shouldn't be animating, simply snap the path piece to its destination
		} else {
			toMove.transform.position = targetPathPiece.transform.position;
			toMove.transform.rotation = targetPathPiece.transform.rotation;
		}

		// We have successfully moved the path piece, so return true
		return true;
	}

	// Function which destroys the given firewall
	protected virtual bool DestroyFirewall(Firewall toDestroy){
		// Error if the firewall to destroy is null
		if(toDestroy == null){
			ErrorHandler(ErrorCodes.FirewallNotSelected, "A Firewall to destroy must be selected!");
			return false;
		}
		// Error if we don't own the firewall
		if(NetworkingManager.isPrimary && toDestroy.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't destroy firewalls you don't own!");
			return false;
		}

		// TODO: Possibly add a particle system!

		// Network destroy the firewall
		PhotonNetwork.Destroy(toDestroy.gameObject);
		return true;
	}

	// Function which updates the settings of the given firewall
	protected virtual bool SetFirewallFilterRules(Firewall toModify, Packet.Details filterRules){
		// Error if the firewall to destroy is null
		if(toModify == null){
			ErrorHandler(ErrorCodes.FirewallNotSelected, "A Firewall to modify must be selected!");
			return false;
		}
		// Error if we don't own the firewall
		if(NetworkingManager.isPrimary && toModify.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't modify firewalls you don't own!");
			return false;
		}
		// Error if the firewall doesn't have any updates remaining
		if(toModify.updatesRemaining <= 0){
			ErrorHandler(ErrorCodes.NoUpdatesRemaining, "The Firewall doesn't have any updates remaining!");
			return false;
		}

		if(toModify.SetFilterRules(filterRules))
			FirewallSettingsUpdated(toModify);
		return true;
	}

	// Function which marks the specified destination as a honeypot
	public bool MakeDestinationHoneypot(Destination toModify){
		// Error if the destination to modify is null
		if(toModify == null){
			ErrorHandler(ErrorCodes.DestinationNotSelected, "A Destination to modify must be selected!");
			return false;
		}
		// Error if the destination doesn't have any updates remaining
		if(toModify.updatesRemainingWhite <= 0){
			ErrorHandler(ErrorCodes.NoUpdatesRemaining, "The Destination doesn't have any updates remaining!");
			return false;
		}

		if(toModify.SetIsHoneypot(true))
			DestinationSettingsUpdated(toModify);
		return true;
	}


	// -- Suggested Firewall --


	// Function which spawns or moves the suggested firewall to the selected path piece
	protected bool SpawnSuggestedFirewall(GameObject targetPathPiece){
		// Error on invalid path piece
		if(targetPathPiece is null){
			ErrorHandler(ErrorCodes.TargetNotSelected, "A location to place the suggested firewall at must be selected!");
			return false;
		}
		// Error if the path piece can't have firewalls on it
		if(targetPathPiece.tag != "FirewallTarget"){
			ErrorHandler(ErrorCodes.InvalidTarget, "Suggested firewalls can't be placed on the selected location!");
			return false;
		}

		// If the suggested firewall doesn't exist spawn it (and start a timer which will delete it after 5 seconds)
		if(suggestedFirewall is null){
			suggestedFirewall = PhotonNetwork.Instantiate(suggestedFirewallPrefabPath, new Vector3(0, 100, 0), Quaternion.identity).GetComponent<SuggestedFirewall>();
			suggestedFirewall.transform.position = targetPathPiece.transform.position;
			suggestedFirewall.transform.rotation = targetPathPiece.transform.rotation;
		}

		// Reset the deletion timer of the selected firewall and gradually move it to its new location
		suggestedFirewall.ResetDeleteTimer();
		return suggestedFirewall.StartGradualMove(targetPathPiece.transform.position, targetPathPiece.transform.rotation);
	}

	// -- Derived Class Callbacks --


	// Function called whenever a firewall's settings are meaninfully updated (updated and actually changed)
	protected virtual void FirewallSettingsUpdated(Firewall updated){ }
	protected virtual void DestinationSettingsUpdated(Destination updated){ }
}
