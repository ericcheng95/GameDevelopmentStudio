using UnityEngine;
using System.Collections;

abstract public class MonsterMovement : Movement {

	public float SPEED;

	public MazeGeneratorController mazeGen;
	private Square[,] walls;
	protected bool canTurn = false;
	protected int direction;
	public int stunTime;
	private int stunned = 0;
	protected bool playerDetected;
	protected int detectionRange;
	protected int farDetectRange;
	protected bool attacking;
    private GameObject target;
    private Square lastSquare;

	public int attackRange;
	public int closeDetectRange;

	// Use this for initialization
	void Start () {
		walls = mazeGen.getWalls ();
		lastSquare = getCurrSquare (transform.position.x, transform.position.z);

		bool[] sides = getSides (lastSquare, transform.position.x, transform.position.z);

		direction = 3; // Quaternion.identity
		playerDetected = false;
		isClose = false;
		attacking = false;
		detectionRange = closeDetectRange;
		farDetectRange = closeDetectRange + 5;

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
    void Update()
    {
        // Stunned is a countdown - once the countdown is up, continue moving towards the player
        if (Network.isServer)
        {
            planMovement();
        }

    }
    private void planMovement()
    {
        try
        {
            if (stunned == 0)
            {
                // Move if not attacking
                if (!attacking)
                {
                    try
                    {
                        lastSquare = getCurrSquare(transform.position.x, transform.position.z);
                    }
                    catch (System.Exception e)
                    {

                    }
                    // Move towards the player if in sight
                    approachPlayer();
                    if (!playerDetected && Network.isServer)
                    {
                        // Navigates the maze
                        idleManeuver();
                    }
                    if (Network.isServer)
                        maneuver();
                }

                // Attack if not moving
                else
                {
                    doAttack();
                }
            }
            else
            {
                stunned -= 1;
            }
        }
        catch (System.Exception e)
        {
            float x = lastSquare.getRow() * mazeGen.wallSize;
            float z = lastSquare.getCol() * mazeGen.wallSize;
            Vector3 resetVector = new Vector3( x, transform.position.y, z);
            transform.position = resetVector;
        }
    }
	abstract public void maneuver ();
	abstract public void idleManeuver ();
	abstract public void doClose (Transform player);
	abstract public void doAttack();
	abstract public bool canAttack();

    public void SetTarget(GameObject t)
    {
        target = t;
    }
	protected void approachPlayer() {
        GameObject player;
        if (target == null) {
            return;
        }
        else
            player = target;
		Transform playerTransform = player.transform;
		float distance = Vector3.Distance (new Vector3(playerTransform.position.x, 0, playerTransform.position.z), 
		                                   new Vector3(transform.position.x, 0, transform.position.z));
		if (distance >= attackRange && distance <= detectionRange) {
			isClose = false;
			detectionRange = farDetectRange;
			transform.LookAt (new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z));
			playerDetected = true;
		} else if(distance < attackRange) {
			isClose = true;
			doClose (playerTransform);
		} else {
			detectionRange = closeDetectRange;

			// Occurs when the player moves out of the monster's range
			if(playerDetected) {

				// Jump to the center of the square, pick a random direction, and go!
                try
                {
                    Square curr = getCurrSquare(transform.position.x, transform.position.z);
                    lastSquare = new Square(curr);
                    Debug.Log(lastSquare);
                    transform.position = new Vector3(curr.getRow() * mazeGen.wallSize, transform.position.y, curr.getCol() * mazeGen.wallSize);
                    transform.rotation = Quaternion.identity;

                    direction = 3;
                    bool[] sides = getSides(curr, transform.position.x, transform.position.z);
                    bool found = false;
                    while (!found)
                    {
                        int side = Random.Range(0, 4);
                        found = !sides[side];

                        if (found)
                        {
                            turn(side);
                        }
                    }
                }
                // Mainly used if the monster leaves the maze
                catch (System.Exception e) 
                {
                    float row = lastSquare.getRow() * mazeGen.wallSize;
                    float col = lastSquare.getCol() * mazeGen.wallSize;
                    float y = transform.position.y;
                    // Reset the monster's position to its last valid square
                    transform.position = new Vector3(row, y, col);
                }
				
			}
			playerDetected = false;
		}
		//transform.position =  Vector3.MoveTowards(transform.position, playerTransform.position, step);

	}

