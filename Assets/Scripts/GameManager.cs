using System;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Linkz")]
    public SpriteAnim BaseChar;
    public SpriteAnim Hammer;

    public EvolutionSettings GeneralSettings;
    public EvolutionPhase StartPhase;
    
    //runtime game state
    private EvolutionPhase currentPhase;
    private EvolutionSettings.LifetimeStage _currentStage = EvolutionSettings.LifetimeStage.Egg;
    private EvolutionSettings.LifetimeStage _lastStage = EvolutionSettings.LifetimeStage.None;
    private float _startGameTime;
    public float GameTime => Time.time - _startGameTime;

    private int _currentHits;
    private bool _hatched;
    
    private int Hunger;
    private int VegPoints;
    private int MeatPoints;
    private int SweetPoints;

    public void Update()
    {
        bool firstUpdate = _lastStage != _currentStage;
        if (_currentStage == EvolutionSettings.LifetimeStage.Egg)
        {
            _startGameTime = Time.time;
            currentPhase = StartPhase;
        }
        else
        {
            _hatched = false;
            _currentHits = GeneralSettings.EggHammerHits;
        }
        
        switch (_currentStage)
        {
            case EvolutionSettings.LifetimeStage.Egg:
                _startGameTime = Time.time;
                EggUpdate(firstUpdate);
                break;
            case EvolutionSettings.LifetimeStage.Dead:
                DeadUpdate(firstUpdate);
                break;
            default:
                GeneralUpdate(_currentStage, firstUpdate);
                break;
        }

        TryMoveToNextStage();
        _lastStage = _currentStage;
    }
   
    private void EggUpdate(bool firstUpdate)
    {
        if (_hatched)
        {
            if (!BaseChar.IsPlaying(GeneralSettings.EggHatch))
            {
                _currentStage = EvolutionSettings.LifetimeStage.Baby;
            }
            return;
        }
        
        if (firstUpdate)
        {
            Hammer.gameObject.SetActive(true);
        }

        if (_currentHits >= GeneralSettings.EggHammerHits)
        {
            _hatched = true;
            Hammer.gameObject.SetActive(false);
            BaseChar.Play(GeneralSettings.EggHatch);
            
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _currentHits++;
            if (!Hammer.IsPlaying(GeneralSettings.HammerHit))
            {
                Hammer.Play(GeneralSettings.HammerHit);
                BaseChar.Play(GeneralSettings.EggHit);
            }
        }
        else
        {
            if (!BaseChar.IsPlaying(GeneralSettings.EggIdle) && !Hammer.IsPlaying(GeneralSettings.HammerHit))
            {
                Hammer.Play(GeneralSettings.HammerIdle);
                BaseChar.Play(GeneralSettings.EggIdle);
            }
        }
    }
    
    private void DeadUpdate(bool firstUpdate)
    {
        if (!BaseChar.IsPlaying(GeneralSettings.Death))
        {
            BaseChar.Play(GeneralSettings.Death);
        }
    }
   
    private void GeneralUpdate(EvolutionSettings.LifetimeStage stage, bool firstUpdate)
    {
        if (!BaseChar.IsPlaying(currentPhase.IdleHappy))
        {
            BaseChar.Play(currentPhase.IdleHappy);
        }
    }

    private void TryMoveToNextStage()
    {
        if (GameTime >= GeneralSettings.GetStageTransitionTime(_currentStage))
        {
            if (_currentStage == EvolutionSettings.LifetimeStage.Dead 
                || _currentStage == EvolutionSettings.LifetimeStage.None
                || _currentStage == EvolutionSettings.LifetimeStage.Egg)
            {
                //These stages are handled with unique logic
                return;
            }
            else
            {
                _currentStage = (EvolutionSettings.LifetimeStage) ((int)_currentStage) + 1;
            }
        }
    }
}

