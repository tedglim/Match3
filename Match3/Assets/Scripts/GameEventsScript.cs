﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventsScript : MonoBehaviour
{
    //Event Declarations
    public static UnityEvent undoOnOff = new UnityEvent();
    public static GemsDestroyedEvent clearGems = new GemsDestroyedEvent();
    public static CountRoundEvent countRound = new CountRoundEvent();
    public static CountMoveEvent countMove = new CountMoveEvent();
    public static GameOverEvent gameIsOver = new GameOverEvent();

    //Even Class Declarations
    public class GemsDestroyedEvent: UnityEvent<DestroyedGemsData>{}
    public class CountRoundEvent: UnityEvent<CountRoundsData>{}
    public class CountMoveEvent: UnityEvent<CountMoveData>{}
    public class GameOverEvent: UnityEvent<GameOverData>{}

    //Event Classes
    public class DestroyedGemsData {
        public int cyanCleared;
        public int greenCleared;
        public int redCleared;
        
        public DestroyedGemsData(int cyan, int green, int red)
        {
            this.cyanCleared = cyan;
            this.greenCleared = green;
            this.redCleared = red;
        }
    }

    public class CountRoundsData {
        public int currRound;
        public int totalRounds;
        
        public CountRoundsData(int currRound, int totalRounds)
        {
            this.currRound = currRound;
            this.totalRounds = totalRounds;
        }
    }

    public class CountMoveData {
        public int currMove;
        public int totalMoves;
        
        public CountMoveData(int currMove, int totalMoves)
        {
            this.currMove = currMove;
            this.totalMoves = totalMoves;
        }
    }

    public class GameOverData {

        public bool isWin;

        public GameOverData(bool isWin)
        {
            this.isWin = isWin;
        }
    }

}