	public override void setAttacking(bool state) {
		if(this.isClose && this.canAttack()) {
			attacking = state;
		}
	}

	// Sees if a monster is approximately in the center of a square (for turning purposes)
	protected bool isInCenter() {
		if (Mathf.Abs (transform.position.x - Mathf.Round (transform.position.x)) < .25 &&
		    Mathf.Round (transform.position.x) % mazeGen.wallSize == 0 && 
		    Mathf.Abs (transform.position.z - Mathf.Round (transform.position.z)) < .25 &&
		    Mathf.Round (transform.position.z) % mazeGen.wallSize == 0) {
			
			return true;
		}

		return false;
	}

	// Does the monster have a choice here?
	protected bool isFork(bool[] sides) {
		int falseCount = sideCount (sides);
		return falseCount > 2;
	}

	// Is this a corner? (No choice)
	protected bool isCorner(bool[] sides) {

		if (sideCount(sides) == 2) {
			return sides[direction]; // If there is no wall going forward, then this is not a corner.
		}

		return false;
	}

	// Is this a dead end?
	protected bool isDeadEnd(bool[] sides) {
		return sideCount (sides) == 1;
	}

	// Which way are we moving? Up/down or left/right?
	protected bool movingVert() {
		return direction % 2 == 0;
	}

	protected bool movingHoriz() {
		return direction % 2 == 1;
	}

	// Actually, returns the amount of missing sides.
	protected int sideCount(bool[] sides) {
		int falseCount = 0;
		for (int i = 0; i < sides.Length; i++) {
			if(!sides[i]) {
				falseCount++;
			}
		}
		return falseCount;
	}

	// Turns in a certain direction. 3 is Quaternion.identity, and the rest follow from there.
	// I should figure that out sometime.
	protected void turn(int dir) {
		transform.Rotate (Vector3.up * 90 * ((dir - direction) % 4));
		
		canTurn = false;
		direction = dir;


	}

	// A boolean array saying if each wall of the square exists.
	// Order: [south, west, north, east]
	protected bool[] getSides(Square s, float x, float z) {
		bool south, west, north, east;
		// Because some mazes don't generate a wall on both sides of the wall, we need to
		// check the next square over as well.
		if (Mathf.Round (x / mazeGen.wallSize + 1) < mazeGen.Rows)
			south = getCurrSquare (x + mazeGen.wallSize, z).hasNorth;
		else
			south = true;
		
		if (Mathf.Round (x / mazeGen.wallSize - 1) >= 0)
			north = getCurrSquare (x - mazeGen.wallSize, z).hasSouth;
		else
			north = true;
		
		if (Mathf.Round (z / mazeGen.wallSize - 1) >= 0)
			west = getCurrSquare (x, z - mazeGen.wallSize).hasEast;
		else
			west = true;
		
		if (Mathf.Round (z / mazeGen.wallSize + 1) < mazeGen.Cols)
			east = getCurrSquare (x, z + mazeGen.wallSize).hasWest;
		else
			east = true;
		
		return new[] {s.hasSouth || south, s.hasWest || west, s.hasNorth || north, s.hasEast || east};
	}

	// Gets the current square.
	protected Square getCurrSquare(float x, float z) {
		int initRow = (int) Mathf.Round (x / mazeGen.wallSize);
		int initCol = (int) Mathf.Round (z / mazeGen.wallSize);
		return walls [initRow, initCol];
	}

	// Stops the monster from moving.
	public override void stun() {
		stunned = stunTime;
	}
}
