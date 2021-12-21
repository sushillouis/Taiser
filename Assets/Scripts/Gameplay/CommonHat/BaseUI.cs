using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// UI elements that are common to both hats
public class BaseUI : Core.Utilities.Singleton<BaseUI> {
	// References to the score text UI elements
	public TMPro.TextMeshProUGUI blackHatScoreText, whiteHatScoreText;

	// De/register ourselves as a listener to game end events
	public void OnEnable(){ GameManager.gameEndEvent += OnGameEnd; }
	public void OnDisable(){ GameManager.gameEndEvent -= OnGameEnd; }

	// Reference to the ready button's text
	public TMPro.TextMeshProUGUI readyText;
	// Reference to the Game Over screen
	public GameObject endGameScreen;
	// References to the win and lose text in the game over screen
	public GameObject winText, loseText, gameEndText;
	// References to the score statistics on the game over screen
	public TMPro.TextMeshProUGUI blackHatFinalScore, whiteHatFinalScore;
	public TMPro.TextMeshProUGUI goodSpawned, maliciousSpawned, totalSpawned;

	// Passthrough callbacks which redirect button presses to the game manager
	public void OnToggleReady(){ GameManager.instance.toggleReady(); }
	public void OnDisconnectButtonPressed(){ GameManager.instance.OnDisconnectButtonPressed(); }

	// Function which fills the game end panel with statistics once the game ends
	public void OnGameEnd(){
		blackHatFinalScore.text = "Final BlackHat Score: " + ScoreManager.instance.blackHatScore;
		whiteHatFinalScore.text = "Final WhiteHat Score: " + ScoreManager.instance.whiteHatScore;

		var metrics = ScoreManager.instance.getAllWavesMetrics();
		goodSpawned.text = "Normal Packets Spawned: " + metrics.totalGoodPackets;
		maliciousSpawned.text = "Malicious Packets Spawned: " + metrics.totalMaliciousPackets;
		totalSpawned.text = "Total Packets Spawned: " + (metrics.totalGoodPackets + metrics.totalMaliciousPackets);
	}
}
