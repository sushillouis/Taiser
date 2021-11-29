using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkVisualizationNode : PathNodeBase {
	public LineRenderer[] lines = new LineRenderer[4];// northLine, southLine, eastLine, westLine;
	public BoxCollider[] colliders = new BoxCollider[4];

	protected override void UpdateLocalGraphConnections(){
		base.UpdateLocalGraphConnections();

		// Ensure that the proper lines are enabled
		for(int i = 0; i < connectedNodes.Length; i++){
			Directions d = PathNodeBase.fromIndex(i);

			if((connectableMask & d) == 0) lines[i].gameObject.SetActive(false);
			else if(connectedNodes[i] is object) {
				lines[i].gameObject.SetActive(true);

				// Calculate the normalized direction and distance to the connected node
				Vector3 direction = Utilities.positionSetY(connectedNodes[i].transform.position, .1f) - Utilities.positionSetY(transform.position, .1f);
				float distance = direction.magnitude;
				direction = direction.normalized;

				// Determine how close the line should get to the connected node
				float connectiveRatio = .5f;
				if(connectedNodes[i] is TerminalNode) connectiveRatio = 1;

				// Calculate the positions of the two points on the line
				Vector3[] positions = new Vector3[2];
				positions[0] = transform.position - direction * lines[i].widthCurve.Evaluate(1) * .5f;
				positions[1] = transform.position + direction * distance * connectiveRatio;
				// Apply those positions
				lines[i].SetPositions(positions);


				// Set the position and size of the colliders
				if(d == PathNodeBase.Directions.North || d == PathNodeBase.Directions.South){
					colliders[i].size = new Vector3(.5f, .5f, distance * connectiveRatio);
					colliders[i].center = new Vector3(0, 0, distance * connectiveRatio * .5f * (d == PathNodeBase.Directions.South ? -1 : 1));
				} else {
					colliders[i].size = new Vector3(distance * connectiveRatio, .5f, .5f);
					colliders[i].center = new Vector3(distance * connectiveRatio * .5f * (d == PathNodeBase.Directions.West ? -1 : 1), 0, 0);
				}
			} else lines[i].gameObject.SetActive(false);
		}
	}

}
