using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun;

public class NewLobbyMgr : MonoBehaviour
{
    public static NewLobbyMgr inst;
    public void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        State = LobbyState.StartOrQuit;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public enum LobbyState
    {
        StartOrQuit = 0,
        EnterAlias,
        CreateOrJoin,
        WaitingForPlayers,
        Play,
        None
    }

    public LobbyState _state;
    public LobbyState State
    {
        get { return _state; }
        set
        {
            _state = value;
            StartPanel.isVisible = (_state == LobbyState.StartOrQuit);
            EnterAliasPanel.isVisible = (_state == LobbyState.EnterAlias);
            CreateOrJoinGamePanel.isVisible = (_state == LobbyState.CreateOrJoin);
            WaitingForPlayersPanel.isVisible = (_state == LobbyState.WaitingForPlayers);
        }
    }
    public TaiserPanel StartPanel;
    public TaiserPanel EnterAliasPanel;
    public TaiserPanel CreateOrJoinGamePanel;
    public TaiserPanel WaitingForPlayersPanel;


    public void OnStartButton()
    {
        State = LobbyState.EnterAlias;
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    public InputField AliasInputFieldText;
    public Text PlayerNameText;
    public Text GameNamePlaceholderText;

    public string GameName;
    public string PlayerName;

    public void OnJoinButton()
    {
        PlayerName = AliasInputFieldText.text.Trim();
        PlayerNameText.text = PlayerName;
        PhotonNetwork.LocalPlayer.NickName = PlayerName;

        GameName = PlayerName + "_Taiser";
        GameNamePlaceholderText.text = GameName;
        //NetworkingManager.instance.JoinLobby();

        //if(!NetworkingManager.gameOpened)
        //    NetworkingManager.gameOpened = true;

        Debug.Log("Joined Lobbby with name: " + PhotonNetwork.LocalPlayer.NickName);

        State = LobbyState.CreateOrJoin;
        //...
    }




    public RectTransform Team1Panel; //Set both enclosing panels in editor 
    public RectTransform Team2Panel; // ... and this panel ...
    public List<string> dropdownValues; // Then set this 
    // and then finally run Menu item below to initialize waiting for players labels and dropdowns
    // Just do this once, beats using editor to connect them all up
    [ContextMenu("FindConnectInitLabelDropdowns")] 
    public void FindConnectInitLabelDropdowns (){
        ConnectTeam(Team1Panel, Team1PlayerNamesList, Team1PlayerRolesList);
        ConnectTeam(Team2Panel, Team2PlayerNamesList, Team2PlayerRolesList);
    }

    public List<Text> Team1PlayerNamesList = new List<Text>();
    public List<Dropdown> Team1PlayerRolesList = new List<Dropdown>();

    public List<Text> Team2PlayerNamesList = new List<Text>();
    public List<Dropdown> Team2PlayerRolesList = new List<Dropdown>();

    public void ConnectTeam(RectTransform panel, List<Text> players, List<Dropdown> teamDropdowns)
    {
        players.Clear();
        foreach(Text t in panel.GetComponentsInChildren<Text>()) {
            if(t.gameObject.name == "TaiserText") {
                t.text = "";
                players.Add(t);
            }
        }
        teamDropdowns.Clear();
        foreach(Dropdown d in panel.GetComponentsInChildren<Dropdown>()) {
            d.ClearOptions();
            d.AddOptions(dropdownValues);
            teamDropdowns.Add(d);
        }
    }


    public InputField GameNameInputField;
    public Text WaitingForPlayersText;
    public int MaxPlayersPerRoom;
    // Function called whenever the create room button is pressed, it updates the player's name and creates a room
    public void OnCreateRoomButton()
    {
        //updatePlayerAlias();
        GameName =
            (GameNameInputField.text.Length == 0 ? GameName : GameNameInputField.text);
        //NetworkingManager.instance.CreateRoom(GameName, /*max players*/ 2, true);
        NewNetworkMgr.inst.CreateTaiserRoom(GameName, MaxPlayersPerRoom);

        //PlayButton.interactable = false;
        ResetPlayerNamesList();
        Team1PlayerNamesList[0].text = PlayerName;
        Team2PlayerNamesList[0].text = "AI";

        WaitingForPlayersText.text = "Game: " + GameName + ", waiting for players";
        State = LobbyState.WaitingForPlayers;
        InvalidateDropdownsExceptForMine();
        Invoke("MakeAIPlayerAndActivatePlayButton", 1);

    }

    public bool isAI;
    public void MakeAIPlayerAndActivatePlayButton()
    {
        string opponent;
        if(isAI)
            opponent = "AI";
        else
            opponent = "zorkster";

        int opponentRole = 1;

        Team2PlayerNamesList[0].text = opponent;
        Team2PlayerRolesList[0].value = opponentRole;
        Team2PlayerRolesList[0].RefreshShownValue();
        ValidatePlayButton();
    }

    public void UpdateRoom()
    {
        foreach (Player p in PhotonNetwork.CurrentRoom.Players.Values) {
            Debug.Log(p.ToStringFull());
        }

    }

    public void InvalidateDropdownsExceptForMine()
    {
        foreach(Dropdown dropdown in Team2PlayerRolesList) {
            dropdown.interactable = false;
        }

        foreach(Dropdown dropdown in Team1PlayerRolesList) {
            dropdown.interactable = false;
        }
        int index = Team1PlayerNamesList.FindIndex(t => t.text == PlayerName);
        Team1PlayerRolesList[index].interactable = true;
    }

    public void ResetPlayerNamesList()
    {
        foreach(Text txt in Team1PlayerNamesList) {
            txt.text = "";
        }
        foreach(Text txt in Team2PlayerNamesList) {
            txt.text = "";
        }
    }

    public Button PlayButton;
    public int MinNumberOfPlayers;
    public void ValidatePlayButton()
    {
        int count = 0;
        foreach(Text txt in Team1PlayerNamesList) {
            if(txt.text != "") count++;
        }
        if(count > MinNumberOfPlayers) PlayButton.interactable = true;
    }


    public void OnPlayButton()
    {
        State = LobbyState.Play;
        UnityEngine.SceneManagement.SceneManager.LoadScene(1); //GraphPrototype
        //...
    }


}
