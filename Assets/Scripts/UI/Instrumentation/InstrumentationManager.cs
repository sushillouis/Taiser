using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Photon.Pun;

public class InstrumentationManager : Core.Utilities.SingletonPun<InstrumentationManager> {
	[System.Serializable]
	public struct InstrumentationEvent {
		public int playerID;
		public float timestamp;
		public string source;
		public string eventType;
		public string data;
	}

	[SerializeField]
	List<InstrumentationEvent> eventLog = new List<InstrumentationEvent>();

	void OnEnable(){
		GameManager.waveStartEvent += OnWaveStart;
		GameManager.waveEndEvent += OnWaveEnd;
		GameManager.gameEndEvent += OnApplicationQuit;
		ScoreManager.scoreEvent += OnScoreEvent;
		BlackHatBaseManager.updatedStartingPointRuleEvent += OnUpdatedStartingPointRuleEvent;
		BlackHatBaseManager.proposedStartingPointRuleEvent += OnProposedStartingPointRuleEvent;
		BlackHatBaseManager.updatedStartingPointProbabilityEvent += OnUpdatedStartingPointProbabilityEvent;
		BlackHatBaseManager.proposedStartingPointProbabilityEvent += OnProposedStartingPointProbabilityEvent;
		BlackHatBaseManager.updatedDestinationLikelihoodEvent += OnUpdatedDestinationLikelihoodEvent;
		BlackHatBaseManager.proposedDestinationLikelihoodEvent += OnProposedDestinationLikelihoodEvent;
		WhiteHatBaseManager.movedFirewallEvent += OnMovedFirewallEvent;
		WhiteHatBaseManager.firewallUpdatedEvent += OnFirewallUpdatedEvent;
		WhiteHatBaseManager.firewallProposedEvent += OnFirewallProposedEvent;
		WhiteHatBaseManager.honeypotUpdatedEvent += OnHoneypotUpdatedEvent;
		WhiteHatBaseManager.honeypotProposedEvent += OnHoneypotProposedEvent;
		WhiteHatBaseManager.suggestedFirewallEvent += OnSuggestedFirewallEvent;
	}
	void OnDisable(){
		GameManager.waveStartEvent -= OnWaveStart;
		GameManager.waveEndEvent -= OnWaveEnd;
		GameManager.gameEndEvent -= OnApplicationQuit;
		ScoreManager.scoreEvent -= OnScoreEvent;
		BlackHatBaseManager.updatedStartingPointRuleEvent -= OnUpdatedStartingPointRuleEvent;
		BlackHatBaseManager.proposedStartingPointRuleEvent -= OnProposedStartingPointRuleEvent;
		BlackHatBaseManager.updatedStartingPointProbabilityEvent -= OnUpdatedStartingPointProbabilityEvent;
		BlackHatBaseManager.proposedStartingPointProbabilityEvent -= OnProposedStartingPointProbabilityEvent;
		BlackHatBaseManager.updatedDestinationLikelihoodEvent -= OnUpdatedDestinationLikelihoodEvent;
		BlackHatBaseManager.proposedDestinationLikelihoodEvent -= OnProposedDestinationLikelihoodEvent;
		WhiteHatBaseManager.movedFirewallEvent -= OnMovedFirewallEvent;
		WhiteHatBaseManager.firewallUpdatedEvent -= OnFirewallUpdatedEvent;
		WhiteHatBaseManager.firewallProposedEvent -= OnFirewallProposedEvent;
		WhiteHatBaseManager.honeypotUpdatedEvent -= OnHoneypotUpdatedEvent;
		WhiteHatBaseManager.honeypotProposedEvent -= OnHoneypotProposedEvent;
		WhiteHatBaseManager.suggestedFirewallEvent -= OnSuggestedFirewallEvent;
	}

	public void SaveToFile(string filePath = "<default>"){
		// If the file path is default, update it to the persistent path, with the player's name and the current date
		if(filePath == "<default>") filePath = Application.persistentDataPath + "/"
			+ Networking.Player.localPlayer.nickname + System.DateTime.Now.Date.ToString("MM_dd_yyyy") + ".csv";

		// Open the file (or create it)
		Debug.Log(filePath);
		FileStream file = File.OpenWrite(filePath);

		// Write the CSV header
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes("player,isAI,timestamp,source,eventType,data\n");
		file.Write(bytes, 0, bytes.Length);

		// Sort the log according to timestamp // TODO: needed?
		eventLog.Sort(delegate(InstrumentationEvent a, InstrumentationEvent b) { return a.timestamp.CompareTo(b.timestamp); });

		// For each instrumentation...
		foreach(InstrumentationEvent e in eventLog){
			// Find the referenced player
			Networking.Player eventPlayer = System.Array.Find(NetworkingManager.roomPlayers, p => p.actorNumber == e.playerID);
			
			// Add the row to the file 
			bytes = System.Text.Encoding.UTF8.GetBytes(eventPlayer.nickname + "," + "false" + "," + e.timestamp.ToString() + "," + e.source + "," + e.eventType + "," + e.data + "\n");
			file.Write(bytes, 0, bytes.Length);
		}

		// Close the connection to the file
		file.Close();
	}

