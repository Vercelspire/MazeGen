using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    // width
    public int width = 10;

    // height
    public int height = 10;
    public float wallSize = 1f;

    // track cells
    // prevents revisiting
    private bool[,] visited;

    // list to store all wall GameObjects so destroy them when carving
    private List<GameObject> walls;

    private Coroutine generationCoroutine;

    // entrance + exit
    private GameObject startMarker;
    private GameObject endMarker;

    void Start()
    {
        GenerateMaze();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearMaze();
            GenerateMaze();
        }
    }
    void GenerateMaze()
    {
        // values default to unvisited
        visited = new bool[width, height];

        // list to store walls
        walls = new List<GameObject>();

        CreateGrid();

        // creates exit + enterance
        CreateEntranceAndExit();
        CreateStartAndEndMarkers();

        // Start gen via (0, 0)
        // coroutine to visualize  generation 
        generationCoroutine = StartCoroutine(Generate(0, 0));
    }

    // Creates grid (4 walls)
    void CreateGrid()
    {
        // loop x-coordinate
        for (int x = 0; x < width; x++)
        {
            // loop y-coordinate
            for (int y = 0; y < height; y++)
            {
                Vector3 cellCenter = new Vector3(x * wallSize, 0, y * wallSize);

                // Create North 
                CreateWall(cellCenter + new Vector3(0, 0, wallSize * 0.5f),
                          new Vector3(wallSize, wallSize, 0.1f)); 

                // Create South 
                CreateWall(cellCenter + new Vector3(0, 0, -wallSize * 0.5f),
                          new Vector3(wallSize, wallSize, 0.1f));

                // Create East  
                CreateWall(cellCenter + new Vector3(wallSize * 0.5f, 0, 0),
                          new Vector3(0.1f, wallSize, wallSize)); 

                // Create West  
                CreateWall(cellCenter + new Vector3(-wallSize * 0.5f, 0, 0),
                          new Vector3(0.1f, wallSize, wallSize));
            }
        }
    }

    void CreateWall(Vector3 position, Vector3 scale)
    {
        // Create cube GameObject
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.parent = transform;

        // adds wall to list so track and remove later
        wall.isStatic = true;
        walls.Add(wall);
    }
    IEnumerator Generate(int x, int y)
    {
        // prevents dupes
        visited[x, y] = true;

        // gets neighboring cells that we not visit
        List<Vector2Int> neighbors = GetUnvisitedNeighbors(x, y);

        // looping while this cell has unvisited neighbors
        while (neighbors.Count > 0)
        {
            // Pick random neighbor from list
            int randIndex = Random.Range(0, neighbors.Count);
            Vector2Int neighbor = neighbors[randIndex];

            // visit/not visit check
            // check to be sure
            if (!visited[neighbor.x, neighbor.y])
            {
                // remove wall between current cell and chosen neighbor
                // creates a passage connecting two cells
                RemoveWallBetween(x, y, neighbor.x, neighbor.y);
                yield return new WaitForSeconds(0.01f);

                // recursively visit neighbor cell
                // continues the search
                yield return StartCoroutine(Generate(neighbor.x, neighbor.y));
            }

            // recheck neighbors just in case
            // we check again after returning
            neighbors = GetUnvisitedNeighbors(x, y);
        }
    }

    // returns list of all neighboring cells -> haven't visited
    List<Vector2Int> GetUnvisitedNeighbors(int x, int y)
    {
        // store valid neighbors
        List<Vector2Int> neighbors = new List<Vector2Int>();

        // check North neighbor (y + 1)
        // checks to make sure it's in grid bounds and not visited
        if (y + 1 < height && !visited[x, y + 1])
            neighbors.Add(new Vector2Int(x, y + 1));

        // check South neighbor (y - 1)
        if (y - 1 >= 0 && !visited[x, y - 1])
            neighbors.Add(new Vector2Int(x, y - 1));

        // check East neighbor (x + 1)
        if (x + 1 < width && !visited[x + 1, y])
            neighbors.Add(new Vector2Int(x + 1, y));

        // check West neighbor (x - 1)
        if (x - 1 >= 0 && !visited[x - 1, y])
            neighbors.Add(new Vector2Int(x - 1, y));

        // return the list
        // can be empty if all visited
        return neighbors;
    }

    // Removes the wall(s) between two adjacent cells to create a passage
    void RemoveWallBetween(int x1, int y1, int x2, int y2)
    {
        // Calc midpoint position between two cells
        // location shared walls are located
        float midX = (x1 + x2) / 2f * wallSize;
        float midY = (y1 + y2) / 2f * wallSize;
        Vector3 midpoint = new Vector3(midX, 0, midY);

        // We make a list to store walls we might wanna remove
        // ->  because duplicate walls in this location
        // -> (cell creates own walls ->  shared walls exist twice)
        List<GameObject> wallsToRemove = new List<GameObject>();

        // Loop existing walls
        foreach (GameObject wall in walls)
        {
            // if wall exist
            if (wall != null)
            {
                float distance = Vector3.Distance(wall.transform.position, midpoint);

                // dist check
                if (distance < 0.1f)
                {
                    wallsToRemove.Add(wall);
                }
            }
        }

        // Remove all walls we found at this location
        foreach (GameObject wall in wallsToRemove)
        {
            // Remove from our tracking list
            walls.Remove(wall);

            // Destroy the GameObject from the scene
            Destroy(wall);
        }
    }

    // Completely destroys the current maze
    void ClearMaze()
    {
        // Stops the generation 
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
        }

        // loop wall GameObjects
        foreach (GameObject wall in walls)
        {
            if (wall != null)
            {
                // destroy 
                Destroy(wall);
            }
        }
        walls.Clear();
        // if start marker exists
        if (startMarker != null)
        {
            Destroy(startMarker);
        }

        // destroy end marker 
        if (endMarker != null)
        {
            Destroy(endMarker);
        }
    }

    // creates openings in the maze boundary for entrance and exit
    void CreateEntranceAndExit()
    {
        // remove entrance wall at start cell (0, 0)
        // remove the west wall so lplr can enter from the left
        Vector3 entrancePos = new Vector3(-wallSize * 0.5f, 0, 0);
        RemoveWallAtPosition(entrancePos);

        // remove exit wall at end cell (width-1, height-1)
        // remove the east wall so lplr can exit to the right
        Vector3 exitPos = new Vector3((width - 1) * wallSize + wallSize * 0.5f, 0, (height - 1) * wallSize);
        RemoveWallAtPosition(exitPos);
    }


    void RemoveWallAtPosition(Vector3 position)
    {
        GameObject wallToRemove = null;
        float minDistance = 0.1f;

        // loop walls
        foreach (GameObject wall in walls)
        {
            // Make sure wall exists
            if (wall != null)
            {
                // dist check
                float distance = Vector3.Distance(wall.transform.position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;

                    // store this as remove wall
                    wallToRemove = wall;
                }
            }
        }

        // removes wall
        if (wallToRemove != null)
        {
            walls.Remove(wallToRemove);
            Destroy(wallToRemove);
        }
    }
    void CreateStartAndEndMarkers()
    {
        startMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);


        // Position it at the start cell center
        startMarker.transform.position = new Vector3(0, 0, 0);
        startMarker.transform.localScale = new Vector3(wallSize * 0.5f, wallSize * 0.5f, wallSize * 0.5f);

        // green = start
        startMarker.GetComponent<Renderer>().material.color = Color.green;

        // child of mazegen
        startMarker.transform.parent = transform;

        endMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Position it at the end cell center (opposite)
        endMarker.transform.position = new Vector3((width - 1) * wallSize, 0, (height - 1) * wallSize);
        endMarker.transform.localScale = new Vector3(wallSize * 0.5f, wallSize * 0.5f, wallSize * 0.5f);

        // red 
        endMarker.GetComponent<Renderer>().material.color = Color.red;
        endMarker.transform.parent = transform;
        Destroy(endMarker.GetComponent<Collider>());
        Destroy(startMarker.GetComponent<Collider>());
    }
}