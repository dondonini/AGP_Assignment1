using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour {

    public int gridSizeX = 16;
    public int gridSizeY = 16;
    public int placementMin = 6;
    public int placementMax = 8;

    public GameObject oreBlock;
    public GameObject grassLayer;
    public Material[] oreMaterials = new Material[System.Enum.GetValues(typeof(GameInfo.OreValue)).Length];

    private Vector3 offset = Vector3.zero;
    private GameObject[,] oreArray;
    private GameObject map;
    private List<Vector2> highValuePos;

    private const float GRASS_YOFFSET = 0.55f;

    void Awake()
    {
        ///////////////////
        // Determind offset
        ///////////////////
        offset.x -= (gridSizeX / 2.0f) - 0.5f;
        offset.y = 0;
        offset.z += (gridSizeY / 2.0f) - 0.5f;

        //////////////////////
        // Build array and map
        //////////////////////

        // Allocating array
        oreArray = new GameObject[gridSizeX,gridSizeY];

        // Creating map "bag"
        map = new GameObject();
        map.name = "Map";

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                GameObject newBlock = Instantiate(oreBlock) as GameObject;

                // Set block position
                newBlock.transform.position = offset + new Vector3(x, 0, -y);

                // Set block position in array
                oreArray[x, y] = newBlock;

                // Set block as child of Map
                newBlock.transform.SetParent(map.transform);

            }
        }

        /////////////////////////////////
        // Generate random ore placements
        /////////////////////////////////
        highValuePos = new List<Vector2>();

        int randAmount = Random.Range(placementMin, placementMax);

        PlaceHighValueOres(randAmount);

        PlaceLowerValueOres();

        UpdateBlockVisuals();

        //////////////////
        // Grass placement
        //////////////////
        for (int x = 0; x < oreArray.GetLength(0); x++)
        {
            for (int y = 0; y < oreArray.GetLength(1); y++)
            {
                GameObject newGrass = Instantiate(grassLayer) as GameObject;

                // Place grass on top of all ore blocks
                newGrass.transform.position = oreArray[x, y].transform.position + new Vector3(0, GRASS_YOFFSET, 0);
                newGrass.transform.SetParent(map.transform);
            }
        }
    }

    // Use this for initialization
    void Start () {
	
        
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    /// <summary>
    /// Updates block visuals
    /// </summary>
    void UpdateBlockVisuals()
    {
        // Scans all blocks
        for (int x = 0; x < oreArray.GetLength(0); x++)
        {
            for (int y = 0; y < oreArray.GetLength(1); y++)
            {
                GameObject newBlock = oreArray[x, y];

                // Updates block material based on value
                GameInfo.OreValue blockValue = newBlock.GetComponent<BlockInfo>().blockValue;
                newBlock.GetComponent<Renderer>().material = oreMaterials[(int)blockValue];
            }
        }
    }

    /// <summary>
    /// High value ore placement
    /// </summary>
    /// <param name="totalLeft">Total ores to be placed</param>
    void PlaceHighValueOres(int totalLeft)
    {
        // Exit
        if (totalLeft <= 0)
        {
            return;
        }

        // Random position
        int randXPos = Random.Range(0, gridSizeX - 1);
        int randYPos = Random.Range(0, gridSizeY - 1);

        // Current block getting examined
        GameObject examineBlock = oreArray[randXPos, randYPos];

        // Checks if block is not empty
        if (examineBlock.GetComponent<BlockInfo>().blockValue != GameInfo.OreValue.None)
        {
            // Recursion
            PlaceHighValueOres(totalLeft);
        }
        else
        {
            // Set empty block as full
            examineBlock.GetComponent<BlockInfo>().blockValue = GameInfo.OreValue.Full;

            // Save high value position for future use
            highValuePos.Add(new Vector2(randXPos, randYPos));

            // Recursion
            PlaceHighValueOres(totalLeft - 1);
        }
    }

    /// <summary>
    /// Half and quarter value placement
    /// </summary>
    void PlaceLowerValueOres()
    {
        for (int i = 0; i < highValuePos.Count; i++)
        {
            // Current pos
            Vector2 pos = highValuePos[i];

            ///////////////////////
            // Half value placement
            ///////////////////////
            for (int hX = (int)pos.x - 1; hX < (int)pos.x + 2; hX++)
            {
                for (int hY = (int)pos.y - 1; hY < (int)pos.y + 2; hY++)
                {
                    // If position is in array index bounds
                    if ((hX >= 0 && hY >= 0) && (hX <= gridSizeX - 1 && hY <= gridSizeX - 1))
                    {
                        GameObject examineBlock = oreArray[hX, hY];

                        switch (examineBlock.GetComponent<BlockInfo>().blockValue)
                        {
                            // Skip full value blocks
                            case GameInfo.OreValue.Full:
                                break;
                            default:
                                examineBlock.GetComponent<BlockInfo>().blockValue = GameInfo.OreValue.Half;
                                break;
                        }
                    }
                }
            }

            ///////////////////////
            // Quarter value placement
            ///////////////////////
            for (int qX = (int)pos.x - 2; qX < (int)pos.x + 3; qX++)
            {
                for (int qY = (int)pos.y - 2; qY < (int)pos.y + 3; qY++)
                {
                    // If position is in array index bounds
                    if ((qX >= 0 && qY >= 0) && (qX <= gridSizeX - 1 && qY <= gridSizeX - 1))
                    {
                        GameObject examineBlock = oreArray[qX, qY];

                        switch (examineBlock.GetComponent<BlockInfo>().blockValue)
                        {
                            // Skip full value ores
                            case GameInfo.OreValue.Full:
                                break;
                            // Skip half value ores
                            case GameInfo.OreValue.Half:
                                break;
                            default:
                                examineBlock.GetComponent<BlockInfo>().blockValue = GameInfo.OreValue.Quarter;
                                break;
                        }
                    }
                }
            }
        }
    }
}
