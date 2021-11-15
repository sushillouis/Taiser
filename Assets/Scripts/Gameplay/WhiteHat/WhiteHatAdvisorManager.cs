using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class WhiteHatAdvisorManager : WhiteHatPlayerManager {

	// Function called when we click the propose button after having selected a firewall
	public void OnFirewallPacketProposePressed(){
		// If the packet panel is open (instead of the firewall panel) abort!
		if(firewallPacketPanelPacketText.gameObject.activeSelf) return;

		// Make sure we have a firewall selected
		Firewall selected = getSelected<Firewall>();
		if(selected is null) return;

		// Determine the details from the selected toggles
		PacketRule.Details details = new PacketRule.Details();
		if(firewallPacketPanelToggles[0].isOn) details.size = PacketRule.Size.Small;
		if(firewallPacketPanelToggles[1].isOn) details.size = PacketRule.Size.Medium;
		if(firewallPacketPanelToggles[2].isOn) details.size = PacketRule.Size.Large;
		if(firewallPacketPanelToggles[3].isOn) details.shape = PacketRule.Shape.Cube;
		if(firewallPacketPanelToggles[4].isOn) details.shape = PacketRule.Shape.Sphere;
		if(firewallPacketPanelToggles[5].isOn) details.shape = PacketRule.Shape.Cone;
		if(firewallPacketPanelToggles[6].isOn) details.color = PacketRule.Color.Blue;
		if(firewallPacketPanelToggles[7].isOn) details.color = PacketRule.Color.Green;
		if(firewallPacketPanelToggles[8].isOn) details.color = PacketRule.Color.Pink;

		// Generate a rule from the details and propse it
		string newRule = new PacketRule.LiteralNode(details).RuleString();
		if( !ProposeNewFirewallFilterRules(selected, PacketRule.Parse(newRule)) ){
			// If the rule was successfully proposed play the settings update sound for feedback
			AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");
			// Then close the panel
			OnClosePacketFirewallPanel();
		}
	}

	// Callback which proposed making the selected destination a honeypot (or sets the relevant click state so that the next click will propose)
	public override void MakeHoneypot(){
		// If we need to select a destination...
		if(getSelected<Destination>() is null){
			clickState = ClickState.SelectingDestinationToMakeHoneypot;
			return;
		}

		// If the selection is a destination, propose it as a honeypot
		if( !ProposeMakeDestinationHoneypot(getSelected<Destination>()) )
			// Play a sound to indicate that settings were updated
			AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated", .5f);

		// Reset the click state
		clickState = ClickState.Selecting;
	}

	// When we show the firewall panel make sure it has the advisor buttons enabled
	public override void showFirewallPanel(Firewall f){
		base.showFirewallPanel(f);

		// Make sure to show the propose button
		firewallPacketAdvisorPanel.SetActive(true);
	}

	// Do nothing when the firewall toggles are changed
	public override void OnFirewallToggleSelected(int deltaNumber){	}

	// Do nothing when we are proposed a firewall filter rule
	public override void OnProposedNewFirewallFilterRules(Firewall toModify, PacketRule filterRules) {}
}
