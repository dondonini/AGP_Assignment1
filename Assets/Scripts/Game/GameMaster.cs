using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameMaster : MonoBehaviour {

    public int gridSizeX = 16;
    public int gridSizeY = 16;
    public int placementMin = 6;
    public int placementMax = 8;
    [Tooltip("x*y size")]
    public int revealSize = 3;
    public int scanLimit = 10;
    public int extractLimit = 10;
    public int maxOreValue = 8000;

    [ReadOnly]
    public int halfOreValue = 0;
    [ReadOnly]
    public int quarterOreValue = 0;
    [ReadOnly]
    public int minimumOreValue = 0;


    [ReadOnly]
    public int m_mode = 0;
    [ReadOnly]
    public uint m_score = 0;
    [ReadOnly]
    public uint m_scans = 0;
    [ReadOnly]
    public uint m_extracts = 0;

    public GameObject oreBlock;
    public GameObject grassLayer;
    public GameObject buttonUI;
    public GameObject addPopupText;
    public Material[] oreMaterials = new Material[System.Enum.GetValues(typeof(GameInfo.OreValue)).Length];
    public Canvas blocksGUI;
    public Canvas HUD;
    public Animator scanUI;
    public Animator extractUI;
    public Animator scanAlert;
    public Text scanAmountText;
    public Animator extractAlert;
    public Text extractAmountText;
    public Text scoreText;
    public Animator cameraAnimator;
    public GameObject gameOverScreen;

    private Vector3 offset = Vector3.zero;
    private GameObject[,] blockArray;
    private GameObject[,] buttonArray;
    private GameObject map;
    private List<Vector2> highValuePos;

    private const float GRASS_YOFFSET = 0.55f;
    private const float BUTTON_YOFFSET = 0.7f;
    private const int TOTAL_MODES = 2;

    private void OnValidate()
    {
        ////////////////////////////
        // Auto-fix revealSize value
        ////////////////////////////
        if (revealSize < 1)
        {
            Debug.LogWarning("Reveal size is too low! Setting to 1.");
            revealSize = 1;
        }
        else if (revealSize % 2 == 0)
        {
            Debug.LogWarning("Reveal size is even! Setting to be odd.");
            revealSize += 1;
        }

        ////////////////////
        // Update ore values
        ////////////////////
        halfOreValue = (int)(maxOreValue * 0.5f);
        quarterOreValue = (int)(maxOreValue * 0.25f);
        minimumOreValue = (int)(maxOreValue * (1.0f / Random.Range(8,16)));
    }

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
        blockArray = new GameObject[gridSizeX, gridSizeY];

        // Creating map "bag"
        map = new GameObject();
        map.name = "Map";

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Block segment
                GameObject seg = new GameObject();

                // Block instance
                GameObject newBlock = Instantiate(oreBlock) as GameObject;

                // Set seg position in array
                blockArray[x, y] = seg;

                // Grass instance
                GameObject newGrass = Instantiate(grassLayer) as GameObject;

                // Place grass on top of all ore blocks
                newGrass.transform.position = newGrass.transform.position + new Vector3(0, GRASS_YOFFSET, 0);

                // Set block as child of Map
                newBlock.transform.SetParent(seg.transform);
                newGrass.transform.SetParent(seg.transform);

                // Removing "(Clone)" in instantiated instances
                newBlock.name = oreBlock.name;
                newGrass.name = grassLayer.name;

                // Setting segment name
                seg.name = "Seg(" + x + ", " + y + ")";

                // Setting seg parent as the map
                seg.transform.SetParent(map.transform);

                // Set seg position
                seg.transform.position = offset + new Vector3(x, 0, -y);

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

        /////////////
        // Button UIs
        /////////////

        buttonArray = new GameObject[gridSizeX, gridSizeY];

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Button instance
                GameObject button = Instantiate(buttonUI) as GameObject;

                // Setting button parent
                button.transform.SetParent(blocksGUI.transform);

                buttonArray[x, y] = button;

                button.GetComponent<ButtonHandler>().gm = this;
                button.GetComponent<ButtonHandler>().pos = new Vector2(x, y);

                button.GetComponent<RectTransform>().position = offset + new Vector3(x, BUTTON_YOFFSET, -y);
            }
        }
    }

    // Use this for initialization
    void Start () {

        UpdateAll();

    }
	
	// Update is called once per frame
	void Update () {
	

	}

    /// <summary>
    /// Score
    /// </summary>
    public uint score
    {
        set {
            m_score = (uint)Mathf.Clamp(value, 0, int.MaxValue);
        }

        get {
            return m_score;
        }
    }

    public void NextMode()
    {
        m_mode++;

        if (m_mode >= TOTAL_MODES)
        {
            m_mode = 0;
        }

        cameraAnimator.SetTrigger("Camera360");

        UpdateHUD();

        StartCoroutine(UpdateAllVisualsDelayed(0.5f));
    }

    IEnumerator UpdateAllVisualsDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        UpdateAll();

        //yield return null;
    }

    void UpdateAll()
    {
        UpdateBlockVisuals();
        UpdateButtonVisuals();

        scanAmountText.text = (scanLimit - m_scans).ToString();
        extractAmountText.text = (extractLimit - m_extracts).ToString();


        CheckWinLoseState();

    }

    void UpdateHUD()
    {
        switch (m_mode)
        {
            case 0:
                scanUI.SetTrigger("ScanIn");
                extractUI.SetTrigger("ExtractOut");
                break;
            case 1:
                scanUI.SetTrigger("ScanOut");
                extractUI.SetTrigger("ExtractIn");
                break;
        }
    }

    /// <summary>
    /// Updates block visuals
    /// </summary>
    void UpdateBlockVisuals()
    {
        // Scans all blocks
        for (int x = 0; x < blockArray.GetLength(0); x++)
        {
            for (int y = 0; y < blockArray.GetLength(1); y++)
            {
                GameObject newBlock = blockArray[x, y].transform.FindChild("Ore").gameObject;

                // Updates block material based on value
                GameInfo.OreValue blockValue = newBlock.GetComponent<BlockInfo>().blockValue;
                newBlock.GetComponent<Renderer>().material = oreMaterials[(int)blockValue];
            }
        }
    }

    /// <summary>
    /// Updates button visuals
    /// </summary>
    void UpdateButtonVisuals()
    {
        switch(m_mode)
        {
            case 0:
                for (int x = 0; x < buttonArray.GetLength(0); x++)
                {
                    for (int y = 0; y < buttonArray.GetLength(1); y++)
                    {
                        GameObject currentButton = buttonArray[x, y];

                        if (blockArray[x,y].transform.FindChild("GrassLayer") == null)
                        {
                            currentButton.SetActive(false);
                        }
                        else
                        {
                            currentButton.SetActive(true);
                        }
                    }
                }
                break;

            case 1:
                for (int x = 0; x < buttonArray.GetLength(0); x++)
                {
                    for (int y = 0; y < buttonArray.GetLength(1); y++)
                    {
                        GameObject currentButton = buttonArray[x, y];

                        currentButton.SetActive(false);

                        if (blockArray[x, y].transform.Find("GrassLayer") == null)
                        {
                            if (blockArray[x,y].transform.Find("Ore") != null)
                            {
                                GameObject currentOre = blockArray[x, y].transform.FindChild("Ore").gameObject;
                                    currentButton.SetActive(true);
                            }
                            
                        }
                    }
                }
                break;
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
        GameObject examineBlock = blockArray[randXPos, randYPos].transform.FindChild("Ore").gameObject;

        // Checks if block is not empty
        if (examineBlock.GetComponent<BlockInfo>().blockValue != GameInfo.OreValue.Minimum)
        {
            // Recursion
            PlaceHighValueOres(totalLeft);
        }
        else
        {
            // Set empty block as full
            examineBlock.GetComponent<BlockInfo>().blockValue = GameInfo.OreValue.Max;

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
                        GameObject examineBlock = blockArray[hX, hY].transform.FindChild("Ore").gameObject;

                        switch (examineBlock.GetComponent<BlockInfo>().blockValue)
                        {
                            // Skip full value blocks
                            case GameInfo.OreValue.Max:
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
                        GameObject examineBlock = blockArray[qX, qY].transform.FindChild("Ore").gameObject;

                        switch (examineBlock.GetComponent<BlockInfo>().blockValue)
                        {
                            // Skip full value ores
                            case GameInfo.OreValue.Max:
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

    public void OnMapButtonPress(Vector2 buttonPos)
    {
        Debug.Log("Button pressed at position (" + buttonPos.x + ", " + buttonPos.y + ")");

        switch(m_mode)
        {
            case 0:
                if (m_scans >= scanLimit)
                {
                    scanAlert.SetTrigger("Alert");
                }
                else
                {
                    m_scans++;
                    scanAlert.SetTrigger("Bump");
                    RevealLand(buttonPos);
                }
                break;
            case 1:
                if (m_extracts >= extractLimit)
                {

                }
                else
                {
                    m_extracts++;
                    CollectOre(buttonPos);
                }
                break;
        }

        UpdateAll();
    }

    public void CollectOre(Vector2 collectPos)
    {
        int x = (int)collectPos.x;
        int y = (int)collectPos.y;

        GameObject currentBlock = blockArray[x, y];

        BlockInfo currentOreInfo = currentBlock.transform.Find("Ore").GetComponent<BlockInfo>(); ;

        switch (currentOreInfo.blockValue)
        {
            case GameInfo.OreValue.Max:
                m_score += (uint)maxOreValue;
                AddPopup(maxOreValue);
                break;
            case GameInfo.OreValue.Half:
                m_score += (uint)halfOreValue;
                AddPopup(halfOreValue);
                break;
            case GameInfo.OreValue.Quarter:
                m_score += (uint)quarterOreValue;
                AddPopup(quarterOreValue);
                break;
            case GameInfo.OreValue.Minimum:
                m_score += (uint)minimumOreValue;
                AddPopup(minimumOreValue);
                break;
        }

        currentOreInfo.blockValue = GameInfo.OreValue.Minimum;

        for (int eX = x - (int)((revealSize + 2) / 2 + 0.5f); eX < x + (int)((revealSize + 2) / 2 + 1.5f); eX++)
        {
            for (int eY = y - (int)((revealSize + 2) / 2 + 0.5f); eY < y + (int)((revealSize + 2) / 2 + 1.5f); eY++)
            {
                // If position is in array index bounds
                if ((eX >= 0 && eY >= 0) && (eX <= gridSizeX - 1 && eY <= gridSizeX - 1))
                {
                    if (!blockArray[eX, eY].transform.FindChild("GrassLayer"))
                    {
                        BlockInfo examineBlockInfo = blockArray[eX, eY].GetComponentInChildren<BlockInfo>();

                        if (examineBlockInfo.blockValue != GameInfo.OreValue.Minimum)
                        {
                            examineBlockInfo.blockValue--;
                        }
                    }
                }
            }
        }
    }

    void AddPopup(int addAmount)
    {
        GameObject newPopup = Instantiate(addPopupText) as GameObject;

        Text newText = newPopup.GetComponent<Text>();

        newText.text = "+" + addAmount;

        newPopup.transform.SetParent(HUD.transform);
    }

    void RevealLand(Vector2 revealPos)
    {
        float explosionRadius = revealSize + 5.0f;
        float explosionPower = 1000.0f;

        int x = (int)revealPos.x;
        int y = (int)revealPos.y;

        Debug.Log(offset + new Vector3(x, 0, -y));

        for (int rX = x - (int)(revealSize / 2 + 0.5f); rX < x + (int)(revealSize / 2 + 1.5f); rX++)
        {
            for (int rY = y - (int)(revealSize / 2 + 0.5f); rY < y + (int)(revealSize / 2 + 1.5f); rY++)
            {
                // If position is in array index bounds
                if ((rX >= 0 && rY >= 0) && (rX <= gridSizeX - 1 && rY <= gridSizeX - 1))
                {
                    GameObject currentSeg = blockArray[rX, rY];

                    if (currentSeg.transform.Find("GrassLayer") != null)
                    {
                        GameObject currentGrass = currentSeg.transform.Find("GrassLayer").gameObject;

                        currentGrass.transform.SetParent(null);

                        Rigidbody currentRB;

                        // Set/Get grass to have physics
                        if (currentGrass.GetComponent<Rigidbody>())
                        {
                            currentRB = currentGrass.GetComponent<Rigidbody>();
                        }
                        else
                        {
                            currentRB = currentGrass.AddComponent<Rigidbody>();
                        }

                        currentRB.AddExplosionForce(explosionPower, offset + new Vector3(x, 0, -y), explosionRadius, revealSize);

                        currentGrass.GetComponent<GrassDelete>().DelayDestoryGrass();
                    }
                }
            }
        }
    }

    void CheckWinLoseState()
    {
        int availableOre = 0;

        for (int x = 0; x < blockArray.GetLength(0); x++)
        {
            for (int y = 0; y < blockArray.GetLength(1); y++)
            {
                GameObject currentBlock = blockArray[x, y];

                if (currentBlock.transform.Find("GrassLayer") == null)
                {
                    GameObject currentOre = currentBlock.transform.Find("Ore").gameObject;
                    if (currentOre.GetComponent<BlockInfo>().blockValue != GameInfo.OreValue.Minimum)
                    {
                        availableOre++;
                    }
                }
            }
        }

        if (availableOre == 0 && m_scans >= scanLimit)
        {
            GameOverState();
        }
    }

    void GameOverState()
    {
        Text finalScore = gameOverScreen.transform.Find("FinalScore").GetComponent<Text>();

        finalScore.text = "Score: " + m_score;

        gameOverScreen.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
