using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGFrogger : MiniGame
{
    private const string LeftTrigger = "Jump_Backward";
    private string RightTrigger = "Jump_Forward";
    private const string GameStartRigger = "Restart";
    
    
    private string WinBool = "Win";
    private string DeathBool = "Death";
    private RiveRender Renderer;

    private bool _gameHasStarted;
    private bool _won;
    private bool _lost;
    void OnEnable()
    {
        Renderer = GetComponentInChildren<RiveRender>();
        
      
        //StartMinigame
    }

    public void EventWin()
    {
        _won = true;
    }

    public void EventLose()
    {
        _lost = true;
    }

    // Update is called once per frame
    void Update()
    {
        _won = Renderer.GetBool(WinBool);
        _lost = Renderer.GetBool(DeathBool);
        if (_won)
        {
            if (InputProxy.Instance.LeftDown || InputProxy.Instance.RightDown || InputProxy.Instance.SubmitDown)
            {
                _won = false;
                EndMiniGame(_won);
            }
        }

        if (_lost)
        {
            if (InputProxy.Instance.LeftDown || InputProxy.Instance.RightDown || InputProxy.Instance.SubmitDown)
            {
                _gameHasStarted = true;
                _lost = false;
                Renderer.TriggerVariable(GameStartRigger, true);
            }
        }
        
        if (!_gameHasStarted)
        {
            if (InputProxy.Instance.LeftDown || InputProxy.Instance.RightDown || InputProxy.Instance.SubmitDown)
            {
                _gameHasStarted = true;
                Renderer.TriggerVariable(GameStartRigger, true);
            }
            else
            {
                Renderer.TriggerVariable(GameStartRigger, false);
            }
            
            return;
        }
        
        if (InputProxy.Instance.LeftDown)
        {
            Renderer.TriggerVariable(LeftTrigger, true);
        }
        else
        {
            Renderer.TriggerVariable(LeftTrigger, false);
        }
        
        if (InputProxy.Instance.RightDown)
        {
            Renderer.TriggerVariable(RightTrigger, true);
        }
        else
        {
            Renderer.TriggerVariable(RightTrigger, false);
        }
    }

    public override MiniGameType GameType => MiniGameType.Car;
}
