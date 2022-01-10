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
		public string _old, _new;
	}

	[SerializeField]
	List<InstrumentationEvent> eventLog = new List<InstrumentationEvent>();

	void OnEnable(){
		GameManager.waveStartEvent += OnWaveStart;
		GameManager.waveEndEvent += OnWaveEnd;
		GameManager.gameEndEvent += OnApplicationQuit;
		ScoreManager.scoreEvent += OnScoreEvent;
		BlackHatBaseManager.updateStartingPointRuleEvent += OnUpdatedStartingPointRuleEvent;
		BlackHatBaseManager.proposeStartingPointRuleEvent += OnProposedStartingPointRuleEvent;
		BlackHatBaseManager.updateStartingPointProbabilityEvent += OnUpdatedStartingPointProbabilityEvent;
		BlackHatBaseManager.proposeStartingPointProbabilityEvent += OnProposedStartingPointProbabilityEvent;
		BlackHatBaseManager.updateDestinationLikelihoodEvent += OnUpdatedDestinationLikelihoodEvent;
		BlackHatBaseManager.proposeDestinationLikelihoodEvent += OnProposedDestinationLikelihoodEvent;
		WhiteHatBaseManager.moveFirewallEvent += OnMovedFirewallEvent;
		WhiteHatBaseManager.firewallUpdateEvent += OnFirewallUpdatedEvent;
		WhiteHatBaseManager.firewallProposeEvent += OnFirewallProposedEvent;
		WhiteHatBaseManager.honeypotUpdateEvent += OnHoneypotUpdatedEvent;
		WhiteHatBaseManager.honeypotProposeEvent += OnHoneypotProposedEvent;
		WhiteHatBaseManager.suggestedFirewallEvent += OnSuggestedFirewallEvent;
	}
	void OnDisable(){
		GameManager.waveStartEvent -= OnWaveStart;
		GameManager.waveEndEvent -= OnWaveEnd;
		GameManager.gameEndEvent -= OnApplicationQuit;
		ScoreManager.scoreEvent -= OnScoreEvent;
		BlackHatBaseManager.updateStartingPointRuleEvent -= OnUpdatedStartingPointRuleEvent;
		BlackHatBaseManager.proposeStartingPointRuleEvent -= OnProposedStartingPointRuleEvent;
		BlackHatBaseManager.updateStartingPointProbabilityEvent -= OnUpdatedStartingPointProbabilityEvent;
		BlackHatBaseManager.proposeStartingPointProbabilityEvent -= OnProposedStartingPointProbabilityEvent;
		BlackHatBaseManager.updateDestinationLikelihoodEvent -= OnUpdatedDestinationLikelihoodEvent;
		BlackHatBaseManager.proposeDestinationLikelihoodEvent -= OnProposedDestinationLikelihoodEvent;
		WhiteHatBaseManager.moveFirewallEvent -= OnMovedFirewallEvent;
		WhiteHatBaseManager.firewallUpdateEvent -= OnFirewallUpdatedEvent;
		WhiteHatBaseManager.firewallProposeEvent -= OnFirewallProposedEvent;
		WhiteHatBaseManager.honeypotUpdateEvent -= OnHoneypotUpdatedEvent;
		WhiteHatBaseManager.honeypotProposeEvent -= OnHoneypotProposedEvent;
		WhiteHatBaseManager.suggestedFirewallEvent -= OnSuggestedFirewallEvent;
	}

	public void SaveToFile(string filePath = "<default>"){
		// If the file path is default, update it to the persistent path, with the player's name and the current date
		if(filePath == "<default>") filePath = Application.persistentDataPath + "/"
			+ Networking.Player.localPlayer.remoteNickname.Replace("#", "_") + "." + System.DateTime.Now.ToString("MM_dd_yyyy.HH_mm") + ".csv";

		// Open the file (or create it)
		Debug.Log("Writing Instrumentation Log to: " + filePath);
		FileStream file = File.OpenWrite(filePath);

		// Write the CSV header
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes("timestamp,player,hat/AI?,source,eventType,old/correct,new/given\n");
		file.Write(bytes, 0, bytes.Length);

		// Sort the log according to timestamp // TODO: needed?
		eventLog.Sort(delegate(InstrumentationEvent a, InstrumentationEvent b) { return a.timestamp.CompareTo(b.timestamp); });

		// For each instrumentation...
		foreach(InstrumentationEvent e in eventLog){
			// Find the referenced player
			Networking.Player eventPlayer = System.Array.Find(NetworkingManager.roomPlayers, p => p.actorNumber == e.playerID);
			
			// Add the row to the file 
			bytes = System.Text.Encoding.UTF8.GetBytes(e.timestamp.ToString() + "," + eventPlayer.remoteNickname + "," + eventPlayer.side + "," + e.source + "," + e.eventType + "," + e._old + "," + e._new + "\n");
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
		else photonView.RPC("RPC_InstrumentationManager_LogInstrumentationEvent", RpcTarget.AllBuffered, e.playerID, e.timestamp, e.source, e.eventType, e._old, e._new);
	}
	public void LogInstrumentationEventIfHost(InstrumentationEvent e) { if(NetworkingManager.isHost) LogInstrumentationEvent(e); }

	// Logs an event (network synced)
	public void LogInstrumentationEvent(string source, string eventType, string _old = "", string _new = "") {
		InstrumentationEvent e = generateNewEvent();
		e.source = source;
		e.eventType = eventType;
		e._old = _old;
		e._new = _new;
		LogInstrumentationEvent(e);
	}
	public void LogInstrumentationEventIfHost(string source, string eventType, string _old = "", string _new = "") { if(NetworkingManager.isHost) LogInstrumentationEvent(source, eventType, _old, _new); }

	// RPC which logs an event on every client
	[PunRPC] void RPC_InstrumentationManager_LogInstrumentationEvent(int playerID, float timestamp, string source, string eventType, string _old = "", string _new = ""){
		InstrumentationEvent e = new InstrumentationEvent();
		e.playerID = playerID;
		e.timestamp = timestamp;
		e.source = source;
		e.eventType = eventType;
		e._old = _old;
		e._new = _new;

		eventLog.Add(e);
	}


	// -- Event Callbacks --


	void OnApplicationQuit() {
        //if(GameManager.instance?.currentWave > GameManager.instance?.maximumWaves) LogInstrumentationEventIfHost("GameManager", "GameEnd");
		//SaveToFile();
	}

	void OnWaveStart() { LogInstrumentationEventIfHost("GameManager", "WaveStart", "", "" + GameManager.instance?.currentWave); }
	void OnWaveEnd() { LogInstrumentationEventIfHost("GameManager", "WaveEnd", "", "" + GameManager.instance?.currentWave); }
	void OnScoreEvent(float whiteHatDelta, float whiteHatScore, float blackHatDelta, float blackHatScore) {
		LogInstrumentationEventIfHost("ScoreManager", "WhiteHat Score", "" + (whiteHatScore - whiteHatDelta), "" + whiteHatScore);
		LogInstrumentationEventIfHost("ScoreManager", "BlackHat Score", "" + (blackHatScore - blackHatDelta), "" + blackHatScore);
	}

	void OnUpdatedStartingPointRuleEvent(StartingPoint s, PacketRule r){
		LogInstrumentationEvent("Source #" + s.ID, "ChangedStartPointMaliciousPacketRules", "" + s.spawnedMaliciousPacketRules, "" + r);
	}
	void OnProposedStartingPointRuleEvent(StartingPoint s, PacketRule r){
		LogInstrumentationEvent("Source #" + s.ID, "ProposedStartPointMaliciousPacketRules", "" + s.spawnedMaliciousPacketRules, "" + r);
	}
	void OnUpdatedStartingPointProbabilityEvent(StartingPoint s, float probability){
		LogInstrumentationEvent("Source #" + s.ID, "ChangedStartPointMaliciousProbability", "" + s.maliciousPacketProbability, "" + probability);
	}
	void OnProposedStartingPointProbabilityEvent(StartingPoint s, float probability){
		LogInstrumentationEvent("Source #" + s.ID, "ProposedStartPointMaliciousProbability", "" + s.maliciousPacketProbability, "" + probability);
	}
	void OnUpdatedDestinationLikelihoodEvent(Destination d, int likelihood){
		LogInstrumentationEvent("Destination #" + d.ID, "ChangedDestinationLikelihood", "" + d.maliciousPacketDestinationLikelihood, "" + likelihood);
	}
	void OnProposedDestinationLikelihoodEvent(Destination d, int likelihood){
		LogInstrumentationEvent("Destination #" + d.ID, "ProposeDestinationLikelihood", "" + d.maliciousPacketDestinationLikelihood, "" + likelihood);
	}

	void OnMovedFirewallEvent(Firewall f, Vector3 pos, Quaternion _){
		LogInstrumentationEvent("Firewall #" + f.ID, "PositionedFirewall", "" + f.transform.position, "" + pos);
	}
	void OnFirewallUpdatedEvent(Firewall f, PacketRule r){
		LogInstrumentationEvent("Firewall #" + f.ID, "ChangedFirewallFilterRules", f.correctRule.Count > 0 ? "" + f.correctRule : "unkown", "" + r);	
	}
	void OnFirewallProposedEvent(Firewall f, PacketRule r){
		LogInstrumentationEvent("Firewall #" + f.ID, "ProposedFirewallFilterRules", f.correctRule.Count > 0 ? "" + f.correctRule : "unkown", "" + r);
	}
	void OnHoneypotUpdatedEvent(Destination d){
		LogInstrumentationEvent("Destination #" + d.ID, "MadeDestinationHoneypot");
	}
	void OnHoneypotProposedEvent(Destination d){
		LogInstrumentationEvent("Destination #" + d.ID, "ProposedMakeDestinationHoneypot");
	}
	void OnSuggestedFirewallEvent(SuggestedFirewall f, Vector3 pos, Quaternion _){
		LogInstrumentationEvent("WhiteHat", "ProposedFirewallPosition", "", pos.ToString());
	}
}
