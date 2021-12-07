using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HoverableLine : MonoBehaviour, SelectionManager.IHoverable /* It has to be IHoverable so that the SelectionManager can detect it*/ {

	// Line renderer which we update
	public LineRenderer line;

	// De/Register this object with the hover manager when it is dis/enabled.
	void OnEnable(){ SelectionManager.hoverChangedEvent += OnHoverChanged; }
	void OnDisable(){ SelectionManager.hoverChangedEvent -= OnHoverChanged; }

	// Callback which changes the hovered state of the object
	public void OnHoverChanged(GameObject newHover){
		if(newHover == gameObject)
			requestHoverEnable(true);
		else if(SelectionManager.instance.hovered == gameObject && newHover != gameObject)
			requestHoverEnable(false);
	}

	// Function which enables the hovering logic
	public Color hoverColor, baseColor;
	void requestHoverEnable(bool enable){
		// Debug.Log(enable);

		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(enable ? hoverColor : baseColor, 0.0f), new GradientColorKey(enable ? hoverColor : baseColor, 1.0f) },
			new GradientAlphaKey[] { new GradientAlphaKey(1, 0.0f), new GradientAlphaKey(1, 1.0f) }
		);

		line.colorGradient = gradient;
	}
}
