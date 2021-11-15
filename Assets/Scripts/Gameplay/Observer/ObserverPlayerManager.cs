using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ObserverPlayerManager : ObserverBaseManager
{

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
	// Reference to the firewall and packet header labels
	public TMPro.TextMeshProUGUI firewallPacketPanelFirewallText, firewallPacketPanelPacketText;
	// References to all of the toggles in the firewall panel
	public Toggle[] firewallPacketPanelToggles;

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

	// References to buttons which are disabled for advisors
	public Button removeFirewallButton, makeHoneypotButton;

	// Enum what a click currently means
	enum ClickState
	{
		Selecting,
		SpawningFirewall,
		SelectingFirewallToMove,
		MovingFirewall,
		SelectingDestinationToMakeHoneypot,
	}
	// Variable defining what should happen when we click
	ClickState clickState = ClickState.Selecting;


	// De/register the click listener as well as Selection Manager event listeners
	public void OnEnable()
	{
		leftClickAction.action.Enable();
		leftClickAction.action.performed += OnClickPressed;
		rightClickAction.action.Enable();
		rightClickAction.action.performed += OnCancel;
		cancelAction.action.Enable();
		cancelAction.action.performed += OnCancel;
		SelectionManager.hoverChangedEvent += OnHoverChanged;
		SelectionManager.packetSelectEvent += OnPacketSelected;
		SelectionManager.firewallSelectEvent += OnFirewallSelected;
		SelectionManager.startingPointSelectEvent += OnStartingPointSelected;
		SelectionManager.destinationSelectEvent += OnDestinationSelected;

		// If we aren't the primary player, then we can't interact with the move, remove, or make-firewall buttons
		if (!NetworkingManager.isPrimary)
		{
			removeFirewallButton.interactable = false;
			makeHoneypotButton.interactable = false;
		}
		else
		{
			removeFirewallButton.interactable = true;
			makeHoneypotButton.interactable = true;
		}

		Debug.Log(firewallCursor);
	}
	void OnDisable()
	{
		leftClickAction.action.performed -= OnClickPressed;
		rightClickAction.action.performed -= OnCancel;
		cancelAction.action.performed -= OnCancel;
		SelectionManager.hoverChangedEvent -= OnHoverChanged;
		SelectionManager.packetSelectEvent -= OnPacketSelected;
		SelectionManager.firewallSelectEvent -= OnFirewallSelected;
		SelectionManager.startingPointSelectEvent -= OnStartingPointSelected;
		SelectionManager.destinationSelectEvent += OnDestinationSelected;
	}


	// -- Callbacks --

	// Function called when the close button of the packet/starting point panel is pressed
	public void OnClosePacketStartPanel()
	{
		packetStartPanel.SetActive(false);
	}

	// Function called when when the close button of the probability/likelihood panel is pressed, updates the settings to refelect the value of the slider
	public void OnCloseProbabilityLikelihoodPanel()
	{
		probabilityLikelihoodPanel.gameObject.SetActive(false);

		// Get a reference to staring point and destination (one of them should be null)
		StartingPoint startPoint = getSelected<StartingPoint>();
		Destination destination = getSelected<Destination>();
	}

	// Function which responds to UI buttons that change the current click state
	public void OnSetClickState(int clickState)
	{
		this.clickState = (ClickState)clickState;
	}

	// Function called when the close button of the firewall panel is pressed
	public void OnClosePacketFirewallPanel()
	{
		firewallPacketPanel.SetActive(false);
		firewallPacketPanelFirewallText.gameObject.SetActive(false);
		firewallPacketPanelPacketText.gameObject.SetActive(false);
	}

	// Callback which responds to cancel (escape and right click) events
	void OnCancel(InputAction.CallbackContext ctx)
	{
		// Ignore click releases
		if (!ctx.ReadValueAsButton()) return;
		// Ignore UI clicks
		if (EventSystem.current.IsPointerOverGameObject()) return;

		clickState = ClickState.Selecting;
		OnHoverChanged(SelectionManager.instance.hovered);
	}

	// Callback which handles when the selected packet changes
	void OnPacketSelected(Packet p)
	{
		showPacketPanel(p);

	}

	// Callback which handles when the selected firewall changes
	void OnFirewallSelected(Firewall f)
	{
		// Error if we don't own the firewall
		if (NetworkingManager.isPrimary && f.photonView.Controller != NetworkingManager.localPlayer)
		{
			ErrorHandler(ErrorCodes.WrongPlayer, "You can't modify the settings of a Firewall you don't own.");
			return;
		}

		showFirewallPanel(f);
	}

	// Callback which handles when the currently hovered grid piece changes
	void OnHoverChanged(GameObject newHover)
	{
		if (newHover is null                    // If there isn't a new hover...
		  || newHover.tag != "FirewallTarget"   // Or the hover target can't host a firewall...
												// Or we aren't in a state where we need the firewall placement indicator...
		  || !(clickState == ClickState.SpawningFirewall || clickState == ClickState.MovingFirewall || (clickState == ClickState.SelectingFirewallToMove && SelectionManager.instance.selected?.GetComponent<Firewall>() != null))
		)
		{
			// Disable the firewall cursor
			firewallCursor.SetActive(false);
			return;
		}

		// Otherwise enable the firewall cursor and snap it to the hovered point
		firewallCursor.SetActive(true);
		firewallCursor.transform.position = newHover.transform.position;
		firewallCursor.transform.rotation = newHover.transform.rotation;
	}

	// Callback which responds to click events (ignoring click release events and events already handled by the UI)
	void OnClickPressed(InputAction.CallbackContext ctx)
	{
		// Ignore click releases
		if (!ctx.ReadValueAsButton()) return;
		// Ignore UI clicks
		if (EventSystem.current.IsPointerOverGameObject()) return;

		switch (clickState)
		{
			case ClickState.Selecting: SelectionManager.instance.SelectUnderCursor(); break; // If we are selecting, simply tell the selection manager to select whatever is under the mouse
		}
	}

	// -- Show Panels --


	// Function which shows the firewall panel
	bool firewallJustSelected = false; // Boolean which tracks if we just selected the firewall, and if we did it prevents toggle updates from registering
	public void showFirewallPanel(Firewall f)
	{
		firewallJustSelected = true; // Disable toggle callbacks

		// Set all of the toggles as interactable (only for the white hat's primary player)
		foreach (Toggle t in firewallPacketPanelToggles)
			if (NetworkingManager.isPrimary) t.interactable = true;
			else t.interactable = false;

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
		packetStartPanelStartHeader.gameObject.SetActive(false);
		// Display the panel
		firewallPacketPanel.SetActive(true);

		firewallJustSelected = false; // Re-enable toggle callbacks
	}


	// Function which shows the packet panel
	public void showPacketPanel(Packet p)
	{
		// Set all of the toggles as uninteractable
		foreach (Toggle t in firewallPacketPanelToggles)
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
		packetStartPanelStartHeader.gameObject.SetActive(false);
		firewallPacketPanelPacketText.gameObject.SetActive(true);
		// Display the panel
		firewallPacketPanel.SetActive(true);
		// Hide the likelihood panel
		probabilityLikelihoodPanel.SetActive(false);
	}

	// Callback which shows the starting point panels when a packet is selected
	void OnStartingPointSelected(StartingPoint p)
	{
		showStartingPointPanel(p);
	}

	// Callback which shows the the destination panel when a destination is selected
	void OnDestinationSelected(Destination d)
	{
		showLikelihoodPanel(d);

		OnClosePacketStartPanel();
	}

	// Function which shows the starting point panel
	bool startingPointJustSelected = false;
	public void showStartingPointPanel(StartingPoint p)
	{
		startingPointJustSelected = true; // Disable toggle callbacks

		// Set all of the toggles as interactable (only for the blackhat's primary player)
		foreach (Toggle t in packetStartPanelToggles)
			if (NetworkingManager.isPrimary) t.interactable = true;
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
		firewallPacketPanelFirewallText.gameObject.SetActive(false);
		packetStartPanelStartHeader.gameObject.SetActive(true);
		// Display the panel
		packetStartPanel.SetActive(true);

		// Display the probability panel
		showProbabilityPanel(p);
		//StartingPointSettingsUpdated(p);

		startingPointJustSelected = false; // Re-enable toggle callbacks
	}

	// Function which shows the destination likelihood panel
	bool destinationJustSelected = false;
	public void showLikelihoodPanel(Destination d)
	{
		destinationJustSelected = true; // Disable toggle callbacks

		// Set the slider as interactable (only for the blackhat's primary player)
		if (NetworkingManager.isPrimary) probabilityLikelihoodPanelSlider.interactable = true;
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

	// Function which shows the starting point probability panel
	public void showProbabilityPanel(StartingPoint p)
	{
		// Set the slider as interactable (only for the blackhat's primary player)
		if (NetworkingManager.isPrimary) probabilityLikelihoodPanelSlider.interactable = true;
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

	// -- Callbacks --


	// Function called whenever a firewall's settings are meaninfully updated (updated and actually changed)
	protected override void FirewallSettingsUpdated(Firewall updated)
	{
		// Play the success sound
		AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");

	}

	// -- Helpers --


	// Function which updates the slider label to reflect the state of the slider
	void updateProbabilityLikelihoodText()
	{
		// Format the text appropriately to the starting point (2 decimal place probability) and destination (integer likelihood)
		if (getSelected<StartingPoint>() != null)
			probabilityLikelihoodPanelValueText.text = probabilityLikelihoodPanelSlider.value.ToString("0.##");
		else if (getSelected<Destination>() != null)
			probabilityLikelihoodPanelValueText.text = "" + (int)probabilityLikelihoodPanelSlider.value;
	}


	// -- Error Handling --


	// Override the error handler.
	protected override void ErrorHandler(BaseSharedBetweenHats.ErrorCodes errorCode, string error)
	{
		// Continue all of the logic in the base handler
		base.ErrorHandler(errorCode, error);

		// Play a sound to indicate that an error occurred
		AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdateFailed");

		// But also present the new error to the screen
		errorText.text = error;
	}
}
