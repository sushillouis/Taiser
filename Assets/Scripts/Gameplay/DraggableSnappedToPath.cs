using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

// Base class which allows derived classes to be dragged around
public class DraggableSnappedToPath : MonoBehaviourPun {
	// Reference to the click action
	public InputActionReference clickAction;
	public InputActionReference mousePositionAction;

	// Boolean tracking if we are currently being dragged
	bool isDragging = false;
	// Boolean tracking if we were being dragged last frame
	bool wasDraggingLastFrame = false;

	// Saved position and rotation from when we started dragging
	protected Vector3 savedDraggingPosition;
	protected Quaternion savedDraggingRotation;

	void Update(){
		// We can only drag elements we control
		if(photonView.Controller != NetworkingManager.localPlayer) return;

		// Update if we were dragging last frame
		wasDraggingLastFrame = isDragging;

		// If we are dragging... drag and mark that we are dragging
		if(SelectionManager.instance.selected == gameObject && clickAction.action.ReadValue<float>() > Mathf.Epsilon){
			OnDrag();
			isDragging = true;
		// Otherwise... mark that we aren't dragging
		} else
			isDragging = false;

		// If our mouse is now over a UI element... we are no longer dragging
		if(Utilities.isPointerOverUIObject(mousePositionAction.action.ReadValue<Vector2>()))
			isDragging = false;

		// If we are dragging but weren't dragging last frame... then begin dragging
		if(isDragging && !wasDraggingLastFrame)
			OnBeginDrag();

		// If we were dragging last frame, but aren't this frame... hen end dragging
		if(wasDraggingLastFrame && !isDragging)
			OnEndDrag();
	}

	// Function which saves our position when we beging dragging
	protected virtual void OnBeginDrag() {
		savedDraggingPosition = transform.position;
		savedDraggingRotation = transform.rotation;
	}

	// Function which snaps our current position while we are dragging
	protected virtual void OnDrag(){
		try{
			Vector3 position;
			Quaternion rotation;
			PathToPositionRotation(SelectionManager.instance.hovered, out position, out rotation);
			transform.position = position;
			transform.rotation = rotation;
		} catch (System.NullReferenceException) {}
	}

	// Function which can be overridden to provide behavior when we end dragging
	protected virtual void OnEndDrag(){ }


	// Function which converts a path piece to a position and rotation for the dragable element to be placed at
	public static void PathToPositionRotation(GameObject targetPathPiece, out Vector3 position, out Quaternion rotation){
		position = targetPathPiece.transform.position;
		rotation = targetPathPiece.transform.rotation;
		BoxCollider collider = targetPathPiece.GetComponent<BoxCollider>();
		if(collider is object){
			position += collider.center;
			rotation = Quaternion.LookRotation(Utilities.positionNoY(collider.center), Vector3.up);
		}
	}
}
