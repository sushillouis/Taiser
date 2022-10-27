using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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
        //State = LobbyState.Admin;
        SetPriorStateMap();
    }

    // Update is called once per frame
    void Update()
    {

    }
    [ContextMenu("TimeString")]
    public void TimeString()
    {
        string tmp = System.DateTime.Now.ToUniversalTime().ToString(); //System.DateTime.Now.ToLocalTime().ToString();

        string tmp2 = tmp.Replace("/", "_");
        tmp2 = tmp2.Replace(" ", "_");
        tmp2 = tmp2.Replace(":", "_");
        Debug.Log(tmp + ", " + tmp2);
    }

    public enum LobbyState
    {
        StartOrQuit = 0,
        EnterAlias,
        Admin,            // if Alias is Administrator
        CreateOrJoin,
        MissionObjective,
        ChooseDifficulty,
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
            AdminPanel.isVisible = (_state == LobbyState.Admin);
            EnterAliasPanel.isVisible = (_state == LobbyState.EnterAlias);
            CreateOrJoinGamePanel.isVisible = (_state == LobbyState.CreateOrJoin);
            WaitingForPlayersPanel.isVisible = (_state == LobbyState.WaitingForPlayers);
            MissionObjectivePanel.isVisible = (_state == LobbyState.MissionObjective);
            //ChooseGameDifficultyPanel.isVisible = (_state == LobbyState.ChooseDifficulty);
        }
    }
    public TaiserPanel AdminPanel;
    public TaiserPanel StartPanel;
    public TaiserPanel EnterAliasPanel;
    public TaiserPanel CreateOrJoinGamePanel;
    public TaiserPanel WaitingForPlayersPanel;
    public TaiserPanel MissionObjectivePanel;
    public TaiserPanel ChooseGameDifficultyPanel;
    public RectTransform TeammateChoiceDropdownPanel;//Just the teammate Species choice label and dropdown
    public Text TeammateChoiceGameDifficultySubtitleText;


    //Button handling =============================================================================
    public void OnPlayButton()
    {
        State = LobbyState.EnterAlias;
    }
    public void OnAdminStoreButton()
    {
        AdminMgr.inst.WriteParamsToServer();
        State = LobbyState.StartOrQuit;
    }

    public void OnNextButton()
    {
        State = LobbyState.MissionObjective;
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
        PriorStateMap.Add(LobbyState.EnterAlias, LobbyState.StartOrQuit);
        PriorStateMap.Add(LobbyState.CreateOrJoin, LobbyState.EnterAlias);
        PriorStateMap.Add(LobbyState.MissionObjective, LobbyState.CreateOrJoin);
        //PriorStateMap.Add(LobbyState.ChooseDifficulty, LobbyState.MissionObjective);
        PriorStateMap.Add(LobbyState.WaitingForPlayers, LobbyState.MissionObjective);
        PriorStateMap.Add(LobbyState.Play, LobbyState.WaitingForPlayers);
        //PriorStateMap.Add(LobbyState.Play, LobbyState.Play);
    }


    public InputField AliasInputFieldText;
    public Text PlayerNameText;
    public Text GameNamePlaceholderText;

    public string GameName;
    public PlayerRoles PlayerRole;
    public PlayerSpecies PlrSpecies;

    //-----------------------------------------------------------
    public static string PlayerName = "sjl";
    public string PlayerNameForDebug;
    public static TaiserPlayer thisPlayer;
    public static Difficulty gameDifficulty;
    public static PlayerSpecies teammateSpecies;
    public static string teammateName;

    //-----------------------------------------------------------
    public PlayerSpecies opponentSpecies;
    public PlayerSpecies publicTeammateSpecies;
    public Difficulty publicGameDifficulty;

    public RectTransform JoinGameSubPanel;

    public void OnJoinButton()
    {

        PlayerName = AliasInputFieldText.text.Trim();
        PlayerNameText.text = PlayerName;
        PlayerRole = PlayerRoles.Whitehat;
        PlrSpecies = PlayerSpecies.Human;

        thisPlayer = new TaiserPlayer(PlayerName, PlayerRole, PlrSpecies);
        gameDifficulty = Difficulty.Novice;
        teammateSpecies = PlayerSpecies.Human;

        GameName = PlayerName + "_Taiser";
        GameNamePlaceholderText.text = GameName;

        PlayerNameForDebug = PlayerName;

        if(PlayerName.Trim().ToLower().Contains("admin")) {
            State = LobbyState.Admin;
        } else {
            State = LobbyState.CreateOrJoin;
        }

    }


    public void OnCreateGameButton()
    {
        ShowBriefing();
    }

    // Function called whenever the create room button is pressed, it updates the player's name and creates a room
    public void CreateGameAndWaitForPlayers()
    {
        //updatePlayerAlias();
        GameName =
            (GameNameInputField.text.Length == 0 ? GameName : GameNameInputField.text);

        TaiserPlayerList.Clear();

        //Make me
        TaiserPlayerList.Add(thisPlayer);

        SetWaitingForPlayersLists();

        PlayButton.interactable = false;
        SpinnerPanel.gameObject.SetActive(true);

        switch(opponentSpecies) {
            case PlayerSpecies.AI:
                Invoke("MakeAIPlayerAndActivatePlayButton", 0.1f);
                break;
            case PlayerSpecies.Human:
                Invoke("MakeHumanPlayerAndActivatePlayButton", 1);
                break;
            case PlayerSpecies.Unknown:
                Invoke("MakeUnknownPlayerAndActivatePlayButton", 2);
                break;
            default:
                Invoke("MakeAIPlayerAndActivatePlayButton", 0.1f);
                break;
        }


        switch(teammateSpecies) {
            case PlayerSpecies.AI:
                int x = Random.Range(20, 99);
                teammateName = "CyberAI " + x.ToString("00");
                MakePlayerAndActivatePlayButton(teammateName, PlayerRoles.Whitehat, PlayerSpecies.AI, false);
                break;
            case PlayerSpecies.Human:
                teammateName = GeneratePlayerName();
                MakePlayerAndActivatePlayButton(teammateName, PlayerRoles.Whitehat, PlayerSpecies.Human, false);
                break;
            case PlayerSpecies.Unknown:
                MakePlayerAndActivatePlayButton("Unknown", PlayerRoles.Whitehat, PlayerSpecies.Unknown, false);
                break;
            default:
                break;

        }

        WaitForPlayers(GameName);

    }

    public TextTypingAnimation TTAnimator;

    public void ShowBriefing()
    {
        State = LobbyState.MissionObjective;
        TTAnimator.Animate = true;
    }

    public void OnBriefingDoneButton()
    {
        //State = LobbyState.ChooseDifficulty;
        State = LobbyState.WaitingForPlayers;
        TTAnimator.Animate = false;
        if(!ChooseOnce)
            FixTeammateSpeciesUIElements();
        CreateGameAndWaitForPlayers();
    }

    void FixTeammateSpeciesUIElements()
    {
        TeammateChoiceDropdownPanel.gameObject.SetActive(false);
        TeammateChoiceGameDifficultySubtitleText.text = "Novice, Intermediate, or Advanced?";
    }

    /// <summary>
    /// Deprecated
    /// </summary>
    public void OnChooseTeammateDoneButton()
    {
        State = LobbyState.WaitingForPlayers;
        CreateGameAndWaitForPlayers();
    }


    public void WaitForPlayers(string gameName)
    {
        GameName = gameName; //assigned twice if you are game creator
        WaitingForPlayersText.text = thisPlayer.name + ". Waiting for teammate.";
        //InvalidateDropdownsExceptForMine();
        PlayButton.interactable = false;
        SpinnerPanel.gameObject.SetActive(true);
        State = LobbyState.WaitingForPlayers;

    }

