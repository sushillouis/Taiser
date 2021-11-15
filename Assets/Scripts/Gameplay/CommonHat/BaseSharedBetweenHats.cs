using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class which acts as the base class for both hat's managers
public class BaseSharedBetweenHats : Core.Utilities.Singleton<BaseSharedBetweenHats> {
	// Error codes used by the error handling system
	public class ErrorCodes {
		public static readonly int NoError = 0;
		public static readonly int Generic = 1;
		public static readonly int WrongPlayer = 2;			// Error code stating that the wrong player tried to interact with the object
		public static readonly int InvalidTarget = 3;		// Error code stating that the selected target is invalid
		public static readonly int NoUpdatesRemaining = 4;	// Error code stating that the selected target doesn't have any updates left

		public int value = Generic;

		// Constructor
		public ErrorCodes(){}
		public ErrorCodes(int _value){
			value = _value;
		}

		// Object equality (Required to override ==)
		public override bool Equals(System.Object obj) {
			if (obj == null)
				return false;
			int? o = obj as int?;
			return Equals(o.Value);
		}

		// Details equality
		public bool Equals(int v){
			return value == v;
		}

		// Required to override Equals
		public override int GetHashCode() { return base.GetHashCode(); }

		// Equality Operator
		public static bool operator ==(ErrorCodes a, ErrorCodes b) => a.Equals(b);
		// Inequality Operator (Required if == is overridden)
		public static bool operator !=(ErrorCodes a, ErrorCodes b) => !a.Equals(b);

		public static implicit operator int(ErrorCodes e) => e.value;
		public static implicit operator ErrorCodes(int value) => new ErrorCodes(value);

		// Implicit conversion to a bool (true if an error occurred, false otherwise)
		public static implicit operator bool(ErrorCodes e) => e != NoError;
	}

	// Register ourselves as a listener for events
	void OnEnable(){
		GameManager.waveStartEvent += OnWaveStart;
		GameManager.waveEndEvent += OnWaveEnd;
		GameManager.gameEndEvent += OnGameEnd;
		ScoreManager.scoreEvent += OnScoreEvent;
	}
	void OnDisable(){
		GameManager.waveStartEvent -= OnWaveStart;
		GameManager.waveEndEvent -= OnWaveEnd;
		GameManager.gameEndEvent -= OnGameEnd;
		ScoreManager.scoreEvent -= OnScoreEvent;
	}


	// -- GameState Accessors --


	// Access the selected element
	public static T getSelected<T>() where T : MonoBehaviour, SelectionManager.ISelectable {
		var selected = SelectionManager.instance.selected;
		if(selected is null || selected == null) return null;
		return selected?.GetComponent<T>();
	}

	public static GameObject[] getFirewallTargets(){ return GameObject.FindGameObjectsWithTag("FirewallTarget"); }
	public static GameObject[] getSwitchTargets(){ return GameObject.FindGameObjectsWithTag("SwitchTarget"); }

	// Access the lists of relevant objects
	public static Firewall[] getFirewalls() => Firewall.firewalls;
	public static StartingPoint[] getStartingPoints() => StartingPoint.startingPoints;
	public static Destination[] getDestinations() => Destination.destinations;
	public static PathNodeBase[] getPathNodes(bool removeStartDestination = false) {
		PathNodeBase[] _ret = FindObjectsOfType<PathNodeBase>();
		if(!removeStartDestination) return _ret;

		// Create a list with all of the starting points and destinations removed
		List<PathNodeBase> ret = new List<PathNodeBase>();
		foreach(PathNodeBase node in _ret){
			if(node is StartingPoint || node is Destination)
				continue;

			ret.Add(node);
		}

		return ret.ToArray();
	}

	// Access score information
	public static ScoreManager.WaveMetrics getCurrentWaveMetrics() => ScoreManager.instance.getCurrentWaveMetrics();
	public static ScoreManager.WaveMetrics getAllWavesMetrics() => ScoreManager.instance.getAllWavesMetrics();
	public static float getBlackHatScore() => ScoreManager.instance.blackHatScore;
	public static float getWhiteHatScore() => ScoreManager.instance.whiteHatScore;

	// Access game information
	public static GameManager.Difficulty getDifficulty() => GameManager.difficulty;
	public static int getCurrentWave() => GameManager.instance.currentWave;
	public static bool isWaveStarted() => GameManager.instance.waveStarted;


	// -- Virtual Event Handlers --


	public virtual void OnWaveStart() {}
	public virtual void OnWaveEnd() {}
	public virtual void OnGameEnd() {}
	public virtual void OnScoreEvent(float whiteHatDerivative, float whiteHatScore, float blackHatDerivative, float blackHatScore) {}


	// -- Error Handling --


	protected virtual void ErrorHandler(ErrorCodes errorCode, string error){
		Debug.LogError(error);
	}
	protected virtual void ErrorHandler(string error){ ErrorHandler(ErrorCodes.Generic, error); }
}