	// Generates a new event, already referencing the local player with the correct current timestamp
	public InstrumentationEvent generateNewEvent(){
		InstrumentationEvent e = new InstrumentationEvent();
		// Player ID won't be found while debugging... so return 1 in that case
		try { e.playerID = NetworkingManager.localPlayer.actorNumber;
		} catch (System.NullReferenceException){ e.playerID = 1; }
		e.timestamp = Time.time;
		return e;
	}

	// Logs an event (network synced)
	public void LogInstrumentationEvent(InstrumentationEvent e) {
		if(photonView is null) eventLog.Add(e);
		else photonView.RPC("RPC_InstrumentationManager_LogInstrumentationEvent", RpcTarget.AllBuffered, e.playerID, e.timestamp, e.source, e.eventType, e.data);
	}
	public void LogInstrumentationEventIfHost(InstrumentationEvent e) { if(NetworkingManager.isHost) LogInstrumentationEvent(e); }

	// Logs an event (network synced)
	public void LogInstrumentationEvent(string source, string eventType, string data = "") {
		InstrumentationEvent e = generateNewEvent();
		e.source = source;
		e.eventType = eventType;
		e.data = data;
		LogInstrumentationEvent(e);
	}
	public void LogInstrumentationEventIfHost(string source, string eventType, string data = "") { if(NetworkingManager.isHost) LogInstrumentationEvent(source, eventType, data); }

	// RPC which logs an event on every client
	[PunRPC] void RPC_InstrumentationManager_LogInstrumentationEvent(int playerID, float timestamp, string source, string eventType, string data){
		InstrumentationEvent e = new InstrumentationEvent();
		e.playerID = playerID;
		e.timestamp = timestamp;
		e.source = source;
		e.eventType = eventType;
		e.data = data;

		eventLog.Add(e);
	}


	// -- Event Callbacks --


	void OnApplicationQuit() {
		if(GameManager.instance?.currentWave > GameManager.instance?.maximumWaves) LogInstrumentationEventIfHost("GameManager", "GameEnd");
		SaveToFile();
	}

	void OnWaveStart() { LogInstrumentationEventIfHost("GameManager", "WaveStart", "" + GameManager.instance?.currentWave); }
	void OnWaveEnd() { LogInstrumentationEventIfHost("GameManager", "WaveEnd", "" + GameManager.instance?.currentWave); }
	void OnScoreEvent(float _, float whiteHatScore, float __, float blackHatScore) {
		LogInstrumentationEventIfHost("ScoreManager", "ScoreEvent - WhiteHat", "" + whiteHatScore + " - " + blackHatScore);
	}

	void OnUpdatedStartingPointRuleEvent(StartingPoint s, PacketRule r){
		LogInstrumentationEvent("BlackHat", "ChangedStartPointMaliciousPacketRules", "" + s.ID + " - " + r);
	}
	void OnProposedStartingPointRuleEvent(StartingPoint s, PacketRule r){
		LogInstrumentationEvent("BlackHat", "ProposedStartPointMaliciousPacketRules", "" + s.ID + " - " + r);
	}
	void OnUpdatedStartingPointProbabilityEvent(StartingPoint s, float probability){
		LogInstrumentationEvent("BlackHat", "ChangedStartPointMaliciousProbability", "" + s.ID + " - " + probability);
	}
	void OnProposedStartingPointProbabilityEvent(StartingPoint s, float probability){
		LogInstrumentationEvent("BlackHat", "ProposedStartPointMaliciousProbability", "" + s.ID + " - " + probability);
	}
	void OnUpdatedDestinationLikelihoodEvent(Destination d, int likelihood){
		LogInstrumentationEvent("BlackHat", "ChangedDestinationLikelihood", "" + d.ID + " - " + likelihood);
	}
	void OnProposedDestinationLikelihoodEvent(Destination d, int likelihood){
		LogInstrumentationEvent("BlackHat", "ProposeDestinationLikelihood", "" + d.ID + " - " + likelihood);
	}

	void OnMovedFirewallEvent(Firewall f, Vector3 pos, Quaternion _){
		LogInstrumentationEvent("WhiteHat", "PositionedFirewall", "" + f.ID + " - " + pos);
	}
	void OnFirewallUpdatedEvent(Firewall f, PacketRule r){
		LogInstrumentationEvent("WhiteHat", "ChangedFirewallFilterRules", "" + f.ID + " - " + r);	
	}
	void OnFirewallProposedEvent(Firewall f, PacketRule r){
		LogInstrumentationEvent("WhiteHat", "ProposedFirewallFilterRules", "" + f.ID + " - " + r);
	}
	void OnHoneypotUpdatedEvent(Destination d){
		LogInstrumentationEvent("WhiteHat", "MadeDestinationHoneypot", "" + d.ID);
	}
	void OnHoneypotProposedEvent(Destination d){
		LogInstrumentationEvent("WhiteHat", "ProposedMakeDestinationHoneypot", "" + d.ID);
	}
	void OnSuggestedFirewallEvent(SuggestedFirewall f, Vector3 pos, Quaternion _){
		LogInstrumentationEvent("WhiteHat", "ProposedFirewallPosition", pos.ToString());
	}
}
