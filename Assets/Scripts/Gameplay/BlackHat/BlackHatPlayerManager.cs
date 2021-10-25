using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class BlackHatPlayerManager : BlackHatBaseManager {

	// Reference to the click action
	public InputActionReference leftClickAction;
	// Reference to the GUI element where we drop error messages
	public TMPro.TextMeshProUGUI errorText;

	// Reference to the packet panel
	public GameObject packetStartPanel;
	// Reference to the packet panel headers
	public TMPro.TextMeshProUGUI packetStartPanelPacketHeader, packetStartPanelStartHeader;
	// Reference to all of the toggles in the packet panel
	public Toggle[] packetStartPanelToggles;

	// Reference to the probability/likelihood panel
	public GameObject probabilityLikelihoodPanel;
	// Reference to the probability/likelihood panel headers
	public TMPro.TextMeshProUGUI probabilityLikelihoodPanelProbabilityHeader, probabilityLikelihoodPanelLikelihoodHeader;
	// Reference to the probability/likelihood slider
	public Slider probabilityLikelihoodPanelSlider;
	// Reference to the text field representing the value of the probability/likelihood slider
	public TMPro.TextMeshProUGUI probabilityLikelihoodPanelValueText;

	// De/register the click listener as well as Selection Manager event listeners
	void OnEnable(){
		leftClickAction.action.Enable();
		leftClickAction.action.performed += OnClickPressed;
		SelectionManager.packetSelectEvent += OnPacketSelected;
		SelectionManager.startingPointSelectEvent += OnStartingPointSelected;
		SelectionManager.destinationSelectEvent += OnDestinationSelected;
	}
	void OnDisable(){
		leftClickAction.action.performed -= OnClickPressed;
		SelectionManager.packetSelectEvent -= OnPacketSelected;
		SelectionManager.startingPointSelectEvent -= OnStartingPointSelected;
		SelectionManager.destinationSelectEvent += OnDestinationSelected;
	}


	// -- Callbacks --


	// Function called when the close button of the packet/starting point panel is pressed
	public void OnClosePacketStartPanel(){
		packetStartPanel.SetActive(false);
	}

	// Function called when when the close button of the probability/likelihood panel is pressed, updates the settings to refelect the value of the slider
	public void OnCloseProbabilityLikelihoodPanel(){
		probabilityLikelihoodPanel.gameObject.SetActive(false);

		// Get a reference to staring point and destination (one of them should be null)
		StartingPoint startPoint = getSelected<StartingPoint>();
		Destination destination = getSelected<Destination>();

		// Attempt to change the starting point's malicious probability
		if (startPoint != null)
			ChangeStartPointMaliciousPacketProbability(startPoint, probabilityLikelihoodPanelSlider.value);
		// Attempt to change the destination's malicious target probability
		if(destination != null)
			ChangeDestinationMaliciousPacketTargetLikelihood(destination, (int) probabilityLikelihoodPanelSlider.value);
	}

	// Callback which responds to click events (ignoring click release events and events already handled by the UI)
	void OnClickPressed(InputAction.CallbackContext ctx){
		// Ignore click releases
		if(!ctx.ReadValueAsButton()) return;
		// Ignore UI clicks
		if(EventSystem.current.IsPointerOverGameObject()) return;

		SelectionManager.instance.SelectUnderCursor();
	}

	// Callback which shows the packet panel when a packet is selected
	void OnPacketSelected(Packet p){
		showPacketPanel(p);
		OnCloseProbabilityLikelihoodPanel();
	}

	// Callback which shows the starting point panels when a packet is selected
	void OnStartingPointSelected(StartingPoint p){
		showStartingPointPanel(p);
	}

	// Callback which shows the the destination panel when a destination is selected
	void OnDestinationSelected(Destination d){
		showLikelihoodPanel(d);

		OnClosePacketStartPanel();
	}

	// Callback which handles when one of the toggles in the starting point panel is adjusted
	public void OnStartingPointToggleSelected(int deltaNumber){
		// Disable adjusting settings when we are just opening the panel for the first time
		if(startingPointJustSelected) return;
		// Don't do anything if we switched off a toggle
		if(!packetStartPanelToggles[deltaNumber].isOn) return;

		// Don't bother with this function if we don't have a starting point selected
		StartingPoint selected = getSelected<StartingPoint>();
		if(selected == null) return;

		// Set the correct spawning rules based on the given input
		PacketRule rules = selected.spawnedMaliciousPacketRules;
		PacketRule.Details d = rules[0];
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
		if(!ChangeStartPointMalciousPacketRules(selected, PacketRule.Parse(newRule)))
			showStartingPointPanel(selected); // Reload the starting point panel if we failed to update the settings
	}

	public void OnProbabilityLikelihoodSliderUpdate(float value){
		// Don't bother with this function if we just selected something
		if(destinationJustSelected || startingPointJustSelected) return;

		updateProbabilityLikelihoodText();
	}


	// -- Show Panels --


	// Function which shows the packet panel
	public void showPacketPanel(Packet p){
		// Set all of the toggles as not interactable
		foreach(Toggle t in packetStartPanelToggles)
			t.interactable = false;

		// Set the correct toggle states
		packetStartPanelToggles[0].isOn = p.details.size == PacketRule.Size.Small;
		packetStartPanelToggles[1].isOn = p.details.size == PacketRule.Size.Medium;
		packetStartPanelToggles[2].isOn = p.details.size == PacketRule.Size.Large;
		packetStartPanelToggles[3].isOn = p.details.shape == PacketRule.Shape.Cube;
		packetStartPanelToggles[4].isOn = p.details.shape == PacketRule.Shape.Sphere;
		packetStartPanelToggles[5].isOn = p.details.shape == PacketRule.Shape.Cone;
		packetStartPanelToggles[6].isOn = p.details.color == PacketRule.Color.Blue;
		packetStartPanelToggles[7].isOn = p.details.color == PacketRule.Color.Green;
		packetStartPanelToggles[8].isOn = p.details.color == PacketRule.Color.Pink;

		// Display the correct header
		packetStartPanelPacketHeader.gameObject.SetActive(true);
		packetStartPanelStartHeader.gameObject.SetActive(false);
		// Display the panel
		packetStartPanel.SetActive(true);
	}

	// Function which shows the starting point panel
	bool startingPointJustSelected = false;
	public void showStartingPointPanel(StartingPoint p){
		startingPointJustSelected = true; // Disable toggle callbacks

		// Set all of the toggles as interactable (only for the blackhat's primary player)
		foreach(Toggle t in packetStartPanelToggles)
			if(NetworkingManager.isPrimary) t.interactable = true;
			else t.interactable = false;

		// Set the correct toggle states
		packetStartPanelToggles[0].isOn = p.spawnedMaliciousPacketRules[0].size == PacketRule.Size.Small;
		packetStartPanelToggles[1].isOn = p.spawnedMaliciousPacketRules[0].size == PacketRule.Size.Medium;
		packetStartPanelToggles[2].isOn = p.spawnedMaliciousPacketRules[0].size == PacketRule.Size.Large;
		packetStartPanelToggles[3].isOn = p.spawnedMaliciousPacketRules[0].shape == PacketRule.Shape.Cube;
		packetStartPanelToggles[4].isOn = p.spawnedMaliciousPacketRules[0].shape == PacketRule.Shape.Sphere;
		packetStartPanelToggles[5].isOn = p.spawnedMaliciousPacketRules[0].shape == PacketRule.Shape.Cone;
		packetStartPanelToggles[6].isOn = p.spawnedMaliciousPacketRules[0].color == PacketRule.Color.Blue;
		packetStartPanelToggles[7].isOn = p.spawnedMaliciousPacketRules[0].color == PacketRule.Color.Green;
		packetStartPanelToggles[8].isOn = p.spawnedMaliciousPacketRules[0].color == PacketRule.Color.Pink;

		// Display the correct header
		packetStartPanelPacketHeader.gameObject.SetActive(false);
		packetStartPanelStartHeader.gameObject.SetActive(true);
		// Display the panel
		packetStartPanel.SetActive(true);

		// Display the probability panel
		showProbabilityPanel(p);
		StartingPointSettingsUpdated(p);

		startingPointJustSelected = false; // Re-enable toggle callbacks
	}

	// Function which shows the starting point probability panel
	public void showProbabilityPanel(StartingPoint p){
		// Set the slider as interactable (only for the blackhat's primary player)
		if(NetworkingManager.isPrimary) probabilityLikelihoodPanelSlider.interactable = true;
		else probabilityLikelihoodPanelSlider.interactable = false;

		// Update the slider's properties
		probabilityLikelihoodPanelSlider.minValue = 0;
		probabilityLikelihoodPanelSlider.maxValue = 1;
		probabilityLikelihoodPanelSlider.wholeNumbers = false;
		// Update the slider's value and text
		probabilityLikelihoodPanelSlider.value = p.maliciousPacketProbability;
		updateProbabilityLikelihoodText();

		// Display the correct header
		probabilityLikelihoodPanelProbabilityHeader.gameObject.SetActive(true);
		probabilityLikelihoodPanelLikelihoodHeader.gameObject.SetActive(false);
		// Display the panel
		probabilityLikelihoodPanel.SetActive(true);
	}

	// Function which shows the destination likelihood panel
	bool destinationJustSelected = false;
	public void showLikelihoodPanel(Destination d){
		destinationJustSelected = true; // Disable toggle callbacks

		// Set the slider as interactable (only for the blackhat's primary player)
		if(NetworkingManager.isPrimary) probabilityLikelihoodPanelSlider.interactable = true;
		else probabilityLikelihoodPanelSlider.interactable = false;

		// Update the slider's properties
		probabilityLikelihoodPanelSlider.minValue = 0;
		probabilityLikelihoodPanelSlider.maxValue = 20;
		probabilityLikelihoodPanelSlider.wholeNumbers = true;
		// Update the slider's value and text
		probabilityLikelihoodPanelSlider.value = d.maliciousPacketDestinationLikelihood;
		updateProbabilityLikelihoodText();

		// Display the correct header
		probabilityLikelihoodPanelProbabilityHeader.gameObject.SetActive(false);
		probabilityLikelihoodPanelLikelihoodHeader.gameObject.SetActive(true);
		// Display the panel
		probabilityLikelihoodPanel.SetActive(true);
		DestinationSettingsUpdated(d);

		destinationJustSelected = false; // Re-enable toggle callbacks
	}


	// -- Callbacks --


	// Function called whenever a firewall's settings are meaninfully updated (updated and actually changed)
	protected override void StartingPointSettingsUpdated(StartingPoint updated){
		packetStartPanelStartHeader.text = "Start Point - " + updated.updatesRemaining;

		// Play the success sound
		if(updated.updatesRemaining > 0) AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");
	}

	// Function called whenever a firewall's settings are meaninfully updated (updated and actually changed)
	protected override void DestinationSettingsUpdated(Destination updated){
		probabilityLikelihoodPanelLikelihoodHeader.text = "Likelihood - " + updated.updatesRemainingBlack;

		// Play the success sound
		if(updated.updatesRemainingBlack > 0) AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");
	}


	// -- Helpers --


	// Function which updates the slider label to reflect the state of the slider
	void updateProbabilityLikelihoodText(){
		// Format the text appropriately to the starting point (2 decimal place probability) and destination (integer likelihood)
		if(getSelected<StartingPoint>() != null)
			probabilityLikelihoodPanelValueText.text = probabilityLikelihoodPanelSlider.value.ToString("0.##");
		else if(getSelected<Destination>() != null)
			probabilityLikelihoodPanelValueText.text = "" + (int) probabilityLikelihoodPanelSlider.value;
	}


	// -- Error Handling --


	// Override the error handler.
	protected override void ErrorHandler(BaseSharedBetweenHats.ErrorCodes errorCode, string error){
		// Continue all of the logic in the base handler
		base.ErrorHandler(errorCode, error);

		// Play the settings error sound if this is a no updates remaining
		if(errorCode == ErrorCodes.NoUpdatesRemaining)
			AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdateFailed", .5f);

		// But also present the new error to the screen
		errorText.text = error;
	}
}
