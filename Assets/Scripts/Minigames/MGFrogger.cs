using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGFrogger : MiniGame
{
    public GameObject Fin;
    private const string LeftTrigger = "Jump_Backward";
    private const string RightTrigger = "Jump_Forward";
    private const string GameReStartRigger = "Restart";
    private const string GameStartRigger = "StartGame";
    
    private string WinBool = "Win";
    private string DeathBool = "Death";
    private RiveRender Renderer;
    
    private bool _won;
    private bool _lost;
    void OnEnable()
    {
        Renderer = GetComponentInChildren<RiveRender>();
        Fin.SetActive(false);
    }

    public void EventWin()
    {
        _won = true;
    }
    
    public void EventDeath()
    {
        _lost = true;
    }

    private Coroutine _restartCoroutine = null;
    
    private bool _gameStarted = false;

    // Update is called once per frame
    void Update()
    {
        if (_restartCoroutine != null)
        {
            return;
        }
        
        if (!_gameStarted)
        {
            if (InputProxy.Instance.LeftDown || InputProxy.Instance.RightDown ||
                     InputProxy.Instance.SubmitDown)
            {
                Debug.Log("Frogger Game Started!");
                Renderer.TriggerVariable(GameStartRigger, true);
                _gameStarted = true;
            }
            return;
        }
        
        if (InputProxy.Instance.LeftDown)
        {
            Renderer.TriggerVariable(LeftTrigger);
        }
        
        if (InputProxy.Instance.RightDown)
        {
            Renderer.TriggerVariable(RightTrigger);
        }
        
        if (_won)
        {
            Fin.SetActive(true);

            StartCoroutine(WaitThenEnd());

            IEnumerator WaitThenEnd()
            {
                yield return new WaitForSeconds(2f);
                EndMiniGame(false);
            }
        }
        else if (_lost)
        {
            _restartCoroutine = StartCoroutine(AskForRestart());
        }
    }

    IEnumerator AskForRestart()
    {
        GameManager.Instance.BringUpTryAgainMenu();
        while (GameManager.Instance.TryAgainMenuUp)
        {
            yield return null;
        }
        
        bool tryAgain = GameManager.Instance.TryAgainResult();

        if (tryAgain)
        {
            Renderer.TriggerVariable(GameReStartRigger);
            _lost = false;
        }
        else
        {
            Fin.SetActive(true);

            StartCoroutine(WaitThenEnd());

            IEnumerator WaitThenEnd()
            {
                yield return new WaitForSeconds(2f);
                EndMiniGame(false);
            }
        }

        _restartCoroutine = null;
    }

    public override MiniGameType GameType => MiniGameType.Frogger;
}
