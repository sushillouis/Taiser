using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EffectsMgr : MonoBehaviour
{

    public static EffectsMgr inst;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        //SetupTextConsole();
        //maxLinesOfText = ConsoleLines.Count;
    }

    // Update is called once per frame
    void Update()
    {
        //string tmp = "";
        //foreach(string s in consoleTextQueue) {
        //    tmp += s + "\n\n";
        //}
        //console.text = tmp;
        //console.CrossFadeAlpha(0, 5f, false);
    }

    //public Text console;
    /*
    int maxLinesOfText;
    public Color TextColor;
    public RectTransform ConsoleRoot;
    public List<Text> ConsoleLines = new List<Text>();
    public List<string> consoleTextQueue = new List<string>();

    public class ConsoleTextProps
    {
        public string text;
        public Color fontColor;
    }

    public List<ConsoleTextProps> consoleTextPropsQueue = new List<ConsoleTextProps>();

    [ContextMenu("SetupTextConsole")]
    public void SetupTextConsole()
    {
        ConsoleLines.Clear();
        foreach(Text uiText in ConsoleRoot.GetComponentsInChildren<Text>()) {
            uiText.text = "";
            ConsoleLines.Add(uiText);
        }
    }

    public void ClearConsoleTextQueue()
    {
        foreach(Text txt in ConsoleLines) {
            txt.text = "";
        }
        consoleTextQueue.Clear();
    }

    */
    public void NewRule(TDestination destination, LightWeightPacket maliciousRule)
    {
        NewAudioMgr.inst.source.PlayOneShot(NewAudioMgr.inst.MaliciousRuleChanged);
        //AddToConsole(destination.inGameName + ": New ATTACK");
    }

    /*
    void AddToConsole(string s)
    {
        if(consoleTextQueue.Count >= maxLinesOfText) {
            consoleTextQueue.RemoveAt(consoleTextQueue.Count-1);
        }
        consoleTextQueue.Insert(0, s);
        int i = 0;
        foreach(string txt in consoleTextQueue) { 
            ConsoleLines[i++].text = txt;
        }
    }
    */

    public void MaliciousUnfilteredPacket(TDestination destination, LightWeightPacket maliciousPacket)
    {
        NewAudioMgr.inst.source.PlayOneShot(NewAudioMgr.inst.maliciousUnfiltered);
        //AddToConsole(destination.inGameName + "\nMALICIOUS packet detected");
    }

    public void MaliciousFilteredPacket(TDestination destination, LightWeightPacket maliciousPacket)
    {
        NewAudioMgr.inst.source.PlayOneShot(NewAudioMgr.inst.maliciousFiltered);
        //AddToConsole(destination.inGameName + ":\nDestroyed malicious packet");
    }

    public void GoodFilterApplied(TDestination destination, LightWeightPacket filterRule)
    {
        NewAudioMgr.inst.PlayOneShot(NewAudioMgr.inst.GoodFilterRule);
        //AddToConsole(destination.inGameName + ": GOOD Rule");
        //console.CrossFadeColor(Color.green, 0f, false, false);
        //console.CrossFadeAlpha(1f, 0f, false);
    }

    public void BadFilterApplied(TDestination destination, LightWeightPacket filterRule)
    {
        NewAudioMgr.inst.PlayOneShot(NewAudioMgr.inst.BadFilterRule);
        //AddToConsole(destination.inGameName + ": BAD Rule");
    }

}
