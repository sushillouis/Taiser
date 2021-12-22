using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Class which provides instrumentation for a button
public class InstrumentedButton : ThemedButton {
	[Tooltip("The name in the event log of this source")]
	public string sourceName;
	[Tooltip("Wether or not instrumentation should be enabled on this button or not.")]
	public bool enableInstrumentation = true;

	// On awake register a callback to log changes
	protected override void Awake() { base.Awake(); onClick.AddListener(logPressed); }

	// When the button is pressed... log that it was pressed
	void logPressed(){
		// If instrumentation is disabled, don't bother with this function
		if(!enableInstrumentation) return;

		// Log the event
		InstrumentationManager.instance.LogInstrumentationEvent(sourceName, "Pressed");
	}

	#if UNITY_EDITOR
		// Menu item which converts ThemedButton to InstrumentedButton
		[MenuItem("CONTEXT/ThemedButton/Make Instrumented")]
		public static void MakeInstrumented(MenuCommand command){
			ThemedButton old = command.context as ThemedButton;
			GameObject go = old.gameObject;

			ButtonClickedEvent onClick = old.onClick;

			// Selectable
			Navigation navigation = old.navigation;
			Transition transition = old.transition;
			ColorBlock colors = old.colors;
			SpriteState spriteState = old.spriteState;
			AnimationTriggers animationTriggers = old.animationTriggers;
			Graphic targetGraphic = old.targetGraphic;
			bool interactable = old.interactable;

			UnityEngine.GameObject.DestroyImmediate(old);
			InstrumentedButton _new = go.AddComponent<InstrumentedButton>();

			// By default set the instrumented's name to the name of the game object it is attached to
			_new.sourceName = _new.gameObject.name;

			_new.onClick = onClick;

			// Selectable
			_new.navigation = navigation;
			_new.transition = transition;
			_new.colors = colors;
			_new.spriteState = spriteState;
			_new.animationTriggers = animationTriggers;
			_new.targetGraphic = targetGraphic;
			_new.interactable = interactable;

			command.context = _new;
		}

		// Menu item which converts InstrumentedSlider back to ThemedSliders
		[MenuItem("CONTEXT/InstrumentedButton/Remove Instrumentation")]
		public static void RemoveInstrumented(MenuCommand command){
			InstrumentedButton old = command.context as InstrumentedButton;
			GameObject go = old.gameObject;

			ButtonClickedEvent onClick = old.onClick;

			// Selectable
			Navigation navigation = old.navigation;
			Transition transition = old.transition;
			ColorBlock colors = old.colors;
			SpriteState spriteState = old.spriteState;
			AnimationTriggers animationTriggers = old.animationTriggers;
			Graphic targetGraphic = old.targetGraphic;
			bool interactable = old.interactable;

			UnityEngine.GameObject.DestroyImmediate(old);
			ThemedButton _new = go.AddComponent<ThemedButton>();

			_new.onClick = onClick;

			// Selectable
			_new.navigation = navigation;
			_new.transition = transition;
			_new.colors = colors;
			_new.spriteState = spriteState;
			_new.animationTriggers = animationTriggers;
			_new.targetGraphic = targetGraphic;
			_new.interactable = interactable;

			command.context = _new;
		}
	#endif
}

#if UNITY_EDITOR
// Class which provides the custom inspector view for a instrumented button
[CustomEditor(typeof(InstrumentedButton))]
public class InstrumentedButtonEditor : ThemedButtonEditor {
	public override void OnInspectorGUI() {
		InstrumentedButton button = (InstrumentedButton)target;

		// Give an option for the name of the source
		button.sourceName = EditorGUILayout.TextField(new GUIContent("Source Name:", "The name in the event log of this source"), button.sourceName);
		// Show the themed button GUI
		base.OnInspectorGUI();
	}
}
#endif
