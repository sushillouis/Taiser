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

	public void OnApplicationQuit(){
		SaveToFile();
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

	// Logs an event (network synced)
	public void LogInstrumentationEvent(string source, string eventType, string data = "") {
		InstrumentationEvent e = generateNewEvent();
		e.source = source;
		e.eventType = eventType;
		e.data = data;
		LogInstrumentationEvent(e);
	}

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
}
