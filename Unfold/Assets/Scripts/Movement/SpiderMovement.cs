﻿using UnityEngine;
using System.Collections;

public class SpiderMovement : MonsterMovement {

	bool forward = true;
	int counter = 0;

	public override void maneuver()
	{
		if(!isClose)
			transform.Translate (Vector3.forward * SPEED);
	}

	public override void idleManeuver()
	{

		
		if (canTurn && isInCenter ()) {
			Square curr = getCurrSquare(transform.position.x, transform.position.z);
			bool[] sides = getSides (curr, transform.position.x, transform.position.z);
			if (isFork (sides)) {
				bool found = false;
				sides[(direction + 2) % 4] = true; // Don't want to turn around
				
				while (!found) {
					int side = Random.Range (0, 4);
					found = !sides[side];
					
					if (found) {
						turn (side);
					}
				}
			} else if (isCorner(sides)) {
				sides[(direction + 2) % 4] = true;
				
				if(sides[(direction + 1) % 4]) {
					turn ((direction + 3) % 4);
				} else {
					turn ((direction + 1) % 4);
				}
			} else if (isDeadEnd(sides)) {
				turn ((direction + 2) % 4);
			}
			canTurn = false;
		} else if (!canTurn && movingVert () && Mathf.Abs (transform.position.x - Mathf.Round (transform.position.x)) < .2 && 
		           Mathf.Round (transform.position.x) % mazeGen.wallSize == Mathf.Round(mazeGen.wallSize / 2)) {
			
			canTurn = true;
		} else if (!canTurn && movingHoriz () && Mathf.Abs (transform.position.z - Mathf.Round (transform.position.z)) < .2 && 
		           Mathf.Round (transform.position.z) % mazeGen.wallSize == Mathf.Round(mazeGen.wallSize / 2)) {	
			canTurn = true;
		}
	}

	// For when the monster is really close
	public override void doClose(Transform player) {
		transform.LookAt (new Vector3(player.position.x, 0, player.position.z));
	}

	// special for birds, but needs to be defined for others
	public override bool canAttack() {
		return true;
	}

	public override void doAttack() {
		if(this.forward) {
			this.transform.Translate (Vector3.forward * .2f);
			this.counter += 2;
		}

		if(this.counter == 10) {
			this.forward = false;
		}

		if(!this.forward) {
			this.transform.Translate (Vector3.back * .1f);
			this.counter -= 1;
		}

		if(!this.forward && counter == 0) {
			this.attacking = false;
			this.forward = true;
		}

	}

}
