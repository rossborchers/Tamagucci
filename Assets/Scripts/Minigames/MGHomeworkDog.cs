using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGHomeworkDog : MiniGame
{
    void OnEnable()
    {
        //StartMinigame
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override MiniGameType GameType => MiniGameType.FeedHomeworkToDog;
}
