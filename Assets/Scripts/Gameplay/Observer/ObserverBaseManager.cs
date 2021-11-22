using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ObserverBaseManager : BaseSharedBetweenHats
{
	// Error codes used by the error handling system

	// Override instance to represent the Whitehat type
	new static public ObserverBaseManager instance
	{
		get => BaseSharedBetweenHats.instance as ObserverBaseManager;
	}


	// When we awake perform all of the code for a singleton and also ensure that the prefab paths are good to be used (removes extra stuff unity's copy path feature gives us)
	override protected void Awake()
	{
		base.Awake();
	}

	// -- Derived Class Callbacks --


	// Function called whenever a firewall's settings are meaninfully updated (updated and actually changed)
	protected virtual void FirewallSettingsUpdated(Firewall updated) { }
	protected virtual void DestinationSettingsUpdated(Destination updated) { }
}
