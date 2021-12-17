using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BlackHatBaseManager : BaseSharedBetweenHats {
	// Callbacks
	public delegate void StartingPointRuleEventCallback(StartingPoint toModify, PacketRule rules);
	public delegate void StartingPointProbabilityEventCallback(StartingPoint toModify, float probability);
	public delegate void DestiniationLikelihoodEventCallback(Destination toModify, int likelihood);
	// Events (NOTE: Events are fired before the action occures, in the callback the old state can be acquired from the object)
	public static StartingPointRuleEventCallback updateStartingPointRuleEvent;
	public static StartingPointRuleEventCallback proposeStartingPointRuleEvent;
	public static StartingPointProbabilityEventCallback updateStartingPointProbabilityEvent;
	public static StartingPointProbabilityEventCallback proposeStartingPointProbabilityEvent;
	public static DestiniationLikelihoodEventCallback updateDestinationLikelihoodEvent;
	public static DestiniationLikelihoodEventCallback proposeDestinationLikelihoodEvent;


	// Error codes used by the error handling system
	public new class ErrorCodes : BaseSharedBetweenHats.ErrorCodes {
		public static readonly int StartingPointNotSelected = 5;	// Error code indicating that a starting point was not selected
		public static readonly int DestinationNotSelected = 6;		// Error code indicating that a destination was not selected
		public static readonly int InvalidProbability = 7;			// Error code indicating that the provided probability is invalid

		// Required function to get the class up to par
		public ErrorCodes() {}
		public ErrorCodes(int _value) : base(_value) {}
		public static implicit operator int(ErrorCodes e) => e.value;
		public static implicit operator ErrorCodes(int value) => new ErrorCodes(value);
		// Implicit conversion to a bool (true if an error occurred, false otherwise)
		public static implicit operator bool(ErrorCodes e) => e != NoError;
	}

	// Override instance to represent the BlackkHat type
	new static public BlackHatBaseManager instance {
		get => BaseSharedBetweenHats.instance as BlackHatBaseManager;
	}


	// -- GameState Manipulators


	// Function which updates all of the starting points to have the specified malicious packet details
	public ErrorCodes ChangeAllStartPointsMalciousPacketRules(PacketRule rules){
		foreach(StartingPoint p in StartingPoint.startingPoints){
			ErrorCodes ret = ChangeStartPointMalciousPacketRules(p, rules);
			if(ret != ErrorCodes.NoError) return ret;
		}

		return ErrorCodes.NoError;
	}

	// Function which updates the specified starting point to have the specified malicious packet details
	public ErrorCodes ChangeStartPointMalciousPacketRules(StartingPoint toModify, PacketRule rules){
		// Error if the starting point to destroy is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.StartingPointNotSelected, "A Starting Point to modify must be selected!");
			return ErrorCodes.StartingPointNotSelected;
		}
		// Error if we don't own the starting point
		if(NetworkingManager.isPrimary && toModify.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't modify Starting Points you don't own!");
			return ErrorCodes.WrongPlayer;
		}

		updateStartingPointRuleEvent?.Invoke(toModify, rules);
		if(toModify.SetMaliciousPacketRules(rules))
			StartingPointSettingsUpdated(toModify);
		return ErrorCodes.NoError;
	}

	// Function proposes an update to the specified starting point to have the specified malicious packet details
	public ErrorCodes ProposeNewStartPointMalciousPacketRules(StartingPoint toModify, PacketRule rules){
		// Error if the starting point to destroy is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.StartingPointNotSelected, "A Starting Point to modify must be selected!");
			return ErrorCodes.StartingPointNotSelected;
		}
		// Error if we aren't an advisor
		if(NetworkingManager.isPrimary || NetworkingManager.isObserver){
			ErrorHandler(ErrorCodes.WrongPlayer, "Only Advisors can propose changes to the Primary Player.");
			return ErrorCodes.WrongPlayer;
		}

		proposeStartingPointRuleEvent?.Invoke(toModify, rules);
		// Synchronize the call through the game manager
		GameManager.instance.photonView.RPC("RPC_BlackHatBaseManager_ProposeNewStartPointMalciousPacketRules", RpcTarget.AllBuffered, (int) toModify.ID, rules.CompressedRuleString());
		return ErrorCodes.NoError;
	}

	// Callback when we are proposed new rules for a starting point
	public virtual void OnProposedNewStartPointMalciousPacketRules(StartingPoint toModify, PacketRule rules) { }

	// Function which changes the probability of a spawned packet being malicious for of all the starting points
	public ErrorCodes ChangeAllStartPointsMaliciousPacketProbabilities(float probability){
		foreach(StartingPoint p in StartingPoint.startingPoints){
			ErrorCodes ret = ChangeStartPointMaliciousPacketProbability(p, probability);
			if(ret != ErrorCodes.NoError) return ret;
		}

		return ErrorCodes.NoError;
	}

	// Function which changes the probability of a spawned packet being malicious for the specified starting point
	public ErrorCodes ChangeStartPointMaliciousPacketProbability(StartingPoint toModify, float probability){
		// Error if the starting point to modify is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.StartingPointNotSelected, "A Starting Point to modify must be selected!");
			return ErrorCodes.StartingPointNotSelected;
		}
		// Error if we don't own the starting point
		if(NetworkingManager.isPrimary && toModify.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't modify Starting Points you don't own!");
			return ErrorCodes.WrongPlayer;
		}
		// Error if the provided probability is invalid
		if(probability < 0 || probability > 1){
			ErrorHandler(ErrorCodes.InvalidProbability, "The probability " + probability + " is invalid!");
			return ErrorCodes.InvalidProbability;
		}

		updateStartingPointProbabilityEvent?.Invoke(toModify, probability);
		if(toModify.SetMaliciousPacketProbability(probability))
			StartingPointSettingsUpdated(toModify);
		return ErrorCodes.NoError;
	}

	// Function which proposes a new change to a starting point's probability
	public ErrorCodes ProposeNewStartPointMaliciousPacketProbability(StartingPoint toModify, float probability){
		// Error if the starting point to modify is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.StartingPointNotSelected, "A Starting Point to modify must be selected!");
			return ErrorCodes.StartingPointNotSelected;
		}
		// Error if we aren't an advisor
		if(NetworkingManager.isPrimary || NetworkingManager.isObserver){
			ErrorHandler(ErrorCodes.WrongPlayer, "Only Advisors can propose changes to the Primary Player.");
			return ErrorCodes.WrongPlayer;
		}
		// Error if the provided probability is invalid
		if(probability < 0 || probability > 1){
			ErrorHandler(ErrorCodes.InvalidProbability, "The probability " + probability + " is invalid!");
			return ErrorCodes.InvalidProbability;
		}

		proposeStartingPointProbabilityEvent?.Invoke(toModify, probability);
		// Synchronize the call through the game manager
		GameManager.instance.photonView.RPC("RPC_BlackHatBaseManager_ProposeNewStartPointMaliciousPacketProbability", RpcTarget.AllBuffered, (int) toModify.ID, probability);
		return ErrorCodes.NoError;
	}

	// Callback called when we receive a proposal for a new malicious packet probability
	public virtual void OnProposedNewStartPointMaliciousPacketProbability(StartingPoint toModify, float probability) {}

	// Function which changes the likelihood that a malicious packet will target the specified destination
	public ErrorCodes ChangeDestinationMaliciousPacketTargetLikelihood(Destination toModify, int likelihood){
		// Error if the destination to modify is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.DestinationNotSelected, "A Destination to modify must be selected!");
			return ErrorCodes.DestinationNotSelected;
		}
		// Error if we don't own the destination
		if(NetworkingManager.isPrimary && toModify.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't modify Destinations you don't own!");
			return ErrorCodes.WrongPlayer;
		}

		updateDestinationLikelihoodEvent?.Invoke(toModify, likelihood);
		if(toModify.SetMaliciousPacketDestinationLikelihood(likelihood))
			DestinationSettingsUpdated(toModify);
		return ErrorCodes.NoError;
	}

	// Function which proposes a new likelihood that a malicious packet will target the specified destination
	public ErrorCodes ProposeNewDestinationMaliciousPacketTargetLikelihood(Destination toModify, int likelihood){
		// Error if the destination to modify is null
		if(toModify is null){
			ErrorHandler(ErrorCodes.DestinationNotSelected, "A Destination to modify must be selected!");
			return ErrorCodes.DestinationNotSelected;
		}
		// Error if we aren't an advisor
		if(NetworkingManager.isPrimary || NetworkingManager.isObserver){
			ErrorHandler(ErrorCodes.WrongPlayer, "Only Advisors can propose changes to the Primary Player.");
			return ErrorCodes.WrongPlayer;
		}

		proposeDestinationLikelihoodEvent?.Invoke(toModify, likelihood);
		// Synchronize the call through the game manager
		GameManager.instance.photonView.RPC("RPC_BlackHatBaseManager_ProposeNewDestinationMaliciousPacketTargetLikelihood", RpcTarget.AllBuffered, (int) toModify.ID, likelihood);
		return ErrorCodes.NoError;
	}

	// Callback called when we receive a proposal for a new malicious packet likelihood
	public virtual void OnProposedNewDestinationMaliciousPacketTargetLikelihood(Destination toModify, int likelihood) {}


	// -- Derived Class Callbacks --


	// Function called whenever a firewall's settings are meaninfully updated (updated and actually changed)
	protected virtual void StartingPointSettingsUpdated(StartingPoint updated){ }
	protected virtual void DestinationSettingsUpdated(Destination updated){ }
}
