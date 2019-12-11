﻿using System.Net.NetworkInformation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class TutorialScript : MonoBehaviour
{
//booleans
    private bool isFirstDrop;
    private bool areGemmsFalling;
    private bool isGemmSelected;
    private bool isGemmCloneAlive;
    private bool canGemmFollowMe;
    private bool isRotating;
    private bool wantGemmDrop;
    private bool isMatching;
    private bool didDestroy;
    private bool canContinueMatching;
    private bool isGameOver;
    private bool isWin;
    private bool noMoves;
    private bool showUndo;
    private bool canUndo;
    private bool wantsUndo;

//serial values
    [SerializeField]
    private int boardDimX;
    [SerializeField]
    private int boardDimY;
    [SerializeField]
    private int dropOffset;
    [SerializeField]
    private int totalRounds;
    [SerializeField]
    private int movesPerRound;
    [SerializeField]
    private int goalNumCyan;
    [SerializeField]
    private int goalNumGreen;
    [SerializeField]
    private int goalNumRed;
    [SerializeField]
    private float fallTimeInterval;
    [SerializeField]
    private float fallPercentIncrease;
    [SerializeField]
    private float underlayAlpha;
    [SerializeField]
    private float overlayAlpha;
    [SerializeField]
    private float moveSpeed;
    [SerializeField]
    private float rotationTimeInterval;
    [SerializeField]
    private float rotatePercentIncrease;
    [SerializeField]
    private float fadeStep;
    
//nonserial values
    private int currNumRounds;
    private int currNumMoves;
    private int redsRemaining;
    private int greensRemaining;
    private int cyansRemaining;

//other serialized
    [SerializeField]
    private List<GameObject> GemmOptions;
    [SerializeField]
    private GameObject undoButton;
    [SerializeField]
    private GameObject roundsButton;

//other nonserialized
    private Gemm[,] GemmGridLayout;
    private Gemm[,] GemmGridLayoutCopy;
    private struct Gemm
    {
        public GameObject gemmGObj;
        public string tagId;
        public bool horizVisited;
        public bool vertVisited;
        public bool floodVisited;
        public bool floodMatched;
        public bool destroyed;
        public int dropDist;
        public int gridXLoc;
        public int gridYLoc;
    }
    private struct GemmLoc
    {
        public int gridXLoc;
        public int gridYLoc;
    }
    private Ray touchPos;
    private Vector2Int prevActiveTouchPos;
    private Vector3 rotationAngle;
    private Gemm gemmClone;
    private Dictionary<GemmLoc, Gemm> FloodMatchDict;
    private Dictionary<GemmLoc, Gemm> GemmDictToDestroy;
    private List<GemmLoc> Check3List;


    // declare inits and get script references
    void Awake()
    {
        GemmGridLayout = new Gemm[boardDimX, boardDimY];
        GemmGridLayoutCopy = new Gemm[boardDimX, boardDimY];
        FloodMatchDict = new Dictionary<GemmLoc, Gemm>();
        GemmDictToDestroy = new Dictionary<GemmLoc, Gemm>();
        Check3List = new List<GemmLoc>();
        
        isMatching = false;
        areGemmsFalling = false;
        isFirstDrop = true;
        canUndo = false;
        showUndo = false;
        wantsUndo = false;

        cyansRemaining = goalNumCyan;
        greensRemaining = goalNumGreen;
        redsRemaining = goalNumRed;
        currNumMoves = movesPerRound;
        currNumRounds = 1;

        SwapUndoStates();
    }

    private void SwapUndoStates()
    {
        showUndo = !showUndo;
        undoButton.SetActive(!showUndo);
        roundsButton.SetActive(showUndo);
    }

    private void GameOver()
    {
        SceneManager.LoadSceneAsync(3);
    }

    void Start()
    {
        // GameEventsScript.gameIsOver.AddListener(GameOver);
        GameEventsScript.undoOnOff.AddListener(DoUndo);
        GameEventsScript.countRound.Invoke(new GameEventsScript.CountRoundsData(currNumRounds, totalRounds));
        GameEventsScript.clearGems.Invoke(new GameEventsScript.DestroyedGemsData(cyansRemaining, greensRemaining, redsRemaining));
        GameEventsScript.countMove.Invoke(new GameEventsScript.CountMoveData(currNumMoves, movesPerRound));

        StartCoroutine(SetupTutorialBoard());
    }

    private void DoUndo()
    {
        wantsUndo = !wantsUndo;
        if(wantsUndo)
        {
            // Debug.Log("CAN undo");
        } else{
            // Debug.Log("CAN'T undo");
        }
    }

    void Update()
    {
        if (isFirstDrop)
        {
            return;
        }
        if (isMatching)
        {
            return;
        }
        if (areGemmsFalling)
        {
            return;
        }
        if(isGameOver)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("make a copy of the board");
            GemmGridLayoutCopy = GemmGridLayout.Clone() as Gemm[,];
            checkGridState();
            //copy Board
            touchPos = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (Mathf.RoundToInt(touchPos.origin.x) < boardDimX && Mathf.RoundToInt(touchPos.origin.x) > -1 && Mathf.RoundToInt(touchPos.origin.y) < boardDimY && Mathf.RoundToInt(touchPos.origin.y) > -1)
            {
                if(!canUndo)
                {
                    SwapUndoStates();
                    canUndo = true;
                }
                isGemmSelected = true;
                DisplayGemmClone(touchPos.origin);
            }
        // }
        } else if (Input.GetMouseButton(0))
        {
            touchPos = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Mathf.RoundToInt(touchPos.origin.x) < boardDimX && Mathf.RoundToInt(touchPos.origin.x) > -1 && Mathf.RoundToInt(touchPos.origin.y) < boardDimY && Mathf.RoundToInt(touchPos.origin.y) > -1)
            {
                canGemmFollowMe = true;
            }

            // if(EventSystem.current.IsPointerOverGameObject())
            // {
            //     // Debug.Log("Current game object hover: " + EventSystem.current.RaycastAll);
            // }
        // } 
        } else if (Input.GetMouseButtonUp(0))
        {
            //Swapping out red undo button
            if(canUndo)
            {
                SwapUndoStates();
                canUndo = false;
            }
            //perform undo on board
            if(wantsUndo)
            {
                DropGemm();
                //revert board state
                Debug.Log("BEFORE\n");
                checkGridState();
                Debug.Log("COPY\n");
                checkGridStateCopy();
                Debug.Log("AFTER\n");
                GemmGridLayout = GemmGridLayoutCopy;
                checkGridState();
                Debug.Log("undo board state");
                ClearGridLayout();
                Debug.Log("CLEARED");
                RemakeGemmsForUndo();
                Debug.Log("REMAKE GEMMS");
                currNumMoves = movesPerRound;
                GameEventsScript.countMove.Invoke(new GameEventsScript.CountMoveData(currNumMoves, movesPerRound));
            } else
            {
                //continue with board state
                wantGemmDrop = true;
            }
            
            isGemmSelected = canGemmFollowMe = wantsUndo = false;
        }
    }

    void FixedUpdate()
    {
        if (isFirstDrop)
        {
            return;
        }
        if (areGemmsFalling)
        {
            return;
        }
        if (isMatching)
        {
            return;
        }
        if(isGameOver)
        {
            return;
        }
        if(isGemmSelected && canGemmFollowMe)
        {
            //moves clone and and gemm to correct pos; locks last move
            if(isGemmCloneAlive && !noMoves)
            {
                gemmClone.gemmGObj.transform.Translate((touchPos.origin - gemmClone.gemmGObj.transform.position) * Time.fixedDeltaTime * moveSpeed);
                ShowGemmMovement(touchPos.origin);
                if (currNumMoves<=0)
                {
                    gemmClone.gemmGObj.SetActive(false);
                    noMoves = true;
                }
            } else if (currNumMoves > 0)
            {
                noMoves = false;
            }
        }
        if (wantGemmDrop)
        {
            DropGemm();
            StartCoroutine(MatchGemms());
        }
    }

    //Wrapper for creating initial board
    IEnumerator SetupTutorialBoard()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        MakeGemmsInGrid();
        MoveGemmsDown();
        isFirstDrop = false;
    }

    //Create Gemms in Grid
    private void MakeGemmsInGrid()
    {
        for (int y = 0; y < boardDimY; y++)
        {
            for (int x = 0; x < boardDimX; x++)
            {
                int z = x + 2;
                GameObject gemm;

                //if rows 3/6, shift gem to the left by 1 color
                if (y % GemmOptions.Count == GemmOptions.Count - 1)
                {
                    gemm = GemmOptions[z%GemmOptions.Count];
                } else 
                {
                    gemm = GemmOptions[x%GemmOptions.Count];
                }
                MakeGemm(gemm, x, y);
            }
        }
    }

    //Instantiate Gemm
    private void MakeGemm(GameObject gemmGObj, int x, int y)
    {
        GameObject currentGemm = (GameObject)Instantiate(gemmGObj, new Vector2((float)x, (float)y + (float)dropOffset), Quaternion.identity);
        currentGemm.transform.parent = transform;
        GemmGridLayout[x, y] = new Gemm {
            gemmGObj = currentGemm,
            tagId = currentGemm.tag,
            floodVisited = false,
            floodMatched = false,
            destroyed = false,
            dropDist = dropOffset,
            gridXLoc = x,
            gridYLoc = y,
        };
    }

    //Wrapper to move Gemms down to be in the screen
    private void MoveGemmsDown()
    {
        for(int y = 0; y < boardDimY; y++)
        {
            for(int x = 0; x < boardDimX; x++)
            {
                if(GemmGridLayout[x, y].dropDist > 0)
                {
                    Vector2 startPos = GemmGridLayout[x, y].gemmGObj.transform.position;
                    Vector2 endPos = new Vector2(GemmGridLayout[x, y].gemmGObj.transform.position.x, GemmGridLayout[x, y].gemmGObj.transform.position.y - GemmGridLayout[x, y].dropDist);
                    StartCoroutine(MoveGemsDownEnum(GemmGridLayout[x, y], startPos, endPos));
                    GemmGridLayout[x, y].dropDist = 0;
                }
            }
        }
    }

    //animate move Gemms down
    IEnumerator MoveGemsDownEnum(Gemm gemm, Vector2 start, Vector2 end)
    {
        areGemmsFalling = true;
        float fallPercent = 0.0f;
        while(fallPercent <= 1.0f)
        {
            gemm.gemmGObj.transform.position = Vector2.Lerp(start, end, fallPercent);
            fallPercent += fallPercentIncrease;
            yield return new WaitForSeconds(fallTimeInterval);
        }
        gemm.gemmGObj.transform.position = end;
        areGemmsFalling = false;
    }

    //Wrapper for showing gemm clone
    private void DisplayGemmClone(Vector2 touchPos)
    {
        int gridXPos = GetPosOnGrid(touchPos.x, boardDimX);
        int gridYPos = GetPosOnGrid(touchPos.y, boardDimY);
        prevActiveTouchPos = new Vector2Int(gridXPos, gridYPos);

        //get Gem in grid
        Gemm selectedGem = GemmGridLayout[gridXPos, gridYPos];
        //create Gem Clone at same location
        MakeGemmClone(selectedGem, gridXPos, gridYPos);
        //change its alpha
        selectedGem.gemmGObj.GetComponent<SpriteRenderer>().color = ChangeGemmAlpha(selectedGem, underlayAlpha);
    }

    private int GetPosOnGrid(float main, int size)
    {
        int coordinate = Mathf.RoundToInt(main);
        if (coordinate < 0)
        {
            coordinate = 0;
        }
        if (coordinate > size - 1)
        {
            coordinate = size - 1;
        }
        return coordinate;
    }

    //makes gemm clone to display movement
    private void MakeGemmClone (Gemm origGemm, int x, int y)
    {
        gemmClone = new Gemm {
            gemmGObj = (GameObject)Instantiate(origGemm.gemmGObj, new Vector2(x, y), Quaternion.identity),
            floodVisited = false,
            floodMatched = false,
            destroyed = false,
            dropDist = 0,
            gridXLoc = x,
            gridYLoc = y,
        };
        gemmClone.gemmGObj.GetComponent<SpriteRenderer>().color = ChangeGemmAlpha(gemmClone, overlayAlpha);
        isGemmCloneAlive = true;
    }

    //adjusts gemm transparency
    private Color ChangeGemmAlpha(Gemm gemm, float aVal)
    {
        Color gemmColor = gemm.gemmGObj.GetComponent<SpriteRenderer>().color;
        gemmColor.a = aVal;
        return gemmColor;
    }

    //wrapper for gem swap coroutine
    private void ShowGemmMovement(Vector2 touchPos)
    {
        int gridXPos = GetPosOnGrid(touchPos.x, boardDimX);
        int gridYPos = GetPosOnGrid(touchPos.y, boardDimY);

        //Updates gem movement when finger moves to new cell and as fast as the rotation happens.
        if ((prevActiveTouchPos.x != gridXPos || prevActiveTouchPos.y != gridYPos) && !isRotating)
        {
            //diagonals
            if(gridXPos - prevActiveTouchPos.x > 0 && gridYPos - prevActiveTouchPos.y > 0)
            {
                gridXPos = prevActiveTouchPos.x + 1;
                gridYPos = prevActiveTouchPos.y + 1;
                StartCoroutine(ShowGemmMovementEnum(gridXPos, gridYPos));
            } else if (gridXPos - prevActiveTouchPos.x > 0 && gridYPos - prevActiveTouchPos.y < 0)
            {
                gridXPos = prevActiveTouchPos.x + 1;
                gridYPos = prevActiveTouchPos.y - 1;
                StartCoroutine(ShowGemmMovementEnum(gridXPos, gridYPos));
            } else if (gridXPos - prevActiveTouchPos.x < 0 && gridYPos - prevActiveTouchPos.y < 0)
            {
                gridXPos = prevActiveTouchPos.x - 1;
                gridYPos = prevActiveTouchPos.y - 1;
                StartCoroutine(ShowGemmMovementEnum(gridXPos, gridYPos));
            } else if (gridXPos - prevActiveTouchPos.x < 0 && gridYPos - prevActiveTouchPos.y > 0)
            {
                gridXPos = prevActiveTouchPos.x - 1;
                gridYPos = prevActiveTouchPos.y + 1;
                StartCoroutine(ShowGemmMovementEnum(gridXPos, gridYPos));
            
            //cardinal directions
            } else if(gridXPos - prevActiveTouchPos.x > 0)
            {
                gridXPos = prevActiveTouchPos.x + 1;
                StartCoroutine(ShowGemmMovementEnum(gridXPos, prevActiveTouchPos.y));
            } else if (gridXPos - prevActiveTouchPos.x < 0)
            {
                gridXPos = prevActiveTouchPos.x - 1;
                StartCoroutine(ShowGemmMovementEnum(gridXPos, prevActiveTouchPos.y));
            } else if (gridYPos - prevActiveTouchPos.y > 0)
            {
                gridYPos = prevActiveTouchPos.y + 1;
                StartCoroutine(ShowGemmMovementEnum(prevActiveTouchPos.x, gridYPos));
            } else if (gridYPos - prevActiveTouchPos.y < 0)
            {
                gridYPos = prevActiveTouchPos.y - 1;
                StartCoroutine(ShowGemmMovementEnum(prevActiveTouchPos.x, gridYPos));
            }
        } 
    }

    //shows rotation speed of gemm movement
    IEnumerator ShowGemmMovementEnum(int currTouchPosX, int currTouchPosY)
    {
        if(currNumMoves > 0)
        {
            currNumMoves--;
        }
        GameEventsScript.countMove.Invoke(new GameEventsScript.CountMoveData(currNumMoves, movesPerRound));
        
        //inits
        isRotating = true;
        float rotatePercent = 0.0f;
        rotationAngle = new Vector3(0, 0, 180.0f);
        GameObject gemRotator = new GameObject();
        gemRotator.transform.position = new Vector2 ((float)prevActiveTouchPos.x - (float)(prevActiveTouchPos.x - currTouchPosX)/2.0f, (float)prevActiveTouchPos.y - (float)(prevActiveTouchPos.y - currTouchPosY)/2.0f);

        // update involved gems's parent to gem Rotator
        GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y].gemmGObj.transform.Rotate(0.0f, 0.0f, 180.0f, Space.Self);        
        GemmGridLayout[currTouchPosX, currTouchPosY].gemmGObj.transform.Rotate(0.0f, 0.0f, 180.0f, Space.Self);
        GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y].gemmGObj.transform.parent = GemmGridLayout[currTouchPosX, currTouchPosY].gemmGObj.transform.parent = null;
        GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y].gemmGObj.transform.parent = GemmGridLayout[currTouchPosX, currTouchPosY].gemmGObj.transform.parent = gemRotator.transform;

        //rotate to desired positions
        while(rotatePercent <= 1.0f)
        {
            gemRotator.transform.eulerAngles = Vector3.Lerp(Vector3.zero, rotationAngle, rotatePercent);
            rotatePercent += rotatePercentIncrease;
            yield return new WaitForSeconds(rotationTimeInterval);
        }

        //finalize rotation and movements
        gemRotator.transform.eulerAngles = rotationAngle;

        //reparent/unparent appropriately
        GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y].gemmGObj.transform.parent = GemmGridLayout[currTouchPosX, currTouchPosY].gemmGObj.transform.parent = null;
        GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y].gemmGObj.transform.parent = GemmGridLayout[currTouchPosX, currTouchPosY].gemmGObj.transform.parent = transform;

        //swap old gem and new gem in grid
        Gemm tempGem = GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y];
        GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y] = GemmGridLayout[currTouchPosX, currTouchPosY];
        GemmGridLayout[currTouchPosX, currTouchPosY] = tempGem;

        //update touch position
        prevActiveTouchPos.x = currTouchPosX;
        prevActiveTouchPos.y = currTouchPosY;

        //Cleanup
        Destroy(gemRotator);
        isRotating = false;
    }

    //ends player turn
    private void DropGemm()
    {
        GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y].gemmGObj.GetComponent<SpriteRenderer>().color = ChangeGemmAlpha(GemmGridLayout[prevActiveTouchPos.x, prevActiveTouchPos.y], 1.0f);
        if (gemmClone.gemmGObj != null)
        {
            Destroy(gemmClone.gemmGObj);
        }
        wantGemmDrop = false;
        isGemmCloneAlive = false;
    }

    //Matches gemms; looks for 3 gemms or more in a row
    IEnumerator MatchGemms()
    {
        // Debug.Log("Start Match");
        ResetBoardForMatching();
        isMatching = true;
        for (int y = 0; y < boardDimY; y++)
        {
            for (int x = 0; x < boardDimX; x++)
            {
                // Debug.Log("CURRENT POSITION: " + x + ", " + y);
                if(!GemmGridLayout[x,y].destroyed)
                {
                    ResetBoardForFloodMarking();
                    FloodMark(x, y, GemmGridLayout[x, y].gemmGObj.tag);
                    Check3PlusInDirectionWrapper(1, 0);
                    Check3PlusInDirectionWrapper(0, 1);
                    CountAndDestroyGems();
                    if(didDestroy)
                    {
                        GameEventsScript.clearGems.Invoke(new GameEventsScript.DestroyedGemsData(cyansRemaining, greensRemaining, redsRemaining));
                        //update destroyed #
                        //if u destroyed shit, you should wait
                        yield return new WaitForSeconds(0.25f);
                        didDestroy = false;
                    }
                } else
                {
                    //don't do anything
                }
            }
        }
        //looped through everything
        
        SetupRemainingGemmsForDrop();
        RemakeDestroyedGemms();
        MoveGemmsDown();
//BUG AREA
        yield return new WaitUntil(() => !areGemmsFalling);
//         Debug.Log("done moving");
//         checkGridState();
// //check list of boolean states?
//         Debug.Log(canContinueMatching);
//         Debug.Log(isMatching);

        if(canContinueMatching)
        {
            canContinueMatching = false;
            StartCoroutine(RepeatMatchGemms());
        } else
        {
            isMatching = false;
            currNumMoves = movesPerRound;
            GameEventsScript.countMove.Invoke(new GameEventsScript.CountMoveData(currNumMoves, movesPerRound));
            currNumRounds++;
            GameEventsScript.countRound.Invoke(new GameEventsScript.CountRoundsData(currNumRounds, totalRounds));
            if (redsRemaining <= 0 && greensRemaining <= 0 && cyansRemaining <=0)
            {
                isGameOver = true;
                isWin = true;
            } else if (currNumRounds > totalRounds)
            {
                isGameOver = true;
                isWin = false;
            }
        
        }


        //determine win/lose
        //numGemsDestroyed
        // if(currNumRounds > totalRounds)
        // {
        //     isGameOver = true;
        //     //determine win or lose
        // }
        //can say round ends
        yield return new WaitForSeconds(1.0f);
        if (isGameOver)
        {
            GameEventsScript.gameIsOver.Invoke(new GameEventsScript.GameOverData(isWin));
        }
    }

    private void ResetBoardForMatching()
    {
        for (int y =0; y < boardDimY; y++)
        {
            for (int x =0; x < boardDimX; x++)
            {
                GemmGridLayout[x,y].tagId = GemmGridLayout[x,y].gemmGObj.tag;
                GemmGridLayout[x,y].horizVisited = false;
                GemmGridLayout[x,y].vertVisited = false;
                GemmGridLayout[x,y].floodVisited = false;
                GemmGridLayout[x,y].floodMatched = false;
                GemmGridLayout[x,y].destroyed = false;
                GemmGridLayout[x,y].dropDist = 0;
                GemmGridLayout[x,y].gridXLoc = x;
                GemmGridLayout[x,y].gridYLoc = y;               
            }
        }
    }

    private void ResetBoardForFloodMarking()
    {
        for (int y =0; y < boardDimY; y++)
        {
            for (int x =0; x < boardDimX; x++)
            {
                GemmGridLayout[x,y].floodVisited = false;
                GemmGridLayout[x,y].floodMatched = false;             
            }
        }
    }

    private void FloodMark(int x, int y, string origTag)
    {
        //assume this ain't destroyed.
        //create ToDo List
        Stack gemmStack = new Stack();
        gemmStack.Push(GemmGridLayout[x,y]);
        while (gemmStack.Count > 0)
        {
            Gemm gemmLFM = (Gemm) gemmStack.Pop();
            GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc].floodVisited = true;
            if (gemmLFM.gemmGObj.tag == origTag)
            {
                GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc].floodMatched = true;
                //create key to add to running list
                GemmLoc key = new GemmLoc {
                    gridXLoc = gemmLFM.gridXLoc,
                    gridYLoc = gemmLFM.gridYLoc,
                };
                if(!FloodMatchDict.ContainsKey(key))
                {
                    FloodMatchDict.Add(key, GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc]);
                }
                //add appropriate cells around current to the ToDoList
                if (gemmLFM.gridXLoc > 0)
                {
                    if (!GemmGridLayout[gemmLFM.gridXLoc - 1, gemmLFM.gridYLoc].floodVisited && !GemmGridLayout[gemmLFM.gridXLoc - 1, gemmLFM.gridYLoc].destroyed)
                    {
                        gemmStack.Push(GemmGridLayout[gemmLFM.gridXLoc - 1, gemmLFM.gridYLoc]);
                    }
                }
                if (gemmLFM.gridXLoc < boardDimX - 1)
                {
                    if (!GemmGridLayout[gemmLFM.gridXLoc + 1, gemmLFM.gridYLoc].floodVisited && !GemmGridLayout[gemmLFM.gridXLoc + 1, gemmLFM.gridYLoc].destroyed)
                    {
                        gemmStack.Push(GemmGridLayout[gemmLFM.gridXLoc + 1, gemmLFM.gridYLoc]);
                    }
                }
                if (gemmLFM.gridYLoc > 0)
                {
                    if (!GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc - 1].floodVisited && !GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc - 1].destroyed)
                    {
                        gemmStack.Push(GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc - 1]);
                    }
                }
                if (gemmLFM.gridYLoc < boardDimY - 1)
                {
                    if (!GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc + 1].floodVisited && !GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc + 1].destroyed)
                    {
                        gemmStack.Push(GemmGridLayout[gemmLFM.gridXLoc, gemmLFM.gridYLoc + 1]);
                    }
                }
            } else 
            {
                // Debug.Log("Skip");
            }
        }
    }

    //wrapper for check 3 plus gemms in a row method. uses horiz and vert int to indicate whether searching horizontal or vertical matches
    //if the gemm's been visited, skip over that iteration. it's been accounted for.
    private void Check3PlusInDirectionWrapper(int horiz, int vert)
    {
        foreach(var gemm in FloodMatchDict)
        {
            Check3PlusInDirection(gemm.Key, horiz, vert);
        }
    }

    //Q each candidate for horiz/vert 3 in a row 1 at a time.
    //if match, add to temp; else see if temp has >=3
    //if so, record for destroying, otherwise clear temp.
    private void Check3PlusInDirection(GemmLoc gemmKey, int horiz, int vert)
    {
        Queue Check3Q = new Queue();
        Check3Q.Enqueue(gemmKey);
        while(Check3Q.Count > 0)
        {
            GemmLoc key = (GemmLoc) Check3Q.Dequeue();
            //if gemm to right/up is not in match blob, not a 3 in a row
            if (FloodMatchDict.ContainsKey(key))
            {
                Check3List.Add(key);
                GemmLoc newKey = new GemmLoc {
                    gridXLoc = key.gridXLoc + horiz,
                    gridYLoc = key.gridYLoc + vert,
                };
                Check3Q.Enqueue(newKey);
            } else
            {
                if(Check3List.Count >= 3)
                {
                    for(int i = 0; i < Check3List.Count; i++)
                    {
                        if(!GemmDictToDestroy.ContainsKey(Check3List[i]))
                        {
                            GemmDictToDestroy.Add(Check3List[i], GemmGridLayout[Check3List[i].gridXLoc, Check3List[i].gridYLoc]);
                        }
                    }
                }
                Check3List.Clear();
                break;
            }
        }
    }

    private void CountAndDestroyGems()
    {
        CountDestroyedGems();
        DestroyGems();
    }

    private void CountDestroyedGems()
    {
        foreach (var gemm in GemmDictToDestroy)
        {
            if(gemm.Value.gemmGObj.tag == "Red")
            {
                if(redsRemaining > 0)
                {
                    redsRemaining--;
                }
            } else if (gemm.Value.gemmGObj.tag == "Green")
            {
                if(greensRemaining > 0)
                {
                    greensRemaining--;
                }
            } else if (gemm.Value.gemmGObj.tag == "Cyan")
            {
                if(cyansRemaining > 0)
                {
                    cyansRemaining--;
                }
            }
        }
    }

    private void DestroyGems()
    {
        // Debug.Log("# Gems to destroy: " + GemmDictToDestroy.Count);
        foreach (var gemm in GemmDictToDestroy)
        {
            int a = gemm.Value.gridXLoc;
            int b = gemm.Value.gridYLoc;
            if (!GemmGridLayout[a, b].destroyed)
            {
                GemmGridLayout[a, b].destroyed = true;
                StartCoroutine(fadeGem(gemm.Value.gemmGObj, 1.0f));
                //do fade and wait
                // Destroy(gemm.Value.gemmGObj);
                didDestroy = true;
                canContinueMatching = true;
                // ParticleSystem.i
            }
        }
        // Debug.Log("redsRemaining: " + redsRemaining);
        // Debug.Log("greensRemaining: " + greensRemaining);
        // Debug.Log("cyansRemaining: " + cyansRemaining);
        FloodMatchDict.Clear();
        GemmDictToDestroy.Clear();
    }

    IEnumerator fadeGem(GameObject gemm, float fadeDuration)
    {
        SpriteRenderer sr = gemm.GetComponent<SpriteRenderer>();
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            Color c = sr.color;
            Color fine = sr.color;
            c.a = c.a - fadeStep;
            sr.color = c;
           
            yield return null;
 
            if (sr.color.a <= 0f)
            {
                Destroy(gemm);
                break;
            }
 
        }
    }

    //if gemms were deleted, drop remaining gems to fill in gaps
    private void SetupRemainingGemmsForDrop()
    {
        for (int y = 1; y < boardDimY; y++)
        {
            for (int x = 0; x < boardDimX; x++)
            {
                if (!GemmGridLayout[x, y].destroyed)
                {
                    int dropDistance = 0;
                    for (int i = 1; i <= y; i++)
                    {
                        if (GemmGridLayout[x, y - i].destroyed)
                        {
                            dropDistance++;
                        }
                    }
                    if (dropDistance > 0)
                    {
                        Gemm temp = GemmGridLayout[x, y - dropDistance];
                        GemmGridLayout[x, y - dropDistance] = GemmGridLayout[x, y];
                        GemmGridLayout[x, y - dropDistance].dropDist = dropDistance;
                        GemmGridLayout[x, y] = temp;
                    }
                }
            }
        }
    }

    //Remake/Replace destroyed gemms
    private void RemakeDestroyedGemms()
    {
        for (int y = 0; y < boardDimY; y++)
        {
            for (int x = 0; x < boardDimX; x++)
            {
                if (GemmGridLayout[x,y].destroyed)
                {
                    GameObject newGemm = GemmOptions[UnityEngine.Random.Range(0, GemmOptions.Count)];
                    MakeGemm(newGemm, x, y);
                }
            }
        }
    }

    IEnumerator RepeatMatchGemms()
    {
        yield return new WaitUntil(() => !areGemmsFalling);
        // checkGridState();
        StartCoroutine(MatchGemms());
    }

    private void ClearGridLayout()
    {
        for(int y = 0; y < boardDimY; y++)
        {
            for(int x = 0; x < boardDimX; x++)
            {
                if (GemmGridLayout[x,y].gemmGObj != null)
                {
                    Destroy(GemmGridLayout[x,y].gemmGObj);
                }
            }
        }
    }

    private void RemakeGemmsForUndo()
    {
        for(int y = 0; y < boardDimY; y++)
        {
            for(int x = 0; x < boardDimX; x++)
            {
                for(int z = 0; z < GemmOptions.Count; z++)
                {
                    if (GemmGridLayout[x,y].tagId == GemmOptions[z].tag)
                    {
                        // GameObject gObj = GemmOptions[z];
                        GameObject gObj = (GameObject) Instantiate(GemmOptions[z], new Vector2((float)x, (float)y), Quaternion.identity);
                        gObj.transform.parent = transform;
                        GemmGridLayout[x,y].gemmGObj = gObj;
                        break;
                    }
                }
            }
        } 
    }

    //Debug function for diff attributes
    private void checkGridState()
    {
        for (int y = 0; y < boardDimY; y++)
        {
            for (int x = 0; x < boardDimX; x++)
            {
                Debug.Log("Coordinates: [" + x + ", " + y + "]" + "\n" + 
                "[DropDistance, gridX, gridY]: [" + GemmGridLayout[x, y].dropDist + ", " + GemmGridLayout[x,y].gridXLoc + ", " + GemmGridLayout[x, y].gridYLoc + "]");
                Debug.Log("Color: " + GemmGridLayout[x, y].gemmGObj.tag + ": " + GemmGridLayout[x,y].tagId + "\n" +
                "World Space: " + GemmGridLayout[x, y].gemmGObj.transform.position);
                Debug.Log("Matched: " + GemmGridLayout[x, y].floodMatched + "\n" +
                "Visited: " + GemmGridLayout[x,y].floodVisited);
            }
        }
    }
    private void checkGridStateCopy()
    {
        for (int y = 0; y < boardDimY; y++)
        {
            for (int x = 0; x < boardDimX; x++)
            {
                Debug.Log("Coordinates: [" + x + ", " + y + "]" + "\n" + 
                "[DropDistance, gridX, gridY]: [" + GemmGridLayoutCopy[x, y].dropDist + ", " + GemmGridLayoutCopy[x,y].gridXLoc + ", " + GemmGridLayoutCopy[x, y].gridYLoc + "]");
                Debug.Log("Color: " + GemmGridLayoutCopy[x, y].gemmGObj.tag + ": " + GemmGridLayoutCopy[x,y].tagId + "\n" +
                "World Space: " + GemmGridLayoutCopy[x, y].gemmGObj.transform.position);
                Debug.Log("Matched: " + GemmGridLayoutCopy[x, y].floodMatched + "\n" +
                "Visited: " + GemmGridLayoutCopy[x,y].floodVisited);
            }
        }
    }
}