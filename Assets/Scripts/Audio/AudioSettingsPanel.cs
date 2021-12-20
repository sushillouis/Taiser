using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsPanel : MonoBehaviour {
	public Slider musicSlider, effectsSlider, uiSlider;

	// When we are created update the sliders to match the currently set volume
	public void OnEnable(){
		musicSlider.value = AudioManager.instance.musicPlayer.volume;
		effectsSlider.value = AudioManager.instance.soundFXPlayer.volume;
		uiSlider.value = AudioManager.instance.uiSoundFXPlayer.volume;
	}

	// Whenever one of the slider's volumes are changed update the volume in the audio manager
	public void OnMusicVolumeChange(float volume) => AudioManager.instance.musicPlayer.volume = musicSlider.value;
	public void OnEffectsVolumeChange(float volume) => AudioManager.instance.soundFXPlayer.volume = effectsSlider.value;
	public void OnUIVolumeChange(float volume) => AudioManager.instance.uiSoundFXPlayer.volume = uiSlider.value;
}