//    public List<string> CachedRoomNameList = new List<string>();
    public List<Button> RoomButtonsList = new List<Button>();

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

    // and then finally run Menu item below to initialize waiting for players labels and dropdowns
    // Just do this once, beats using editor to connect them all up
    [ContextMenu("FindConnectInitLabelDropdowns")] 
    public void FindConnectInitLabelDropdowns (){
        ConnectTeam(Team1Panel, Team1PlayerNamesList, Team1PlayerRolesList, Team1PlayerSpeciesList);
        ConnectTeam(Team2Panel, Team2PlayerNamesList, Team2PlayerRolesList, Team2PlayerSpeciesList);
    }

    public List<Text> Team1PlayerNamesList = new List<Text>();
    public List<Dropdown> Team1PlayerRolesList = new List<Dropdown>();
    public List<Dropdown> Team1PlayerSpeciesList = new List<Dropdown>();

    public List<Text> Team2PlayerNamesList = new List<Text>();
    public List<Dropdown> Team2PlayerRolesList = new List<Dropdown>();
    public List<Dropdown> Team2PlayerSpeciesList = new List<Dropdown>();

    public List<TaiserPlayer> TaiserPlayerList = new List<TaiserPlayer>();
    //public List<TaiserPlayer> TeammatePlayers = new List<TaiserPlayer>();

    public void ConnectTeam(RectTransform panel, List<Text> players, List<Dropdown> teamDropdowns, 
        List<Dropdown> speciesDropdowns)
    {
        players.Clear();
        foreach(Text t in panel.GetComponentsInChildren<Text>()) {
            if(t.gameObject.name == "TaiserText") {
                t.text = "";
                players.Add(t);
            }
        }

        //Debug.Log("Roles: " + System.Enum.GetNames(typeof(PlayerRoles)).ToList<string>());
        teamDropdowns.Clear();
        foreach(RoleDropdownHandler rdh in panel.GetComponentsInChildren<RoleDropdownHandler>()) { 
            Dropdown d = rdh.GetComponentInParent<Dropdown>();
            d.ClearOptions();
            d.AddOptions(System.Enum.GetNames(typeof(PlayerRoles)).ToList<string>());// dropdownValues);
            teamDropdowns.Add(d);
        }

        speciesDropdowns.Clear();
        foreach(SpeciesDropdownHandler sdh in panel.GetComponentsInChildren<SpeciesDropdownHandler>()) {
            Dropdown d = sdh.GetComponentInParent<Dropdown>();
            d.ClearOptions();
            d.AddOptions(System.Enum.GetNames(typeof(PlayerSpecies)).ToList<string>());// dropdownValues);
            speciesDropdowns.Add(d);
        }


    }
    //--------------------------------------------------------------------------

    public InputField GameNameInputField;
    public Text WaitingForPlayersText;


    public void MakePlayerAndActivatePlayButton(string name, PlayerRoles role, PlayerSpecies species, bool isOpponent)
    {
        TaiserPlayer player = new TaiserPlayer(name, role, species);
        TaiserPlayerList.Add(player);
        Invoke("SetWaitingForPlayersLists", 5.0f);//this does not work right because SetWaitingForPlayerLists will add all current players
        //the first time it is called though an Invoke or otherwise. So opponent's Invoke is the limiting factor 
        //SetWaitingForPlayersLists();
    }

    public void MakeAIPlayerAndActivatePlayButton()
    {
        TaiserPlayer aip = new TaiserPlayer("AI", PlayerRoles.Blackhat, PlayerSpecies.AI);
        TaiserPlayerList.Add(aip);
        SetWaitingForPlayersLists();
    }

    public void MakeHumanPlayerAndActivatePlayButton()
    {
        TaiserPlayer aip = new TaiserPlayer(name, PlayerRoles.Blackhat, PlayerSpecies.Human); //same as our pretend human alex
        TaiserPlayerList.Add(aip);
        SetWaitingForPlayersLists();
    }


    public void MakeUnknownPlayerAndActivatePlayButton()
    {
        TaiserPlayer aip = new TaiserPlayer("Unknown " + Random.Range(0,100).ToString("00"), 
            PlayerRoles.Blackhat, PlayerSpecies.Unknown); 
        TaiserPlayerList.Add(aip);
        SetWaitingForPlayersLists();
    }

    public List<string> HumanNames = new List<string>();
    string GeneratePlayerName()
    {
        int choice = Random.Range(0, HumanNames.Count);
        //Debug.Log("Choice: " + choice + ", name: " + HumanNames[choice]);
        return HumanNames[choice];
    }

 

    public void SetWaitingForPlayersLists()
    {
        UninteractDropdowns();
        ResetPlayerNamesList();

        int index1 = 0;
        int index2 = 0;


        foreach(TaiserPlayer tp in TaiserPlayerList) {
            if(tp.role == thisPlayer.role)
                SetPlayerInfoDisplay(tp.name, Team1PlayerNamesList, tp.role, Team1PlayerRolesList, 
                    tp.species, Team1PlayerSpeciesList, index1++);
            else
                SetPlayerInfoDisplay(tp.name, Team2PlayerNamesList, tp.role, Team2PlayerRolesList, 
                    tp.species, Team2PlayerSpeciesList, index2++);
        }


        ValidatePlayButton();

    }

    public RectTransform SpinnerPanel;
    public void StopSpinner()
    {
        SpinnerPanel.gameObject.SetActive(false);
    }

    public void SetPlayerInfoDisplay(string name, List<Text> names, PlayerRoles role, List<Dropdown> rolesDropdowns, 
        PlayerSpecies species, List<Dropdown> speciesDropdowns, int i)
    {
        names[i].text = name;
        RoleDropdownHandler rdh = rolesDropdowns[i].GetComponent<RoleDropdownHandler>();
        rdh.playerName = name;
        rdh.SetValueWithoutTrigger((int) role);
        SpeciesDropdownHandler sdh = speciesDropdowns[i].GetComponent<SpeciesDropdownHandler>();
        sdh.playerName = name;
        sdh.SetValueWithoutTrigger((int) species);

    }

    public void OnValueChangedInRoleDropdown(string playerName, PlayerRoles role, Dropdown dropdown, int index)
    {
        Debug.Log(playerName + " set this player's role to " + role);
        //SetNetworkPlayerRole(role);
        SetWaitingForPlayersLists();
    }

    public void OnValueChangedInSpeciesDropdown(string playerName, PlayerSpecies species, Dropdown dropdown, int index)
    {
        Debug.Log(playerName + " set this player's species to " + species);
        //        SetNetworkPlayerRole(role);
        SetWaitingForPlayersLists();
    }

    public void OnValueChangedInTeammateSpeciesChoiceDropdown(string playerName, PlayerSpecies species, Dropdown dropdown, int index)
    {
        Debug.Log(playerName + " set teammate species to " + species);
        //        SetNetworkPlayerRole(role);
        teammateSpecies = species;
        publicTeammateSpecies = species;
        SetWaitingForPlayersLists();
    }





    //----------------------------------------------------------------------
    public void UninteractDropdowns()
    {
        foreach(Dropdown dropdown in Team2PlayerRolesList) {
            dropdown.GetComponent<RoleDropdownHandler>().SetValueWithoutTrigger((int) PlayerRoles.Blackhat);
            dropdown.interactable = false;
        }
        foreach(Dropdown dropdown in Team1PlayerRolesList) {
            dropdown.interactable = false;
            dropdown.GetComponent<RoleDropdownHandler>().SetValueWithoutTrigger((int) thisPlayer.role);
        }
        //Species
        foreach(Dropdown dropdown in Team2PlayerSpeciesList) {
            dropdown.interactable = false;
            dropdown.GetComponent<SpeciesDropdownHandler>().SetValueWithoutTrigger((int) PlayerSpecies.Unknown);
        }
        foreach(Dropdown dropdown in Team1PlayerSpeciesList) {
            dropdown.interactable = false;
            dropdown.GetComponent<SpeciesDropdownHandler>().SetValueWithoutTrigger((int) PlayerSpecies.Unknown);
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
    [Header("---------------")]
    public static bool ChooseOnce = false;
    [Header("---------------")]

    public Button PlayButton;
    public int MinNumberOfPlayers;
    public void ValidatePlayButton()
    {
 
        PlayButton.interactable = true;
        SpinnerPanel.gameObject.SetActive(false);
        WaitingForPlayersText.text = PlayerName + " found teammate: " + teammateName;
        //
    }


    public void OnStartButton()
    {
        Debug.Log("Creating Game");
        State = LobbyState.Play;
        InstrumentMgr.isDebug = false;
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(1);
    }


}
