using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun;

public class AIPlayer
{
    public string name;
    public NewLobbyMgr.PlayerRoles role;
}

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
        SetPriorStateMap();
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
        MissionObjective,
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
            MissionObjectivePanel.isVisible = (_state == LobbyState.MissionObjective);
        }
    }
    public TaiserPanel StartPanel;
    public TaiserPanel EnterAliasPanel;
    public TaiserPanel CreateOrJoinGamePanel;
    public TaiserPanel WaitingForPlayersPanel;
    public TaiserPanel MissionObjectivePanel;

    public void OnStartButton()
    {
        State = LobbyState.MissionObjective;
    }

    public void OnNextButton()
    {
        State = LobbyState.EnterAlias;
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    public void OnBackButton()
    {
        State = PriorStateMap[State];
    }
    public Dictionary<LobbyState, LobbyState> PriorStateMap = new Dictionary<LobbyState, LobbyState>();
    public void SetPriorStateMap()
    {
        PriorStateMap.Add(LobbyState.StartOrQuit, LobbyState.StartOrQuit);
        PriorStateMap.Add(LobbyState.MissionObjective, LobbyState.MissionObjective);
        PriorStateMap.Add(LobbyState.EnterAlias, LobbyState.StartOrQuit);
        PriorStateMap.Add(LobbyState.CreateOrJoin, LobbyState.EnterAlias);
        PriorStateMap.Add(LobbyState.WaitingForPlayers, LobbyState.CreateOrJoin);
    }


    public InputField AliasInputFieldText;
    public Text PlayerNameText;
    public Text GameNamePlaceholderText;

    public string GameName;
    public static string PlayerName;
    public static PlayerRoles PlayerRole;
    public RectTransform JoinGameSubPanel;

    public void OnJoinButton()
    {

        PlayerName = AliasInputFieldText.text.Trim();
        PlayerNameText.text = PlayerName;
        PlayerRole = PlayerRoles.Whitehat;

        GameName = PlayerName + "_Taiser";
        GameNamePlaceholderText.text = GameName;
        //NetworkingManager.instance.JoinLobby();

        //if(!NetworkingManager.gameOpened)
        //    NetworkingManager.gameOpened = true;


        if(!NewNetworkMgr.inst.doMultiplayer) {
            PhotonNetwork.LocalPlayer.NickName = PlayerName;
            JoinGameSubPanel.gameObject.SetActive(false);
        } else {
            Debug.Log("Joined Lobby with name: " + PhotonNetwork.LocalPlayer.NickName);
            UpdateRoomList();
        }
        
        State = LobbyState.CreateOrJoin;
        //...
    }

    //------------------------------------------------------------------
    public enum PlayerRoles
    {
        Whitehat = 0,
        Blackhat,
        Observer,
        None
    }

    public void SetNetworkPlayerRole(PlayerRoles role)
    {
        ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
        playerProps.Add("R", role.ToString());
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }
    //------------------------------------------------------------------

    public void JoinExistingRoomButtonClicked(int buttonId)
    {
        string roomName = RoomButtonsList[buttonId].GetComponentInChildren<Text>().text.Trim();
        Debug.Log(PhotonNetwork.NickName + " is joining " + roomName);
        SetNetworkPlayerRole(PlayerRoles.Blackhat);
        NewNetworkMgr.inst.JoinTaiserRoom(roomName);

        WaitForPlayers(roomName);
    }

    public void WaitForPlayers(string gameName)
    {
        GameName = gameName; //assigned twice if you are game creator
        WaitingForPlayersText.text = "Game: " + GameName + ", waiting for players";
        //InvalidateDropdownsExceptForMine();
        State = LobbyState.WaitingForPlayers;

    }

    public List<RoomInfo> CachedRoomList = new List<RoomInfo>();
    public List<string> CachedRoomNameList = new List<string>();
    public List<Button> RoomButtonsList = new List<Button>();
    public void UpdateRoomList()
    {
        CachedRoomNameList.Clear();
        int i = 0;
        foreach (RoomInfo ri in CachedRoomList) {
            if(ri.Name != GameName && i < 4) {//Can only do 4 buttons/rooms/games
                RoomButtonsList[i].interactable = true;
                RoomButtonsList[i].GetComponentInChildren<Text>().text = ri.Name;
                i++;
                CachedRoomNameList.Add(ri.Name);
               
            }
        }
    }

    public RectTransform GameButtonsPanel;
    [ContextMenu("FindConnectGameButtons")]
    public void FindConnectGameButtons()
    {
        RoomButtonsList.Clear();
        foreach(Button b in GameButtonsPanel.GetComponentsInChildren<Button>()) {
            RoomButtonsList.Add(b);
            b.interactable = false;
            
        }
    }

    //--------------------------------------------------------------------------
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

    public List<AIPlayer> AIPlayers = new List<AIPlayer>();

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
    //--------------------------------------------------------------------------

    public InputField GameNameInputField;
    public Text WaitingForPlayersText;
    public int MaxPlayersPerRoom;
    public bool isRoomCreator = false;
    // Function called whenever the create room button is pressed, it updates the player's name and creates a room
    public void OnCreateRoomButton()
    {
        //updatePlayerAlias();
        GameName =
            (GameNameInputField.text.Length == 0 ? GameName : GameNameInputField.text);
        //NetworkingManager.instance.CreateRoom(GameName, /*max players*/ 2, true);
        if(NewNetworkMgr.inst.doMultiplayer) {
            NewNetworkMgr.inst.CreateTaiserRoom(GameName, MaxPlayersPerRoom);
            isRoomCreator = true;
            SetNetworkPlayerRole(PlayerRoles.Whitehat);
        } 

        if(isAI)
            Invoke("MakeAIPlayerAndActivatePlayButton", 1);
        else
            Invoke("MakeHumanPlayerAndActivatePlayButton", 1);

        WaitForPlayers(GameName);

    }

    //-----------------------------------------------------------
    public bool isAI;
    public void MakeAIPlayerAndActivatePlayButton()
    {
        string opponent = "AI";
        PlayerRoles opponentRole = PlayerRoles.Blackhat; // fixed for now
        AIPlayer aip = new AIPlayer();
        aip.name = opponent;
        aip.role = opponentRole;
        AIPlayers.Add(aip);
        SetWaitingForPlayersLists();
    }

    public void MakeHumanPlayerAndActivatePlayButton()
    {
        string opponent = "Alex";
        PlayerRoles opponentRole = PlayerRoles.Blackhat; // fixed for now
        AIPlayer aip = new AIPlayer(); //same as our pretend human alex
        aip.name = opponent;
        aip.role = opponentRole;
        AIPlayers.Add(aip);
        SetWaitingForPlayersLists();
    }



    public PlayerRoles GetRole(Player p)
    {
        object myRoleObject;
        bool status = p.CustomProperties.TryGetValue("R", out myRoleObject);
        PlayerRoles role = PlayerRoles.None;
        if(status) {
            string myRoleString = myRoleObject.ToString();
            switch (myRoleString) {
                case "Blackhat":
                    role = PlayerRoles.Blackhat;
                    break;
                case "Whitehat":
                    role = PlayerRoles.Whitehat;
                    break;
                case "Observer":
                    role = PlayerRoles.Observer;
                    break;
                default:
                    role = PlayerRoles.None;
                    break;
            }
        }
        return role;
    }

    public void SetWaitingForPlayersLists()
    {
        UninteractDropdowns();
        ResetPlayerNamesList();


        int index1 = 0;
        int index2 = 0;
        PlayerRoles myRole;

        if(NewNetworkMgr.inst.doMultiplayer) {
            myRole = GetRole(PhotonNetwork.LocalPlayer);
            Team1PlayerRolesList[index1].interactable = true;
            SetPlayerInfoDisplay(PlayerName, Team1PlayerNamesList, myRole, Team1PlayerRolesList, index1++);

            foreach(Player p in PhotonNetwork.CurrentRoom.Players.Values) {
                if(p.NickName != PhotonNetwork.LocalPlayer.NickName) {
                    PlayerRoles pRole = GetRole(p);
                    if(pRole == myRole)
                        SetPlayerInfoDisplay(p.NickName, Team1PlayerNamesList, pRole, Team1PlayerRolesList, index1++);
                    else
                        SetPlayerInfoDisplay(p.NickName, Team2PlayerNamesList, pRole, Team2PlayerRolesList, index2++);

                }
            }
        } else {//single player against )pretend-human)/ai opponent
            myRole = PlayerRole;
            SetPlayerInfoDisplay(PlayerName, Team1PlayerNamesList, myRole, Team1PlayerRolesList, index1++);
        }
        foreach(AIPlayer aip in AIPlayers) {
            if(aip.role == myRole)
                SetPlayerInfoDisplay(aip.name, Team1PlayerNamesList, aip.role, Team1PlayerRolesList, index1++);
            else
                SetPlayerInfoDisplay(aip.name, Team2PlayerNamesList, aip.role, Team2PlayerRolesList, index2++);
        }
        ValidatePlayButton();
    }

    public void SetPlayerInfoDisplay(string name, List<Text> names, PlayerRoles role, List<Dropdown> roles, int i)
    {
        names[i].text = name;
        RoleDropdownHandler rdh = roles[i].GetComponent<RoleDropdownHandler>();
        rdh.playerName = name;
        rdh.SetValueWithoutTrigger((int) role);
    }

    public void OnValueChangedInRoleDropdown(string playerName, PlayerRoles role, Dropdown dropdown, int index)
    {
        Debug.Log(playerName + " set role to " + role);
        SetNetworkPlayerRole(role);
        SetWaitingForPlayersLists();
    }


    //----------------------------------------------------------------------
    public void UninteractDropdowns()
    {
        foreach(Dropdown dropdown in Team2PlayerRolesList) {
            dropdown.interactable = false;
        }
        foreach(Dropdown dropdown in Team1PlayerRolesList) {
            dropdown.interactable = false;
        }
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
    //-------------------------------------------------------------------------
    public Button PlayButton;
    public int MinNumberOfPlayers;
    public void ValidatePlayButton()
    {
        int count;
        if(NewNetworkMgr.inst.doMultiplayer) {
            if(isAI)
                count = PhotonNetwork.CurrentRoom.PlayerCount + 1;
            else
                count = PhotonNetwork.CurrentRoom.PlayerCount;
            Debug.Log("ValidatePlayButton: NPlayers: " + count);

            if(count >= MinNumberOfPlayers && isRoomCreator)
                PlayButton.interactable = true;
        } else {
            PlayButton.interactable = true;
        }
        Debug.Log("Creating Game");
    }


    public void OnPlayButton()
    {
        State = LobbyState.Play;
        //UnityEngine.SceneManagement.SceneManager.LoadScene(1); //GraphPrototype
        InstrumentMgr.isDebug = false;
        PhotonNetwork.LoadLevel(1);
        //...
    }


}
