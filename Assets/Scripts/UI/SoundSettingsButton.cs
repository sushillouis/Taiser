using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundSettingsButton : EnhancedUIBehavior {
	// Reference to the audio settings window prefab
    public GameObject audioSettingsWindowPrefab;
	// Reference to a spawned audio settings window
    public GameObject audioSettingsWindowInstance;

	// When we click on the button, either spawn the settings window or make it our focus
	public void OnClick(){
		if(!audioSettingsWindowInstance)
			audioSettingsWindowInstance = Instantiate(audioSettingsWindowPrefab, new Vector3(canvas.pixelRect.width / 2, canvas.pixelRect.height / 2, 0), Quaternion.identity, canvas.transform);
		else
			audioSettingsWindowInstance.GetComponent<Window>().Focus();
	}

}
