using JetBrains.Annotations;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static NewGameMgr;

public class RuleSpecButtonMgr : MonoBehaviour
{
    public static RuleSpecButtonMgr inst;
    public void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        if(!isRandomSeedInitialized) {
            Debug.Log("Initializing Advice Randomizers");
            isRandomSeedInitialized = true;
            AIDecisionRandomizer = new System.Random(AIRandomizerSeed);
            HumanDecisionRandomizer = new System.Random(HumanRandomizerSeed);
        }
        ClearPacketInformation(ClickedPacketRuleTextList);
        ClearPacketInformation(AdvisorPacketRuleTextList);
    }


    // Update is called once per frame
    void Update()
    {

    }

    [System.Serializable]
    public class ButtonList
    {
        public List<Button> buttons = new List<Button>();
    }

    [Header("Other public variables")]
    public GameObject ButtonsRoot;
    public List<ButtonList> RuleButtons2d = new List<ButtonList>();
    public int nrows = 3;
    public int ncols = 3;

    public Button AcceptAdviceButton;

    [ContextMenu("SetupButtons")]
    public void SetupButtons()
    {
        RuleButtons2d.Clear();
        for(int i = 0; i < nrows; i++) {
            RuleButtons2d.Add(new ButtonList());
        }
        int row = 0;
        int index = 0;
        foreach(Button b in ButtonsRoot.GetComponentsInChildren<Button>()) {
            string bt = b.GetComponentInChildren<Text>().text.ToLower();
            if(bt != "Size".ToLower() && bt != "Color".ToLower() && bt != "Shape".ToLower()) {
                row = index / nrows;
                RuleButtons2d[row].buttons.Add(b);
                index++;
            }
        }
    }

    public TDestination CurrentDestination;//Set by NewGameMgr OnAttackableDestinationClicked
    public LightWeightPacket PlayerRuleSpec = new LightWeightPacket();
    public LightWeightPacket AdvisorRuleSpec = new LightWeightPacket();
    public LightWeightPacket ClickedPacketRuleSpec = new LightWeightPacket();

    public void OnSizeClick(int size)
    {
        PlayerRuleSpec.size = (PacketSize) size;
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.RuleSpec.ToString(), PlayerRuleSpec.size.ToString());
    }
    public void OnColorClick(int color)
    {
        PlayerRuleSpec.color = (PacketColor) color;
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.RuleSpec.ToString(), PlayerRuleSpec.color.ToString());
    }
    public void OnShapeClick(int shape)
    {
        PlayerRuleSpec.shape = (PacketShape) shape;
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.RuleSpec.ToString(), PlayerRuleSpec.shape.ToString());
    }


    //-------------------------------------------------------------------------------------
    // Random number generators, seeds, and utility functions for advice generation
    //-------------------------------------------------------------------------------------


    public float HumanCorrectAdviceProbability = 0.8f;
    public float AICorrectAdviceProbability = 0.8f;
    public int AIRandomizerSeed = 4321;
    public int HumanRandomizerSeed = 6789;
    public System.Random AIDecisionRandomizer;
    public System.Random HumanDecisionRandomizer;

    public static bool isRandomSeedInitialized = false;

    /// <summary>
    /// Like Flip but takes a System.Random as first param
    /// </summary>
    /// <param name="Randomizer"></param>
    /// <param name="prob"> Probability of returning true </param> 
    /// <returns></returns>
    public bool AdviceRandomizerFlip(System.Random Randomizer, float prob)
    {
        return (Randomizer.NextDouble() < prob);
    }

    public float RandomRange(System.Random Randomizer, float min, float max)
    {
        return min + (float) Randomizer.NextDouble() * (max - min);
    }

    public Text FilterRuleSpecTitle;

    //-------------------------------------------------------------------------------------
    //--- For when a destination in the examining panel is clicked

    public RectTransform MeRulePanel;
    public RectTransform AdvicePanel;
    /*
    public void OnAttackableDestinationClicked(TDestination destination)
    {
        NewGameMgr.inst.State = GameState.ChooseAdvisorOrMe;
        NewGameMgr.inst.SetButtonNamesAndState();
        CurrentDestination = destination;
        CurrentDestination.isBeingExamined = true;
    }
    */
    public void DoPacketExamining(AdvisingState AIHumanOrMe) 
    {
        //InstrumentMgr.inst.AddRecord(TaiserEventTypes.MaliciousDestinationClicked, CurrentDestination.inGameName);
        advisingState = AIHumanOrMe;
        CurrentDestination.isBeingExamined = true;
        FilterRuleSpecTitle.text = CurrentDestination.inGameName;
        PacketButtonMgr.inst.SetupPacketButtonsForInspection(CurrentDestination); // Setup packet buttons on the top panel
        AcceptAdviceButton.interactable = false;
        //State = AdvisingState.Undecided;
        switch(advisingState) {
            case AdvisingState.Me:
                AskMe();
                break;
            case AdvisingState.Human:
                AskForHumanAdvice();
                break;
            case AdvisingState.AI:
                AskForAIAdvice();
                break;
            default:
                AskForAIAdvice();
                break;
        }

        /*
        if(NewLobbyMgr.ChooseOnce)
            DoChooseOnce();
        else
            DoChooseEveryTime();
        */
        ClearPacketInformation(ClickedPacketRuleTextList);
        ClearPacketInformation(AdvisorPacketRuleTextList);


    }

    //-------------------------------------------------------------------------------------
    /*
    public void DoChooseOnce()
    {
        AdviceSpeciesButtonPanel.gameObject.SetActive(false);
        if(NewLobbyMgr.teammateSpecies == PlayerSpecies.Human)
            AskForHumanAdvice();
        else
            AskForAIAdvice();
            
    }

    public void DoChooseEveryTime()
    {
        AdviceSpeciesButtonPanel.gameObject.SetActive(true);
        State = AdvisingState.Undecided;

    }
    */
    //-------------------------------------------------------------------------------------
    //--- For When a packet button in the examining panel is clicked

    public List<int> FontSizes = new List<int>();
    public List<Color> TextColors = new List<Color>();

    public List<Text> ClickedPacketRuleTextList = new List<Text>();
    public List<Text> AdvisorPacketRuleTextList = new List<Text>();

    public GameObject PacketRuleTextListRoot;
    public GameObject AdvisorRuleTextListRoot;
    [ContextMenu("SetupRuleTextLists")]
    public void SetupRuleTextLists()
    {
        ClickedPacketRuleTextList.Clear();
        foreach(Text t in PacketRuleTextListRoot.GetComponentsInChildren<Text>()) {
            ClickedPacketRuleTextList.Add(t);
        }
        AdvisorPacketRuleTextList.Clear();
        foreach(Text t in AdvisorRuleTextListRoot.GetComponentsInChildren<Text>()) {
            AdvisorPacketRuleTextList.Add(t);
        }
    }
    public void OnPacketClicked(LightWeightPacket packet)
    {
        ClickedPacketRuleSpec = packet;
        DisplayPacketInformation(packet, ClickedPacketRuleTextList); // expand on this
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.PacketInspect.ToString(), packet.ToString());
    }

    public void DisplayPacketInformation(LightWeightPacket packet, List<Text> RuleTextList)
    {
        RuleTextList[0].text = packet.size.ToString();
        RuleTextList[0].fontSize = FontSizes[(int) packet.size];
        RuleTextList[1].text = packet.color.ToString();
        RuleTextList[1].color = TextColors[(int) packet.color];
        RuleTextList[2].text = packet.shape.ToString();
    }

    public void ClearPacketInformation(List<Text> RuleTextList)
    {
        RuleTextList[0].text = "";
        RuleTextList[1].text = "";
        RuleTextList[2].text = "";

    }


    /// <summary>
    /// Called from TaiserFilterRuleSpecPanel from SetFirewall button
    /// </summary>
    public void ApplyCurrentUserRule()
    {
        AcceptAdviceButton.interactable = false;
        //AdviceSpeciesButtonPanel.gameObject.SetActive(true);
        NewGameMgr.inst.ApplyFirewallRule(CurrentDestination, PlayerRuleSpec, false);

    }

    /// <summary>
    /// Not used currently. Available for future use. 
    /// Called from TaiserFilterRuleSpecPanel from SetPacketRuleFirewall button
    /// </summary>
    public void ApplyClickedPacketRule()
    {
        AcceptAdviceButton.interactable = false;
        //AdviceSpeciesButtonPanel.gameObject.SetActive(true);

        NewGameMgr.inst.ApplyFirewallRule(CurrentDestination, ClickedPacketRuleSpec, false);

    }


    /// <summary>
    /// Called from TaiserFilterRuleSpecPanel from Accept Advice button
    /// </summary>
    public void ApplyAdvice()
    {
        AcceptAdviceButton.interactable = false;
        //AdviceSpeciesButtonPanel.gameObject.SetActive(true);
        NewGameMgr.inst.ApplyFirewallRule(CurrentDestination, AdvisorRuleSpec, true);
    }

    //--------------------------Picking human or AI advice every time-----------------

    //public RectTransform AdviceSpeciesButtonPanel;
    public Text TeammateNameText;
    public enum AdvisingState
    {
        Undecided = 0,
        Human,
        AI,
        Me
    }

    public AdvisingState advisingState = AdvisingState.Undecided;
    public void AskForHumanAdvice()
    {
        Debug.Log("Picked human");
        //State = AdvisingState.Human;
        InstrumentMgr.inst.AddRecord2(TaiserEventTypes.PickedAdvisorType, "Human");
        StartCoroutine(ProvideAdviceWithDelay());
    }
    public void AskForAIAdvice()
    {
        Debug.Log("Picked AI");
        //State = AdvisingState.AI;
        //Show AI advice after interval
        InstrumentMgr.inst.AddRecord2(TaiserEventTypes.PickedAdvisorType, "AI");
        StartCoroutine(ProvideAdviceWithDelay());
    }

    public void AskMe()
    {
        
        InstrumentMgr.inst.AddRecord2(TaiserEventTypes.PickedAdvisorType, "Me");
    }

    public float MinHumanTime = 1f;
    public float MaxHumanTime = 1f;
    public float MinAITime = 1f;
    public float MaxAITime = 1f;

    public RectTransform Spinner;
    IEnumerator ProvideAdviceWithDelay()
    {
        PreAdviceUISetup();
        float delayInSeconds = (advisingState == AdvisingState.Human ?
            RandomRange(HumanDecisionRandomizer, MinHumanTime, MaxHumanTime) :
            RandomRange(AIDecisionRandomizer, MinAITime, MaxAITime));
        yield return  new WaitForSeconds(delayInSeconds);
        ProvideAdvice();
        PostAdviceUIReset();
    }

    void PreAdviceUISetup()
    {
        //AdviceSpeciesButtonPanel.gameObject.SetActive(false);

        Spinner.gameObject.SetActive(true);
        TeammateNameText.text = "Getting advice";
    }

    void PostAdviceUIReset()
    {
        AcceptAdviceButton.interactable = true;
        Spinner.gameObject.SetActive(false);
        //TeammateNameText.text = NewLobbyMgr.teammateName + "'s advice";
        TeammateNameText.text = advisingState.ToString();

    }

    public bool isFirstHumanAdvice = true;
    public bool isFirstAIAdvice = true;
    public void ProvideAdvice()
    {
        bool isCorrect = (advisingState == AdvisingState.Human ? 
            AdviceRandomizerFlip(HumanDecisionRandomizer, HumanCorrectAdviceProbability) :
            AdviceRandomizerFlip(AIDecisionRandomizer, AICorrectAdviceProbability));
        if(isFirstAIAdvice && advisingState == AdvisingState.AI) {
            isCorrect = true;
            isFirstAIAdvice = false;
        }
        if(isFirstHumanAdvice && advisingState == AdvisingState.Human) {
            isCorrect = true;
            isFirstHumanAdvice = false;
        }

        if(isCorrect) {
            AdvisorRuleSpec = CurrentDestination.MaliciousRule;
        } else {
            AdvisorRuleSpec = BlackhatAI.inst.CreateNonMaliciousPacketRuleForDestination(CurrentDestination);
        }
        DisplayPacketInformation(AdvisorRuleSpec, AdvisorPacketRuleTextList);
        InstrumentMgr.inst.AddRecord2(TaiserEventTypes.AdviceAppeared);

    }

}
