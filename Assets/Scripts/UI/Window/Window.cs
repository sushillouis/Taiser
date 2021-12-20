using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Window : EnhancedUIBehavior, IPointerDownHandler {
	// Reference to the window's title bar
	public WindowDrag titlebar;

	// Function which causes us to focus on this window
	public void Focus() => transform.SetAsLastSibling();

	// When a window is clicked, make sure it is drawn over all other UI elements
	public void OnPointerDown(PointerEventData e) => Focus();

	// When the window's close button is pressed destroy it
	public virtual void OnCloseButtonPressed() => Destroy(gameObject);

}
