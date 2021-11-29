using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using Photon.Pun;

public class LobbyScreenManager : Window {
	// The three screens that the lobby manages
	public GameObject roomListScreen, inRoomScreen, loadingPrompt, splashScreen, nameEntryScreen;

	// Background texture
	public GameObject background;

	// RoomList
	[Header("Room List")]
	// Reference to the prefab instantiated for each room listing
	public GameObject roomListingPrefab;
	public TMPro.TMP_InputField aliasTextbox;
	public List<TMPro.TextMeshProUGUI> roomLabels;
	public GameObject roomListContent;
	public GameObject[] joinButtons;

	// In Room
	[Header("In Room")]
	public TMPro.TMP_Dropdown sideDropdown;
	public DropdownController sideDropdownController;
	public TMPro.TMP_Dropdown roleDropdown;
	public DropdownController roleDropdownController;
	public Button startButton, leaveButton;
	public TMPro.TextMeshProUGUI[] playerLabels;
	public TMPro.TextMeshProUGUI[] advisorLabels;

	// Boolean tracking if the current lobby is a multiplayer lobby or not
	bool isMultiplayer = false;
	public Toggle multiplayerToggle;

	// Connect and disconnect to networking events
	void OnEnable(){
		NetworkingManager.connectedEvent += onConnect; 										// Run the 'onConnect' function once we are connected to the network!
		NetworkingManager.roomListUpdateEvent += UpdateRoomList; 							// Update the room list whenever a new one becomes available
		NetworkingManager.becomeRoomHostEvent += becomeRoomHost;							// Mark ourselves as ready when we become the host
		NetworkingManager.roomJoinEvent += joinedRoom; 										// Run join room function when we join the room
		NetworkingManager.roomLeaveEvent += onConnect; 										// Return to the game select screen when someone leaves the room
		NetworkingManager.roomOtherJoinEvent += playerJoinedOrLeft; 						// Sync shared state when someone else joins
		NetworkingManager.roomOtherLeaveEvent += playerJoinedOrLeft; 						// Sync shared state when someone else leaves
		NetworkingManager.roomPlayerPropertiesUpdateEvent += roomPlayerPropertiesChanged; 	// Update the player properties whenever they change
		NetworkingManager.roomStateUpdateEvent += roomStateChanged;							// Sync shared state when an update becomes available
	}
	void OnDisable(){
		NetworkingManager.connectedEvent -= onConnect;
		NetworkingManager.roomListUpdateEvent -= UpdateRoomList;
		NetworkingManager.becomeRoomHostEvent -= becomeRoomHost;
		NetworkingManager.roomJoinEvent -= joinedRoom;
		NetworkingManager.roomLeaveEvent -= onConnect;
		NetworkingManager.roomOtherJoinEvent -= playerJoinedOrLeft;
		NetworkingManager.roomOtherLeaveEvent -= playerJoinedOrLeft;
		NetworkingManager.roomPlayerPropertiesUpdateEvent -= roomPlayerPropertiesChanged;
		NetworkingManager.roomStateUpdateEvent -= roomStateChanged;
	}

	// When we load make sure the menus are hidden and the loading screen is visible
	void Start(){
		roomListScreen.SetActive(false);
		inRoomScreen.SetActive(false);
		splashScreen.SetActive(false);
		nameEntryScreen.SetActive(false);
		background.SetActive(false);
		loadingPrompt.SetActive(true);
	}


	// -- Networking Callbacks --

	void onConnect() {
		Debug.Log("Connection event fired");
		splashScreen.SetActive(false);
		roomListScreen.SetActive(false);
		inRoomScreen.SetActive(false);
		background.SetActive(true);

		// If the loading text is still present... destroy it
		if (loadingPrompt is object) {
			Destroy(loadingPrompt);
			loadingPrompt = null;
		}

		// Ensure that the player's alias is saved between loads of the main menu
		aliasTextbox.text = PhotonNetwork.LocalPlayer.NickName; // We use the photon network version here since the NetworkingManager players are nullified while not in a room
		if (aliasTextbox.text == "You") aliasTextbox.text = "";

		if (!NetworkingManager.gameOpened)
			init();
		else
			roomListScreen.SetActive(true);
	}

	// Displays the main screen and makes sure the loading screen is gone
	void init(){
		splashScreen.SetActive(true);
		background.SetActive(false);
		NetworkingManager.gameOpened = true;
	}

