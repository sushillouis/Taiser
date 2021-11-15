using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// Class which mangages the blackhat side advisor (extends blackhat player)
public class BlackHatAdvisorManager : BlackHatPlayerManager {

	// Function called when we propse changes to starting point rules
	public void OnStartPacketProposePressed(){
		// If the packet panel is open (instead of the starting point panel) abort!
		if(packetStartPanelPacketHeader.gameObject.activeSelf) return;

		// Make sure we have a starting point selected
		StartingPoint selected = getSelected<StartingPoint>();
		if(selected is null) return;

		// Determine the details from the selected toggles
		PacketRule.Details details = new PacketRule.Details();
		if(packetStartPanelToggles[0].isOn) details.size = PacketRule.Size.Small;
		if(packetStartPanelToggles[1].isOn) details.size = PacketRule.Size.Medium;
		if(packetStartPanelToggles[2].isOn) details.size = PacketRule.Size.Large;
		if(packetStartPanelToggles[3].isOn) details.shape = PacketRule.Shape.Cube;
		if(packetStartPanelToggles[4].isOn) details.shape = PacketRule.Shape.Sphere;
		if(packetStartPanelToggles[5].isOn) details.shape = PacketRule.Shape.Cone;
		if(packetStartPanelToggles[6].isOn) details.color = PacketRule.Color.Blue;
		if(packetStartPanelToggles[7].isOn) details.color = PacketRule.Color.Green;
		if(packetStartPanelToggles[8].isOn) details.color = PacketRule.Color.Pink;

		// Generate a rule from the details and propse it
		string newRule = new PacketRule.LiteralNode(details).RuleString();
		if( !ProposeNewStartPointMalciousPacketRules(selected, PacketRule.Parse(newRule)) ){
			// If the rule was successfully propsed play the settings update sound for feedback
			AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");
			// Then close the panel
			OnClosePacketStartPanel();
			OnCloseProbabilityLikelihoodPanel();
		}
	}

	// Function called when we propse changes to starting point probability or destination likelihood
	public void OnProbabilityLikelihoodProposePressed(){
		// If we have the starting point probablity panel open...
		if(probabilityLikelihoodPanelProbabilityHeader.gameObject.activeSelf) {
			// Make sure we have a starting point selected
			StartingPoint selected = getSelected<StartingPoint>();
			if(selected  is null) return;

			// Propse the new probability, closing the panel and playing the success sound if it was sent
			if( !ProposeNewStartPointMaliciousPacketProbability(selected, probabilityLikelihoodPanelSlider.value) ){
				AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");
				OnClosePacketStartPanel();
				OnCloseProbabilityLikelihoodPanel();
			}
		// If we have the destination likelihood panel open...
		} else if(probabilityLikelihoodPanelLikelihoodHeader.gameObject.activeSelf) {
			// Make sure we have a destination selected
			Destination selected = getSelected<Destination>();
			if(selected is null) return;

			// Propse the new likelihood, closing the panel and playing the success sound if it was sent
			if( !ProposeNewDestinationMaliciousPacketTargetLikelihood(selected, (int) probabilityLikelihoodPanelSlider.value) ){
				AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");
				OnCloseProbabilityLikelihoodPanel();
			}
		}
	}


	// -- Base Class Overrides --

	// Don't update settings when the probablity/likelihood panel is closed
	public override void OnCloseProbabilityLikelihoodPanel(){
		probabilityLikelihoodPanel.gameObject.SetActive(false);
	}

	// Do nothing when a starting point rule toggle is updated
	public override void OnStartingPointToggleSelected(int deltaNumber){}

	// When the probablity likelihood slider is updated, only update the text
	public override void OnProbabilityLikelihoodSliderUpdate(float value){
		updateProbabilityLikelihoodText();
	}

	// Show packet panel make sure the propse button is hidden
	public override void showPacketPanel(Packet p){
		base.showPacketPanel(p);

		packetStartAdvisorPanel.SetActive(false);
	}

	// Show starting point panel makes sure the propse button is not hidden
	public override void showStartingPointPanel(StartingPoint p){
		base.showStartingPointPanel(p);

		packetStartAdvisorPanel.SetActive(true);
		probabilityLikelihoodAdvisorPanel.SetActive(true);
	}

	// Show likelihood panel makes sure the propose button is not hidden
	public override void showLikelihoodPanel(Destination d){
		base.showLikelihoodPanel(d);

		probabilityLikelihoodAdvisorPanel.SetActive(true);
	}
}
