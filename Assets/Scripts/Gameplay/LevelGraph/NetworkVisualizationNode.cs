using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkVisualizationNode : PathNodeBase {
	public LineRenderer[] lines = new LineRenderer[4];// northLine, southLine, eastLine, westLine;

	protected override void UpdateLocalGraphConnections(){
		base.UpdateLocalGraphConnections();

		// Ensure that the proper lines are enabled
		Directions[] toLoopThrough = new Directions[]{ Directions.North, Directions.East, Directions.South, Directions.West };
		for(int i = 0; i < connectedNodes.Length; i++)
			if((connectableMask & PathNodeBase.fromIndex(i)) == 0) lines[i].gameObject.SetActive(false);
			else if(connectedNodes[i] is object) {
				lines[i].gameObject.SetActive(true);

				Vector3 direction = Utilities.positionSetY(connectedNodes[i].transform.position, .1f) - Utilities.positionSetY(transform.position, .1f);
				float distance = direction.magnitude;
				direction = direction.normalized;

				float connectiveRatio = .5f;
				if(connectedNodes[i] is TerminalNode) connectiveRatio = 1;

				Vector3[] positions = new Vector3[2];
				positions[0] = transform.position - direction * lines[i].widthCurve.Evaluate(1) * .5f;
				positions[1] = transform.position + direction * distance * connectiveRatio;

				lines[i].SetPositions(positions);
			} else lines[i].gameObject.SetActive(false);
	}

}
