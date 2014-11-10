﻿using System;
using System.Collections;
/* @author: Michael Gonzalez
 * This class represents one Square inside the wall matrix. By default, all 
 * walls are considered "up" or "on"
 */
public class Square
{
    public bool visited { get; set; } // Has this cell been visited yet?
    public bool start; // Is this the start point for the player?
    public bool exit; // Is this the maze exit?
    public bool hasNorth {get; set;} // Is there a wall to the North?
    public bool hasSouth { get; set; } // Is there a wall to the South?
    public bool hasWest { get; set; } // Is there a wall to the West?
    public bool hasEast { get; set; } // Is there a wall to the East?

    //(Can only be accessed through getters)
    private int row; // R index in walls matrix 
    private int col; // C index in walls matrix

    // You can be creative with this one. It ensures both adjacent walls are
    // destroyed in the DepthFirst algorithm. For example, say the next wall to 
    // destroy is to the North of the current square. In this algorithm, this 
    // variable provides space to store the fact the north wall in the current
    // square can be destroyed and the south wall in the square above it is 
    // destroyed as well.
    private int wallToDestroy;
	public Square(int r, int c)
	{
        row = r;
        col = c;
        hasNorth = true;
        hasSouth = true;
        hasWest = true;
        hasEast = true;
	}
    public int getRow() { return row; }
    public int getCol() { return col; }
    public int getWallToDestroy() { return wallToDestroy;  }
    public void setWallToDestroy(int w2d) { wallToDestroy = w2d; }
}
