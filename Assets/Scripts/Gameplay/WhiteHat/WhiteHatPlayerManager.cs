using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class WhiteHatPlayerManager : WhiteHatBaseManager {

	// Reference to the click action
	public InputActionReference leftClickAction;
	// Reference to the right click action
	public InputActionReference rightClickAction;
	// Reference to the cancel action
	public InputActionReference cancelAction;
	// Reference to the GUI element where we drop error messages
	public TMPro.TextMeshProUGUI errorText;

	// Reference to the cursor displayed when placing a firewall
	public GameObject firewallCursor;

	// Reference to the panel which displays information about firewalls and packets
	public GameObject firewallPacketPanel;
	// Reference to the panel holding advisor buttons
	public GameObject firewallPacketAdvisorPanel;
	// Reference to the firewall and packet header labels
	public TMPro.TextMeshProUGUI firewallPacketPanelFirewallText, firewallPacketPanelPacketText;
	// References to all of the toggles in the firewall panel
	public Toggle[] firewallPacketPanelToggles;

	// References to buttons which are disabled for advisors
	public Button removeFirewallButton, makeHoneypotButton;

	// Proposed packet rule
	PacketRule proposedRule;

	// Packet Select Panel
	public GameObject packetSelectPanel;

	// Enum what a click currently means
	protected enum ClickState {
		Selecting,
		SpawningFirewall,
		SelectingFirewallToMove,
		MovingFirewall,
		SelectingDestinationToMakeHoneypot,
	}
	// Variable defining what should happen when we click
	protected ClickState clickState = ClickState.Selecting;


	// De/register the click listener as well as Selection Manager event listeners
	public void OnEnable(){
		leftClickAction.action.Enable();
		leftClickAction.action.performed += OnClickPressed;
		rightClickAction.action.Enable();
		rightClickAction.action.performed += OnCancel;
		cancelAction.action.Enable();
		cancelAction.action.performed += OnCancel;
		SelectionManager.hoverChangedEvent += OnHoverChanged;
		SelectionManager.packetSelectEvent += OnPacketSelected;
		SelectionManager.firewallSelectEvent += OnFirewallSelected;
		SelectionManager.destinationSelectEvent += ShowPacketSelectPanel;
	}
	void OnDisable(){
		leftClickAction.action.performed -= OnClickPressed;
		rightClickAction.action.performed -= OnCancel;
		cancelAction.action.performed -= OnCancel;
		SelectionManager.hoverChangedEvent -= OnHoverChanged;
		SelectionManager.packetSelectEvent -= OnPacketSelected;
		SelectionManager.firewallSelectEvent -= OnFirewallSelected;
	}


	// -- Callbacks --


	// Function which responds to UI buttons that change the current click state
	public void OnSetClickState(int clickState){
		this.clickState = (ClickState)clickState;
	}

	// Function which responds to the remove selected firewall button
	public void OnRemoveSelectedFirewall(){
		Firewall selected = getSelected<Firewall>();
		SelectionManager.instance.SelectGameObject(null); // Make sure that the selection manager is not pointed at the item when we delete it
		DestroyFirewall(selected);

		// Make sure the firewall panel closes
		OnClosePacketFirewallPanel();
	}

	// Function called when the close button of the firewall panel is pressed
	public void OnClosePacketFirewallPanel(){
		firewallPacketPanel.SetActive(false);
		firewallPacketPanelFirewallText.gameObject.SetActive(false);
		firewallPacketPanelPacketText.gameObject.SetActive(false);
	}

	// Callback which responds to cancel (escape and right click) events
	void OnCancel(InputAction.CallbackContext ctx){
		// Ignore click releases
		if(!ctx.ReadValueAsButton()) return;
		// Ignore UI clicks
		if(EventSystem.current.IsPointerOverGameObject()) return;

		clickState = ClickState.Selecting;
		OnHoverChanged(SelectionManager.instance.hovered);
	}

	// Callback which handles when the selected packet changes
	void OnPacketSelected(Packet p){
		showPacketPanel(p);
	}



	// Callback which handles when the selected firewall changes
	void OnFirewallSelected(Firewall f){
		// Error if we don't own the firewall
		if(NetworkingManager.isPrimary && f.photonView.Controller != NetworkingManager.localPlayer){
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't modify the settings of a Firewall you don't own.");
			return;
		}

		showFirewallPanel(f);
	}

	// Callback which handles when the currently hovered grid piece changes
	void OnHoverChanged(GameObject newHover){
		if( newHover is null 					// If there isn't a new hover...
		  || newHover.tag != "FirewallTarget"	// Or the hover target can't host a firewall...
		  // Or we aren't in a state where we need the firewall placement indicator...
		  || !(clickState == ClickState.SpawningFirewall || clickState == ClickState.MovingFirewall || (clickState == ClickState.SelectingFirewallToMove && SelectionManager.instance.selected?.GetComponent<Firewall>() != null))
		){
			// Disable the firewall cursor
			firewallCursor.SetActive(false);
			return;
		}

		// Otherwise enable the firewall cursor and snap it to the hovered point
		firewallCursor.SetActive(true);
		Vector3 position;
		Quaternion rotation;
		Firewall.PathToPositionRotation(newHover, out position, out rotation);
		firewallCursor.transform.position = position;
		firewallCursor.transform.rotation = rotation;
	}

	// Callback which handles when one of the toggles in the firewall panel is adjusted
	public virtual void OnFirewallToggleSelected(int deltaNumber){
		// Disable filter settings when we are just opening the panel for the first time
		if(firewallJustSelected) return;
		// Don't do anything if we switched off a toggle
		if(!firewallPacketPanelToggles[deltaNumber].isOn) return;

		// Don't bother with this function if we don't have a firewall selected
		Firewall selected = getSelected<Firewall>();
		if(selected == null) return;

		PacketRule rules = selected.filterRules;
		PacketRule.Details d = rules[0];

		// Set the correct filter rules based on the given input
		switch(deltaNumber){
			case 0: d.size = PacketRule.Size.Small; break;
			case 1: d.size = PacketRule.Size.Medium; break;
			case 2: d.size = PacketRule.Size.Large; break;
			case 3: d.shape = PacketRule.Shape.Cube; break;
			case 4: d.shape = PacketRule.Shape.Sphere; break;
			case 5: d.shape = PacketRule.Shape.Cone; break;
			case 6: d.color = PacketRule.Color.Blue; break;
			case 7: d.color = PacketRule.Color.Green; break;
			case 8: d.color = PacketRule.Color.Pink; break;
		}

		string newRule = new PacketRule.LiteralNode(d).RuleString();
		if(SetFirewallFilterRules(selected, PacketRule.Parse(newRule)))
			showFirewallPanel(selected); // Reload the firewall panel if we failed to update the settings
	}

	// Callback which makes the selected destination a honeypot (or sets the relevant click state so that the next click will make a destination a honeypot)
	public virtual void MakeHoneypot(){
		// If we need to select a destination...
		if(getSelected<Destination>() is null){
			clickState = ClickState.SelectingDestinationToMakeHoneypot;
			return;
		}

		// If the selection is a destination, make it a honeypot
		if(!MakeDestinationHoneypot(getSelected<Destination>()))
			// Play a sound to indicate that settings were updated
			AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated", .5f);

		// Reset the click state
		clickState = ClickState.Selecting;
	}

	// Callback which responds to click events (ignoring click release events and events already handled by the UI)
	void OnClickPressed(InputAction.CallbackContext ctx){
		// Ignore click releases
		if(!ctx.ReadValueAsButton()) return;
		// Ignore UI clicks
		if(EventSystem.current.IsPointerOverGameObject()) return;

		switch(clickState){
			case ClickState.Selecting: SelectionManager.instance.SelectUnderCursor(); break; // If we are selecting, simply tell the selection manager to select whatever is under the mouse
			case ClickState.SpawningFirewall: OnClick_SpawningFirewall(); break;
			case ClickState.SelectingFirewallToMove: OnClick_SelectingFirewallToMove(); break;
			case ClickState.MovingFirewall: OnClick_MovingFirewall(); break;
			case ClickState.SelectingDestinationToMakeHoneypot: OnClick_SelectingDestinationToMakeHoneypot(); break;
		}
	}


	// -- Click Events --


	// Function which handles clicks when we should be placing firewalls
	void OnClick_SpawningFirewall(){
		// If we are the primary player...
		if(NetworkingManager.isPrimary){
			// Place a firewall at the currently hovered path piece (the base class takes care of most error handling)
			Firewall spawned = SpawnFirewall(SelectionManager.instance.hovered);
			// If we succeeded, mark the new fire wall as selected and reset the click state
			if(spawned != null){
				SelectionManager.instance.SelectGameObject(spawned.gameObject);
				clickState = ClickState.Selecting;

				// Make sure the placement cursor is hidden
				OnHoverChanged(SelectionManager.instance.hovered);

				// Play a sound to indicate that it has spawned
				AudioManager.instance.soundFXPlayer.PlayTrackImmediate("FirewallSpawn", .5f);
			}
		// If we are an advisor... simply suggest where a firewall should be placed
		} else if(!SpawnSuggestedFirewall(SelectionManager.instance.hovered))
 			clickState = ClickState.Selecting;
	}

	// Function which handles clicks when we should be selecting a firewall to move
	void OnClick_SelectingFirewallToMove(){
		// If we need to select a firewall...
		if(getSelected<Firewall>() == null){
			// Tell the selection manager to select whatever is under it
			SelectionManager.instance.SelectUnderCursor(/*No events*/ false);

			// If its selection isn't a firewall, give the user an error message
			if(SelectionManager.instance.selected == null || SelectionManager.instance.selected.GetComponent<Firewall>() == null){
				SelectionManager.instance.SelectGameObject(null);
				ErrorHandler(ErrorCodes.FirewallNotSelected, "A Firewall to move must be selected!");
				return;
			}

			// If the selection is a firewall, switch the state so the next click moves the selected firewall
			clickState = ClickState.MovingFirewall;
		// If we already have a firewall selected, simply move the selected firewall
		} else
			OnClick_MovingFirewall();
	}

	// Function which handles clicks when we are supposed to be moving firewalls
	void OnClick_MovingFirewall(){
		if(MoveFirewall(getSelected<Firewall>(), SelectionManager.instance.hovered)){
			clickState = ClickState.Selecting;

			// Make sure the placement cursor is hidden
			OnHoverChanged(SelectionManager.instance.hovered);

			// Play a sound to indicate that it has moved
			AudioManager.instance.soundFXPlayer.PlayTrackImmediate("FirewallSpawn", .5f);
		}
	}

	// function which handles clicks when we are supposed to be making destinations into honeypots
	public void OnClick_SelectingDestinationToMakeHoneypot(){
		// If we need to select a destination...
		if(getSelected<Destination>() == null){
			// Tell the selection manager to select whatever is under it
			SelectionManager.instance.SelectUnderCursor(/*No events*/ false);

			// If its selection isn't a destination, give the user an error message
			if(SelectionManager.instance.selected == null || SelectionManager.instance.selected.GetComponent<Destination>() == null){
				SelectionManager.instance.SelectGameObject(null);
				ErrorHandler(ErrorCodes.FirewallNotSelected, "A Destination to make into a Honeypot must be selected!");
				return;
			}
		}

		MakeHoneypot();
	}


	// -- Advisor Callbacks --

	public override void OnProposedNewFirewallFilterRules(Firewall f, PacketRule r) {
		// Save the rule as propsesed
		proposedRule = r;

		// Override what is currently selected
		SelectionManager.instance.SelectGameObject(f.gameObject);
		// Show the firewall panel
		showFirewallPanel(f);
		firewallJustSelected = true;

		// Make sure nothing is interactable
		foreach(Toggle t in firewallPacketPanelToggles)
			t.interactable = false;

		// Set the correct toggle states
		firewallPacketPanelToggles[0].isOn = r[0].size == PacketRule.Size.Small;
		firewallPacketPanelToggles[1].isOn = r[0].size == PacketRule.Size.Medium;
		firewallPacketPanelToggles[2].isOn = r[0].size == PacketRule.Size.Large;
		firewallPacketPanelToggles[3].isOn = r[0].shape == PacketRule.Shape.Cube;
		firewallPacketPanelToggles[4].isOn = r[0].shape == PacketRule.Shape.Sphere;
		firewallPacketPanelToggles[5].isOn = r[0].shape == PacketRule.Shape.Cone;
		firewallPacketPanelToggles[6].isOn = r[0].color == PacketRule.Color.Blue;
		firewallPacketPanelToggles[7].isOn = r[0].color == PacketRule.Color.Green;
		firewallPacketPanelToggles[8].isOn = r[0].color == PacketRule.Color.Pink;

		// Show the advisor buttons
		firewallPacketAdvisorPanel.SetActive(true);

		// Re-enable events
		firewallJustSelected = false;
	}

	// Callback when we are proposed that we should make a destination a honeypot
	public override void OnProposedMakeDestinationHoneypot(Destination toModify) {
		// TODO: How should we present this to the player?
	}

	// Callback when a firewall rule from an advisor is accepted
	public void OnFirewallAdvisorAccept(){
		// If nothing ges wrong changing the firewall rule...
		if( !SetFirewallFilterRules(getSelected<Firewall>(), proposedRule) ){
			// Close the panel
			OnClosePacketFirewallPanel();
			// Clear propsed changes
			proposedRule = new PacketRule();
		}
	}

	// Callback when a firewall rule from an advisor is rejected
	public void OnFirewallAdvisorReject(){
		// Close the panel
		OnClosePacketFirewallPanel();
		// Clear propsed changes
		proposedRule = new PacketRule();
	}


	// -- Show Panels --


	// Function which shows the firewall panel
	bool firewallJustSelected = false; // Boolean which tracks if we just selected the firewall, and if we did it prevents toggle updates from registering
	public virtual void showFirewallPanel(Firewall f){
		firewallJustSelected = true; // Disable toggle callbacks

		// Set all of the toggles as interactable (only for the white hat's primary player)
		foreach(Toggle t in firewallPacketPanelToggles)
			t.interactable = true;

		// Set the correct toggle states
		firewallPacketPanelToggles[0].isOn = f.filterRules[0].size == PacketRule.Size.Small;
		firewallPacketPanelToggles[1].isOn = f.filterRules[0].size == PacketRule.Size.Medium;
		firewallPacketPanelToggles[2].isOn = f.filterRules[0].size == PacketRule.Size.Large;
		firewallPacketPanelToggles[3].isOn = f.filterRules[0].shape == PacketRule.Shape.Cube;
		firewallPacketPanelToggles[4].isOn = f.filterRules[0].shape == PacketRule.Shape.Sphere;
		firewallPacketPanelToggles[5].isOn = f.filterRules[0].shape == PacketRule.Shape.Cone;
		firewallPacketPanelToggles[6].isOn = f.filterRules[0].color == PacketRule.Color.Blue;
		firewallPacketPanelToggles[7].isOn = f.filterRules[0].color == PacketRule.Color.Green;
		firewallPacketPanelToggles[8].isOn = f.filterRules[0].color == PacketRule.Color.Pink;

		// Update the text to represent the number of updates remaining
		FirewallSettingsUpdated(f);

		// Display the correct header
		firewallPacketPanelFirewallText.gameObject.SetActive(true);
		firewallPacketPanelPacketText.gameObject.SetActive(false);
		// Display the panel
		firewallPacketPanel.SetActive(true);
		// Make sure to hide the advisor buttons
		firewallPacketAdvisorPanel.SetActive(false);

		firewallJustSelected = false; // Re-enable toggle callbacks
	}


	// Function which shows the packet panel
	public void showPacketPanel(Packet p){
		// Set all of the toggles as uninteractable
		foreach(Toggle t in firewallPacketPanelToggles)
			t.interactable = false;

		// Set the correct toggle states
		firewallPacketPanelToggles[0].isOn = p.details.size == PacketRule.Size.Small;
		firewallPacketPanelToggles[1].isOn = p.details.size == PacketRule.Size.Medium;
		firewallPacketPanelToggles[2].isOn = p.details.size == PacketRule.Size.Large;
		firewallPacketPanelToggles[3].isOn = p.details.shape == PacketRule.Shape.Cube;
		firewallPacketPanelToggles[4].isOn = p.details.shape == PacketRule.Shape.Sphere;
		firewallPacketPanelToggles[5].isOn = p.details.shape == PacketRule.Shape.Cone;
		firewallPacketPanelToggles[6].isOn = p.details.color == PacketRule.Color.Blue;
		firewallPacketPanelToggles[7].isOn = p.details.color == PacketRule.Color.Green;
		firewallPacketPanelToggles[8].isOn = p.details.color == PacketRule.Color.Pink;

		// Display the correct header
		firewallPacketPanelFirewallText.gameObject.SetActive(false);
		firewallPacketPanelPacketText.gameObject.SetActive(true);
		// Display the panel
		firewallPacketPanel.SetActive(true);
		// Make sure to hide the advisor buttons
		firewallPacketAdvisorPanel.SetActive(false);
	}

	// Function which shows the PacketSelectPanel
	public void ShowPacketSelectPanel(Destination d) {
		packetSelectPanel.SetActive(true);
    }

	public void HidePacketSelectPanel(Destination d) {
		packetSelectPanel.SetActive(false);
    }


	// -- Callbacks --


	// Function called whenever a firewall's settings are meaninfully updated (updated and actually changed)
	protected override void FirewallSettingsUpdated(Firewall updated){
		// Play the success sound
		AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");
	}


	// -- Error Handling --


	// Override the error handler.
	protected override void ErrorHandler(BaseSharedBetweenHats.ErrorCodes errorCode, string error){
		// Continue all of the logic in the base handler
		base.ErrorHandler(errorCode, error);

		// Play a sound to indicate that an error occurred
		AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdateFailed");

		// If there are too many firewalls switch back to selecting mode
		if(errorCode == ErrorCodes.TooManyFirewalls){
			clickState = ClickState.Selecting;
			// Make sure the placement cursor is hidden
			OnHoverChanged(SelectionManager.instance.hovered);
		}

		// But also present the new error to the screen
		errorText.text = error;
	}
}