	// Updates the list of rooms that can be joined
	public void UpdateRoomList(List<RoomInfo> roomList){
		int i = 0; // Counter representing how many rooms have been displayed
		// For each room in the list
		foreach(var info in roomList){
			// If we have displayed more than 4 rooms... then stop // TODO: this limit needs to be removed
			if(i >= 4) break;
			// If the room is closed, invisible, or removed from the list then don't display it
			if (!info.IsOpen || !info.IsVisible || info.RemovedFromList) {
				roomLabels[i].text = "";
				continue;
			}

			// Create a new room listing
			GameObject listing = Instantiate(roomListingPrefab);
			listing.transform.parent = roomListContent.transform;

			// Save a reference to it's label
			roomLabels.Add(listing.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>());
			Button button = listing.transform.GetChild(1).GetComponent<Button>();
			TMPro.TextMeshProUGUI buttonText = button.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshProUGUI>();

			// Update the room buttons
			roomLabels[i].text = info.Name;
			buttonText.text =info.Name;
			Debug.Log("Room " + i.ToString() + ": " + info.Name);
			i++;
		}
	}

	// Updates internal state when we join a room
	void joinedRoom(){
		// Switch screens
		
		roomListScreen.SetActive(false);
		inRoomScreen.SetActive(true);

		// This particular screen shouldn't be appearing at all, but it does.
		// This line patches it.
		splashScreen.SetActive(false);

		// If we are the host, become the whitehat primary player
		if (NetworkingManager.isHost){
			NetworkingManager.instance.BecomeWhiteHat();
			NetworkingManager.instance.BecomePrimaryPlayer();
			return;
		}

		// If we are not the host and the blackhat primary is taken... become a whitehat
		if(NetworkingManager.whiteHatPrimaryPlayer is null && NetworkingManager.blackHatPrimaryPlayer is object)
			NetworkingManager.instance.BecomeWhiteHat();
		// If the whitehat primary is taken... become the blackhat
		else if(NetworkingManager.whiteHatPrimaryPlayer is object && NetworkingManager.blackHatPrimaryPlayer is null)
			NetworkingManager.instance.BecomeBlackHat();
		// If both the black and whitehat primaries are taken... become an advisor
		else if(NetworkingManager.whiteHatPrimaryPlayer is object && NetworkingManager.blackHatPrimaryPlayer is object)
			NetworkingManager.instance.BecomeAdvisor();
	}

