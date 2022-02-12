using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Text;

//----------------------------------------------------
//should use csv helper
//https://joshclose.github.io/CsvHelper/
//----------------------------------------------------

[System.Serializable]
public class TaiserSession
{
    public string name;
    public Role role;
    public System.DateTime dayAndTime;
    public float whitehatScore;
    public float blackhatScore;
    public List<TaiserRecord> records;
}

[System.Serializable]
public class TaiserRecord
{
    /// <summary>
    /// From start of game
    /// </summary>
    public float secondsFromStart;
    public string eventName;
    public List<string> eventModifiers;
}

[System.Serializable]
public enum TaiserEventTypes
{
    RuleSpec = 0, //which button?
    Filter,       //which rule?
    MaliciousBuilding,  //which building?
    Menu,
    FirewallSetCorrect,
    FirewallSetInCorrect,
    PacketInspect,      //Packet info
    StartWave,
    EndWave,
    SetNewMaliciousRule,
}

public class InstrumentMgr : MonoBehaviour
{
    public static InstrumentMgr inst;
    public void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateOrFindTaiserFolder();

    }

    // Update is called once per frame
    void Update()
    {
        if(UnityEngine.InputSystem.Keyboard.current.homeKey.wasReleasedThisFrame) {
            WriteSession();
        }
    }

    public string TaiserFolder;

    public void CreateOrFindTaiserFolder()
    {
        try {
            TaiserFolder = System.IO.Path.Combine(Application.persistentDataPath);
            System.IO.Directory.CreateDirectory(TaiserFolder);
        }
        catch(System.Exception e) {
            Debug.Log("Cannot create Taiser Directory: " + e.ToString());
        }

    }

    public List<TaiserRecord> records = new List<TaiserRecord>();
    public TaiserSession session = new TaiserSession();

    public void AddRecord(string eventName, List<string> modifiers)
    {
        TaiserRecord record = new TaiserRecord();
        record.eventName = eventName;
        record.eventModifiers = modifiers;
        record.secondsFromStart = Time.realtimeSinceStartup;
        session.records.Add(record);
    }

    public void AddRecord(string eventName, string modifier = "")
    {
        TaiserRecord record = new TaiserRecord();
        record.eventName = eventName;
        List<string> mods = new List<string>();
        mods.Add(modifier);
        record.eventModifiers = mods;
        record.secondsFromStart = Time.realtimeSinceStartup;
        session.records.Add(record);
    }

    public void WriteSession()
    {
        session.whitehatScore = BlackhatAI.inst.wscore;
        session.blackhatScore = BlackhatAI.inst.bscore;
        session.name = NewLobbyMgr.PlayerName;
        using(StreamWriter sw = new StreamWriter(File.Open(Path.Combine(TaiserFolder, session.name+".csv"), FileMode.CreateNew), Encoding.UTF8)) {
            WriteHeader(sw);
            WriteRecords(sw);
        }
    }

    public void WriteHeader(StreamWriter sw)
    {
        sw.WriteLine(session.name + ", " + session.role + ", " + session.dayAndTime);
        sw.WriteLine("Whitehat Score, " + session.whitehatScore.ToString("00.0") + 
            ", Blackhat Score, " + session.blackhatScore.ToString("00.0"));
        sw.WriteLine("Time, Event, Modifiers");

    }
    
    public void WriteRecords(StreamWriter sw)
    {
        foreach(TaiserRecord tr in session.records) {
            string mods = CSVString(tr.eventModifiers);
            sw.WriteLine(tr.secondsFromStart.ToString("0000.0") + ", "
                + tr.eventName + mods);
        }
    }

    public string CSVString(List<string> mods)
    {
        string modifiers = "";
        foreach(string mod in mods) {
            modifiers += ", " + mod;
        }
        return modifiers;
    }
}
