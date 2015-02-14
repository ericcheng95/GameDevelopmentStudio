using UnityEngine;
using System.Collections;

abstract public class MonsterMovement : MonoBehaviour {

	public float SPEED;

	public MazeGeneratorController mazeGen;
	private Square[,] walls;
	bool canTurn = false;
	int direction;
	// Use this for initialization
	void Start () {
		walls = mazeGen.getWalls ();
		Square initSqr = getCurrSquare (transform.position.x, transform.position.z);

		bool[] sides = getSides (initSqr);

		direction = 3;

		bool found = false;
		while (!found) {
			int side = Random.Range (0, 4); // Anhquan thinks this is gonna be a problem, if he's right then he wins
			found = !sides [side];
		
			if (found) {
				turn (side);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		transform.Translate (Vector3.forward * SPEED);

		if (canTurn && isInCenter ()) {
			Square curr = getCurrSquare(transform.position.x, transform.position.z);
			bool[] sides = getSides (curr);
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
		maneuver ();
	}

	abstract public void maneuver ();

	bool isInCenter() {
		if (Mathf.Abs (transform.position.x - Mathf.Round (transform.position.x)) < .25 &&
		    Mathf.Round (transform.position.x) % mazeGen.wallSize == 0 && 
		    Mathf.Abs (transform.position.z - Mathf.Round (transform.position.z)) < .25 &&
		    Mathf.Round (transform.position.z) % mazeGen.wallSize == 0) {
			
			return true;
		}

		return false;
	}

	bool isFork(bool[] sides) {
		int falseCount = sideCount (sides);

		return falseCount > 2;
	}

	bool isCorner(bool[] sides) {

		if (sideCount(sides) == 2) {
			return sides[direction]; // If there is no wall going forward, then this is not a corner.
		}

		return false;
	}

	bool isDeadEnd(bool[] sides) {
		return sideCount (sides) == 1;
	}

	bool movingVert() {
		return direction % 2 == 0;
	}

	bool movingHoriz() {
		return direction % 2 == 1;
	}

	// Actually, returns the amount of missing sides.
	int sideCount(bool[] sides) {
		int falseCount = 0;
		for (int i = 0; i < sides.Length; i++) {
			if(!sides[i]) {
				falseCount++;
			}
		}
		return falseCount;
	}

	void turn(int dir) {

		transform.Rotate (Vector3.up * 90 * ((dir - direction) % 4));
		
		canTurn = false;
		direction = dir;


	}

	private bool[] getSides(Square s) {
		return new[] {s.hasSouth, s.hasWest, s.hasNorth, s.hasEast};
	}

	private Square getCurrSquare(float x, float z) {
		int initRow = (int) Mathf.Round (x / mazeGen.wallSize);
		int initCol = (int) Mathf.Round (z / mazeGen.wallSize);
		return walls [initRow, initCol];
	}
}
