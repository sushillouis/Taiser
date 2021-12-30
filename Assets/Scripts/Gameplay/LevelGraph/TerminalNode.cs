using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerminalNode : StartingPoint {
	public static TerminalNode[] terminals = null;

	new protected void Awake() {
		terminals = FindObjectsOfType<TerminalNode>();
		
		// TODO: Is this nessicary if StartingPoint's awake is hidden?
		try{
			// Remove all terminal nodes from the starting point list
			List<StartingPoint> startingPoints = new List<StartingPoint>(StartingPoint.startingPoints);
			startingPoints.RemoveAll(i => i is TerminalNode); 
			StartingPoint.startingPoints = startingPoints.ToArray();
		} catch (System.ArgumentNullException) {} // Do nothing on a null reference
		
	}
}
