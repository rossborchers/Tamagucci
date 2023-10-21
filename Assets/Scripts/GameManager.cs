using System;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [Header("Linkz")]
    public SpriteAnim BaseChar;
    public SpriteAnim Hammer;

    public EvolutionSettings GeneralSettings;
    public EvolutionPhase StartPhase;

    private List<GameObject> _activePoop = new List<GameObject>();
    public GameObject Poop;
    public Transform PoopZone;

    public GameObject Environment;

    public SelectionMenu MainMenu;
    
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

    private float _lastPoop;
    
    public void Update()
    {
       
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
        
        bool firstUpdate = _lastStage != _currentStage;
        switch (_currentStage)
        {
            case EvolutionSettings.LifetimeStage.Egg:
                _startGameTime = Time.time;
                if (EggUpdate(firstUpdate))
                {
                    _lastStage = _currentStage;
                }
                break;
            case EvolutionSettings.LifetimeStage.Dead:
                DeadUpdate();
                _lastStage = _currentStage;
                break;
            default:
                GeneralUpdate(_currentStage, firstUpdate);
                if (Time.time - _lastPoop > 1f / GeneralSettings.PoopsPerSecond)
                {
                    GameObject poop = Instantiate(Poop);
                    poop.transform.parent = PoopZone;
                    var rand = Random.insideUnitCircle * 0.1f;
                    poop.transform.localPosition = new Vector2(rand.x, rand.y);
                    _lastPoop = Time.time;
                    _activePoop.Add(poop);
                }
                _lastStage = _currentStage;
                break;
        }

        TryMoveToNextStage();
    }
   
    private bool EggUpdate(bool firstUpdate)
    {
        if (_hatched)
        {
            if (!BaseChar.IsPlaying(GeneralSettings.EggHatch))
            {
                _currentStage = EvolutionSettings.LifetimeStage.Baby;
                _lastPoop = Time.time;
                MainMenu.Open();
                return false;
            }

            return true;
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

        return true;
    }
    
    private void DeadUpdate()
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

    private float _lastCleanTime;
    public void Clean()
    {
        if (Time.time - _lastCleanTime > 0.5f)
        {
            _lastCleanTime = Time.time;
            if(_activePoop.Count > 0)
            {
                Destroy(_activePoop[0]);
            }
        }
    }
    

    public void HideEnvironment()
    {
        Environment.SetActive(false);
    }

    public void ToggleLights()
    {
        //TODO:
    }
    
    public void FeedVeg()
    {
        Environment.SetActive(true);
        //TODO:
    }
    
    public void FeedMeat()
    {
        Environment.SetActive(true);
        //TODO:
    }
    
    public void FeedSweet()
    {
        Environment.SetActive(true);
        //TODO:
    }
}