	// When we become the room's host, mark ourselves as ready
	void becomeRoomHost(){
		Debug.Log("Became Host!");

		// The host is always ready
		if(!NetworkingManager.isSingleplayer)
			PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable(){ // Networking Manager player may be null
				{NetworkingManager.IS_PLAYER_READY, true}
			});
	}

	// When a player's ready state changes, update the interactivity of the dropdowns and update the start button
	void roomPlayerPropertiesChanged(Player _1, ExitGames.Client.Photon.Hashtable _2){
		// If we are ready (and not the host)... make our dropdowns uninteractable
		if(NetworkingManager.localPlayer.isReady && !NetworkingManager.isHost){
			sideDropdown.interactable = false;
			roleDropdown.interactable = false;
		// Otherwise... make our dropdowns interactable
		} else {
			sideDropdown.interactable = true;
			roleDropdown.interactable = true;
		}

		// Make sure the start button state is synced
		roomStateChanged();
	}

	// Update the room state
	void roomStateChanged(){
		// Get references to both side's advisors
		var whiteHatAdvisors = NetworkingManager.whiteHatAdvisors;
		var blackHatAdvisors = NetworkingManager.blackHatAdvisors;

		// Get references to both side's primary players
		var whiteHatPrimaryPlayer = NetworkingManager.whiteHatPrimaryPlayer;
		var blackHatPrimaryPlayer = NetworkingManager.blackHatPrimaryPlayer;

		// If there are whitehat advisors...
		if (whiteHatAdvisors.Length > 0) {
			// Display the whitehat primary player with a spacer
			playerLabels[0].text = "White: " + (whiteHatPrimaryPlayer is null ? "Selecting..." : whiteHatPrimaryPlayer.nickname) + " - ";
			// Display the name of the first whitehat advisor
			advisorLabels[0].text = whiteHatAdvisors[0].nickname;
			Debug.Log(whiteHatAdvisors);
			// Display the rest of the whitehat advisors with commas separating them
			for (int i = 1; i < whiteHatAdvisors.Length; i++)
				advisorLabels[0].text += ", " + whiteHatAdvisors[i].nickname;
			// If there is a whitehat primary player (but no advisors)... display their name
		} else if (whiteHatPrimaryPlayer is object) {
			playerLabels[0].text = "White: " + (whiteHatPrimaryPlayer is null ? "Joining..." : whiteHatPrimaryPlayer.nickname);
			advisorLabels[0].text = "- No Advisor -";
		}
		// Otherwise display that the whitehat side is joining, or an AI in singleplayer
		else {
			playerLabels[0].text = "White: " + (!NetworkingManager.isSingleplayer ? "Joining..." : "AI");
			advisorLabels[0].text = "- No Advisor -";
		}

		// If there are blackhat advisors...
		if (blackHatAdvisors.Length > 0) {
			// Display the black primary player with a spacer
			playerLabels[1].text = "Black: " + (blackHatPrimaryPlayer is null ? "Selecting..." : blackHatPrimaryPlayer.nickname) + " - ";
			// Display the name of the first blackhat advisor
			advisorLabels[1].text = blackHatAdvisors[0].nickname;
			// Display the rest of the blackhat advisors with commas separating them
			for (int i = 1; i < blackHatAdvisors.Length; i++)
				advisorLabels[1].text += ", " + blackHatAdvisors[i].nickname;
			// If there is a blackhat primary player (but no advisors)... display their name
		} else if (blackHatPrimaryPlayer is object) {
			playerLabels[1].text = "Black: " + (blackHatPrimaryPlayer is null ? "Joining..." : blackHatPrimaryPlayer.nickname);
			advisorLabels[1].text = "- No Advisor -";
		}
		// Otherwise display that the blachat side is joining, or an AI in singleplayer
		else {
			playerLabels[1].text = "Black: " + (!NetworkingManager.isSingleplayer ? "Joining..." : "AI");
			advisorLabels[1].text = "- No Advisor -";
		}


			// Clear the lists of disabled dropdown indices
			sideDropdownController.indicesToDisable = new List<int>();
		roleDropdownController.indicesToDisable = new List<int>();

		// If there is already a primary player on our side... disable selecting the primary player role
		try{
			if(NetworkingManager.localPlayer.side == Networking.Player.Side.WhiteHat){
				if(NetworkingManager.whiteHatPrimaryPlayer is object)
					roleDropdownController.indicesToDisable.Add(0);
			} else if(NetworkingManager.localPlayer.side == Networking.Player.Side.BlackHat)
				if(NetworkingManager.blackHatPrimaryPlayer is object)
					roleDropdownController.indicesToDisable.Add(0);
		} catch(System.NullReferenceException) {
			// Disable primary if an error occurs
			roleDropdownController.indicesToDisable.Add(0);
		}

		try{
			// If we are a black or whitehat... disable becoming a Observer
			if(NetworkingManager.localPlayer.side != Networking.Player.Side.Common)
				roleDropdownController.indicesToDisable.Add(2);
			// If we are a common Observer... disable becoming a primary player or advisor
			else {
				roleDropdownController.indicesToDisable.Add(0);
				roleDropdownController.indicesToDisable.Add(1);
			}
		} catch(System.NullReferenceException) {
			// Disable becoming a primary player or advisor if an error occurs
			roleDropdownController.indicesToDisable.Add(0);
			roleDropdownController.indicesToDisable.Add(1);
		}

		// Update the currently selected dropdown item
		try{
			sideDropdownController.SetValueWithoutTriggeringEvents((int) NetworkingManager.localPlayer.side);
			roleDropdownController.SetValueWithoutTriggeringEvents((int) NetworkingManager.localPlayer.role);
		} catch(System.NullReferenceException) {
			Debug.LogWarning("Failed to update dropdowns... local player not found");
		}

		// TODO: Disable joining a side if there are already too many players on that side.

		// Make sure the start button state is synced
		updateStartButton();
	}

	// Makes sure state is synced when another player joins or leaves
	void playerJoinedOrLeft(Player _){ roomStateChanged(); }


	// -- UI Callbacks --


	// Function called whenever the create room button is pressed, it updates the player's name and creates a room
	public void OnCreateRoomButtonPressed(){
		//updatePlayerAlias();

		if (multiplayerToggle.isOn)
			NetworkingManager.instance.CreateRoom(/*max players*/ 16, true);
		else
			NetworkingManager.instance.CreateOfflineRoom();
	}

	// Function called when one of the join room buttons is pressed (it joins the specified room)
	public void OnJoinRoomButtonPressed(int index){
		string name = joinButtons[index].transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshProUGUI>().text;
		Debug.Log("Joining room: " + name);
		NetworkingManager.instance.JoinRoom(name);
	}

	// Function called when the player enters an alias and enters the game select screen
	public void OnJoinGameSelectButtonPressed() {
		updatePlayerAlias();
		nameEntryScreen.SetActive(false);
		roomListScreen.SetActive(true);
	}

	// Function called when the side dropdown's value is changed
	public void OnSideDropdownStateChanged(int side){
		switch((Networking.Player.Side) side){
			case Networking.Player.Side.WhiteHat:
				NetworkingManager.instance.BecomeWhiteHat();
				break;
			case Networking.Player.Side.BlackHat:
				NetworkingManager.instance.BecomeBlackHat();
				break;
			case Networking.Player.Side.Common:
				NetworkingManager.instance.BecomeObserver();
				// Make the role automatically be Observer
				roleDropdownController.SetValueWithoutTriggeringEvents(2);
				break;
		}
	}

	// Function called when the role dropdown's value is changed
	public void OnRoleDropdownStateChanged(int role){
		switch((Networking.Player.Role) role){
			case Networking.Player.Role.Player:
				NetworkingManager.instance.BecomePrimaryPlayer();
				break;
			case Networking.Player.Role.Advisor:
				NetworkingManager.instance.BecomeAdvisor();
				break;
			case Networking.Player.Role.Observer:
				NetworkingManager.instance.BecomeObserver();
				// Make the side automatically be Observer
				sideDropdownController.SetValueWithoutTriggeringEvents(2);
				break;
		}
	}

	// Function called when the start game button is pressed (if we aren't the multiplayer host this is the ready/unready button)
	public void OnStartGameButtonPressed(){
		// If we aren't the host then toggle our ready state
		if(!NetworkingManager.isHost){
			NetworkingManager.instance.toggleReady();
			updateStartButton();
			return;
		}

		// Close the current room so that more players can't join
		 PhotonNetwork.CurrentRoom.IsOpen = false;
		// Load the gameplay scene
		SceneManager.LoadScene(1);
	}

	// Function called when the leave room button is pressed
	public void OnLeaveRoomButtonPressed(){
		//titlebar.SetWindowTitle("Multiplayer");

		if(isMultiplayer)
			NetworkingManager.instance.LeaveRoom();
		else
			NetworkingManager.instance.Reconnect();
	}

	// Function called wwhen the splash screen entry button is pressed
	public void OnGameEntered() {
		splashScreen.SetActive(false);
		nameEntryScreen.SetActive(true);
		background.SetActive(true);
	}


	// -- Helpers --


	// Utility function which updates the player's name on the network
	void updatePlayerAlias(){
		string playerAlias = aliasTextbox.text;
		if(string.IsNullOrEmpty(playerAlias)) playerAlias = "You";
		PhotonNetwork.LocalPlayer.NickName = playerAlias; // Update our nickname in photon (networking manager version may be nullified)
	}

	// Helper function which updates the start button's text and intractability to reflect the current state of the lobby
	void updateStartButton(){
		// Get a reference to the start button's text
		TMPro.TextMeshProUGUI buttonText = startButton.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshProUGUI>();

		// If we aren't the host defer to the host to press the start button (the button becomes a ready button)
		try{
			if(!NetworkingManager.isHost && !NetworkingManager.localPlayer.isReady){
				startButton.interactable = true;
				buttonText.text = "Ready Up!";
				return;
			} else if(!NetworkingManager.isHost){
				startButton.interactable = true;
				buttonText.text = "Unready";
				return;
			}
		} catch (System.NullReferenceException) {
			startButton.interactable = true;
			buttonText.text = "Unready";
			if(!NetworkingManager.isHost) return;
		}

		// If we are in singleplayer mode... we can start
		if(NetworkingManager.isSingleplayer) {
			startButton.interactable = true;
			buttonText.text = "Start Singleplayer";
		// If all players aren't ready... Then we are waiting for someone to ready upp
		} else if(!NetworkingManager.instance.allPlayersReady()){
			startButton.interactable = false;
			buttonText.text = "Waiting for Ready...";
		// If one of the sides doesn't have a primary player... then we are waiting for their primary player
		} else if(NetworkingManager.whiteHatPrimaryPlayer is null || NetworkingManager.blackHatPrimaryPlayer is null){
			startButton.interactable = false;
			buttonText.text = "Waiting for Players...";
		// Otherwise, if all players are ready... we can start
		} else if(NetworkingManager.instance.allPlayersReady()) {
			startButton.interactable = true;
			buttonText.text = "Start";
		// Otherwise... we are waiting for players to join
		} else {
			startButton.interactable = false;
			buttonText.text = "Waiting for Players...";
		}
	}
}
