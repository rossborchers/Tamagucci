using DG.Tweening;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using PowerTools;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Sequence = DG.Tweening.Sequence;

public class GameManager : MonoBehaviour
{
    public GameObject Midpoint;
    public List<MiniGame> MiniGames;
    
    public AudioSource SFXEatingBig;
    public AudioSource SFXEatingSmall;
    public AudioSource SFXEvolve;
    public AudioSource SFXDeath;
    public AudioSource SFXHatch;
    public AudioSource SFXSelect;
    public AudioSource SFXHighlight;

    public static GameManager Instance;

    [Header("Linkz")]
    public SpriteAnim BaseChar;
    public SpriteAnim EggChar;
    public SpriteAnim Hammer;
    
    public SpriteAnim SweetFoodEat;
    public SpriteAnim MeatFoodEat;
    public SpriteAnim VegFoodEat;
    
    public SpriteAnim Meteor;

    public GameObject EggBackground;

    private SpriteRenderer _baseCharRenderer;

    public GameObject Darkness;

    public EvolutionSettings GeneralSettings;
    public EvolutionPhase StartPhase;

    private List<GameObject> _activePoop = new List<GameObject>();
    public GameObject Poop;
    public Transform PoopZone;

    public GameObject Environment;

    public TMP_Text DeathReasonText;
    public SelectionMenu MainMenu;
    public SelectionMenu LightsMenu;
    public SelectionMenu FoodMenu;
    public SelectionMenu TryAgainMenu;
    
    public SpriteAnim EvolutionAnimation;

    public GameObject TopBar;

    public Bar HgrBar;
    public Bar EvoBar;
    public Bar ClnBar;
    public Bar SlpBar;

    public SpriteAnim Hearts;
    public SpriteAnim Zzz;
    
    public GameObject Title;
    public SpriteAnim TitleSparkle;
    public SpriteAnim TitleFade;
    
    public Animator EnvironmentMenuController;

    public GameObject GrimReaperEvent;
    public GameObject EndGameEvent;

    public Material CleanRadialFill;
    public float CleanCooldown;

    public float HeartDelayAfterEating = 2f;
    public float ZzzDelayAfterDarkness = 0.5f;

    private float _lastHammerHitTime;
    
    //runtime game state
    private EvolutionPhase currentPhase;
    private EvolutionSettings.LifetimeStage _currentStage = EvolutionSettings.LifetimeStage.Egg;
    private EvolutionSettings.LifetimeStage _lastStage = EvolutionSettings.LifetimeStage.None;
    private EvolutionSettings.LifetimeStage _lastEvolvutionsStage = EvolutionSettings.LifetimeStage.Egg;
    private float _startGameTime;
    private float _currentTime;
    private float GameTime => _currentTime - _startGameTime;
    public bool TryAgainMenuUp { get; private set; }

    private int _currentHits;
    private bool _hatched;

    private bool _completedMinigameForState;

    public float HungerEatReduction = 3;
    public float HungerGrowthSpeed = 0.25f;
    public float MaxHunger = 6;
    private float _hunger;
    
    [FormerlySerializedAs("SleepGained")] public float SleepRecoverySpeed = 2;
    public float SleepDecaySpeed = 0.25f;
    public float MaxSleep = 2;
    private float _sleepNeeded;

    public float MaxClean = 5;
    //infer clean from poop count

    [Space] private float NormalizedVeryHappySatisfactionLevel = 0.8f;
    private float NormalizedNormalSatisfactionLevel = 0.5f;
    private float NormalizedSadSatisfactionLevel = 0.3f;

    private int _vegPoints;
    private int _meatPoints;
    private int _sweetPoints;

    private bool _sleeping;

    private float _lastPoop;

    private MiniGame _currentMinigame = null;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _baseCharRenderer = BaseChar.GetComponent<SpriteRenderer>();
        MeatFoodEat.gameObject.SetActive(false);
        SweetFoodEat.gameObject.SetActive(false);
        VegFoodEat.gameObject.SetActive(false);
        TryAgainMenu.Close();
    }

    private bool TimeForMinigame()
    {
        return GetNormalizedStageTime() > 0.5f;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Aurdino.Instance.UpdateState(Aurdino.GameState.Egg);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Aurdino.Instance.UpdateState(Aurdino.GameState.Evolving);
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Aurdino.Instance.UpdateState(Aurdino.GameState.AwakeTimer);
        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Aurdino.Instance.UpdateState(Aurdino.GameState.SleepTimer);
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Aurdino.Instance.UpdateState(Aurdino.GameState.Dead);
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            Time.timeScale = 3f;
        }
        else
        {
            Time.timeScale = 1f;
        }
        
        if (_currentStage == EvolutionSettings.LifetimeStage.Egg)
        {
            _startGameTime = Time.time;
            _currentTime = _startGameTime;
            currentPhase = StartPhase;
            TopBar.SetActive(false);
        }
        else
        {
            _hatched = false;
            _currentHits = GeneralSettings.EggHammerHits;

            if (_evolving)
            {
                TopBar.SetActive(false);
            }
            else
            {
                TopBar.SetActive(true);
            }
        }
        
        bool firstUpdate = _lastStage != _currentStage;


        if (currentPhase.StartMinigames.Count == 0)
        {
            Midpoint.gameObject.SetActive(false);
        }
        
        //Dont perform stage logic or update anything
        if (_currentMinigame != null)
        {
            return;
        }

        bool isSeniorUpdate = _currentStage == EvolutionSettings.LifetimeStage.Senior;
        if (TimeForMinigame() && (currentPhase.StartMinigames.Count > 0) && !_completedMinigameForState || isSeniorUpdate && GeneralSettings.SeniorMinigame.Count > 0)
        {
            Debug.Log("TIME FOR MINIGAME");
            Midpoint.gameObject.SetActive(false);
            MainMenu.Close();
            MiniGame.MiniGameType chosenGame;
            
            if(isSeniorUpdate)
            {
                chosenGame = GeneralSettings.SeniorMinigame[Random.Range(0, GeneralSettings.SeniorMinigame.Count)];
            }
            else 
            {
                chosenGame = currentPhase.StartMinigames[Random.Range(0, currentPhase.StartMinigames.Count)];
            }
            foreach (var g in MiniGames)     
            {
                if (g.GameType == chosenGame)
                {
                    
                    _currentMinigame = g;
                    _currentMinigame.StartMinigame(chosenGame);
                    _currentMinigame.OnMinigameEnd += (bool success) =>
                    {
                        _completedMinigameForState = true;
                        MainMenu.Open();
                        if (success)
                        {
                            Debug.Log("MINIGAME SUCCESS");
                        }
                        else
                        {
                            Debug.Log("MINIGAME FAILURE");
                        }

                        _currentMinigame = null;
                    };
                }
            }
        }
        
        
        switch (_currentStage)
        {
            case EvolutionSettings.LifetimeStage.Egg:
                if (firstUpdate)
                {
                    EggBackground.SetActive(true);
                    TitleFade.gameObject.SetActive(false);
                    Title.SetActive(true);
                    TitleSparkle.gameObject.SetActive(true);
                    TitleSparkle.Play(TitleSparkle.Clip);
                    EggChar.gameObject.SetActive(true);
                    BaseChar.gameObject.SetActive(false);
                    Meteor.gameObject.SetActive(false);
                }
                _startGameTime = Time.time;
                _currentTime = _startGameTime;
                if (EggUpdate(firstUpdate))
                {
                    _lastStage = _currentStage;
                }
                break;
            case EvolutionSettings.LifetimeStage.Senior:
                SeniorUpdate();
                _lastStage = _currentStage;
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

        if (endGameBusy)
        {
            return;
        }
        
        TryMoveToNextStage();

        if (currentPhase != null)
        {
            if (currentPhase.UseSpriteTint)
            {
                _baseCharRenderer.color = currentPhase.TintColor;
            }
            else
            {
                _baseCharRenderer.color = Color.white;
            }
        }

        if (!_evolving)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                _currentTime+=Time.deltaTime * 15;
            }
            else
            {
                _currentTime+=Time.deltaTime;
            }
        }
    }
   
    private bool EggUpdate(bool firstUpdate)
    {
        if (_hatched)
        {
            if (!EggChar.IsPlaying(GeneralSettings.EggHatch))
            {
                EggChar.gameObject.SetActive(false);
                BaseChar.gameObject.SetActive(true);
                _currentStage = EvolutionSettings.LifetimeStage.Baby;
                UpdateStateTime();
                _lastPoop = Time.time;
                MainMenu.Open();
                return false;
            }

            return true;
        }
        
        if (firstUpdate)
        {
            GrimReaperEvent.gameObject.SetActive(false);
            Hearts.gameObject.SetActive(false);
            Zzz.gameObject.SetActive(false);
            Darkness.SetActive(false);
            Hammer.gameObject.SetActive(true);
            EndGameEvent.SetActive(false);
            EvolutionAnimation.gameObject.SetActive(false);

            StartCoroutine(DelaySend());
            IEnumerator DelaySend()
            {
                yield return new WaitForSeconds(2);
                Aurdino.Instance.UpdateState(Aurdino.GameState.Egg);
            }
           
        }

        if (_currentHits >= GeneralSettings.EggHammerHits)
        {
            _hatched = true;
            Hammer.gameObject.SetActive(false);
            Instance.SFXHatch.Play();
            EggChar.Play(GeneralSettings.EggHatch);
            
            if (Title.activeSelf)
            {
                EggBackground.SetActive(false);
                Title.SetActive(false);
                TitleSparkle.Stop();
                TitleSparkle.gameObject.SetActive(false);
                TitleFade.gameObject.SetActive(true);
                TitleFade.Play(TitleFade.Clip);
                _hunger = 0;
                _sleepNeeded = 0;
            }
        }

        if (InputProxy.Instance.SubmitDown)
        {
            _lastHammerHitTime = Time.time;
            GameManager.Instance.SFXHighlight.Play();
            _currentHits++;
            if (!Hammer.IsPlaying(GeneralSettings.HammerHit))
            {
                Hammer.Play(GeneralSettings.HammerHit);
                BaseChar.Play(GeneralSettings.EggHit);
            }
        }
        else
        {
            if (!EggChar.IsPlaying(GeneralSettings.EggIdle) && !Hammer.IsPlaying(GeneralSettings.HammerHit))
            {
                Hammer.Play(GeneralSettings.HammerIdle);
                EggChar.Play(GeneralSettings.EggIdle);
            }
        }

        if (Time.time - _lastHammerHitTime > 10)
        {
            //Reset hits if somebody walks away
            _currentHits = 0;
        }

        return true;
    }

    private Coroutine _endGameCoroutine;
    private void DeadUpdate()
    {
        if (!BaseChar.IsPlaying(GeneralSettings.Death))
        {
            BaseChar.Play(GeneralSettings.Death);

            if (_endGameCoroutine == null)
            {
                _endGameCoroutine = StartCoroutine(EndGameAfter(3f, "DEATH BY\nOLD AGE"));
            }
        }

        IEnumerator EndGameAfter(float time, string reason)
        {
            yield return new WaitForSeconds(time);
            EndGame(reason); 
            _endGameCoroutine = null;
        }
    }
    
    private void SeniorUpdate()
    {
        if (_dying)
        {
            if (!BaseChar.IsPlaying(currentPhase.Death))
            {
                BaseChar.Play(currentPhase.Death);
            }
        }
        else
        {
            if (!BaseChar.IsPlaying(currentPhase.Old))
            {
                BaseChar.Play(currentPhase.Old);
            }
        }
    }

    private Sequence _bounceSequence;
   
    private void GeneralUpdate(EvolutionSettings.LifetimeStage stage, bool firstUpdate)
    {
        if (!BaseChar.IsPlaying(currentPhase.Eat) && !BaseChar.IsPlaying(currentPhase.Speak) && (currentPhase.Death == null))
        {
            float hungerSatisfaction = (MaxHunger - _hunger)/MaxHunger;
            float sleepSatisfaction = (MaxSleep - _sleepNeeded)/MaxSleep;
            float cleanSatisfaction = 1f-Mathf.Min(_activePoop.Count,MaxClean)/MaxClean;

            if (hungerSatisfaction > NormalizedVeryHappySatisfactionLevel
                && sleepSatisfaction > NormalizedVeryHappySatisfactionLevel
                && cleanSatisfaction > NormalizedVeryHappySatisfactionLevel)
            {
                if (!BaseChar.IsPlaying(currentPhase.IdleVeryHappy))
                {
                    BaseChar.Play(currentPhase.IdleVeryHappy);
                }
            }
            else if (hungerSatisfaction > NormalizedNormalSatisfactionLevel
                && sleepSatisfaction > NormalizedNormalSatisfactionLevel
                && cleanSatisfaction > NormalizedNormalSatisfactionLevel)
            {
                if (!BaseChar.IsPlaying(currentPhase.IdleHappy))
                {
                    BaseChar.Play(currentPhase.IdleHappy);
                }
            }
            else if (hungerSatisfaction > NormalizedSadSatisfactionLevel
                     && sleepSatisfaction > NormalizedSadSatisfactionLevel
                     && cleanSatisfaction > NormalizedSadSatisfactionLevel)
            {
                if (!BaseChar.IsPlaying(currentPhase.IdleSad))
                {
                    BaseChar.Play(currentPhase.IdleSad);
                }
            }
            else
            {
                if (!BaseChar.IsPlaying(currentPhase.IdleVerySad))
                {
                    BaseChar.Play(currentPhase.IdleVerySad);
                }
            }
        }
        
        if (currentPhase.UseBounceAnim && _currentStage != EvolutionSettings.LifetimeStage.Senior && _currentStage != EvolutionSettings.LifetimeStage.Dead)
        {
            if (_bounceSequence == null || !_bounceSequence.IsPlaying())
            {
                _bounceSequence?.Kill();
                _bounceSequence = DOTween.Sequence();
                _bounceSequence.Append(BaseChar.transform.DOScale(new Vector3(0.995f, 1.05f, 1), 0.4f));
                _bounceSequence.Append(BaseChar.transform.DOScale(new Vector3(1, 1, 1), 0.4f));
            }
        }
        else
        {
            _bounceSequence?.Kill();
            BaseChar.transform.localScale = Vector3.one;
        }
        
        //Clean cooldown
        float normalizedTime = Mathf.Clamp01((Time.time - _lastCleanTime) / CleanCooldown);
        CleanRadialFill.SetFloat("_Arc1", (1f-normalizedTime) * 360);
        
        if (InputProxy.Instance.ResetDown)
        {
            SceneManager.LoadScene(0);
        }
    }

    private Tween evoShake;

    private bool IsSad()
    {
        float hungerSatisfaction = (MaxHunger - _hunger)/MaxHunger;
        float sleepSatisfaction = (MaxSleep - _sleepNeeded)/MaxSleep;
        float cleanSatisfaction = 1f-Mathf.Min(_activePoop.Count,MaxClean)/MaxClean;
        return hungerSatisfaction < NormalizedNormalSatisfactionLevel
            && sleepSatisfaction < NormalizedNormalSatisfactionLevel
            && cleanSatisfaction < NormalizedNormalSatisfactionLevel;
    }

    private bool _dying;

    private void TryMoveToNextStage()
    {
        float transitionTime = GeneralSettings.GetStageTransitionTime(_currentStage);
       // CounterText.text = $"{Mathf.FloorToInt(GameTime)}/{transitionTime}";

       if (!_evolving)
       {
           _hunger += Time.deltaTime * HungerGrowthSpeed;

           if (_sleeping)
           {
               _sleepNeeded = Mathf.Max(0, _sleepNeeded - Time.deltaTime * SleepRecoverySpeed);
           }
           else
           {
               _sleepNeeded += Time.deltaTime * SleepDecaySpeed;
           }
       }
       
        HgrBar.SetValue(Mathf.Max(0, MaxHunger-_hunger), MaxHunger);

        float lastEvoTime = GeneralSettings.GetStageTransitionTime(_lastEvolvutionsStage);
        float evoTime = GeneralSettings.GetStageTransitionTime(_currentStage);
        float stageTime = evoTime - lastEvoTime;
        float currentStageTime = GameTime - lastEvoTime;
        
        //Debug.Log($"{currentStageTime}/{stageTime}, last evo {_lastEvolvutionsStage}: {lastEvoTime}. Current {_currentStage}: {evoTime}");

        if(currentStageTime >= 0 && stageTime > 0)
        {
            float stageTimeClamped = Mathf.Clamp(currentStageTime, 0, stageTime);
            var lhs = stageTimeClamped;
            var rhs = stageTime;
            EvoBar.SetValue(Mathf.Max(rhs-lhs), rhs);

            if (lhs / rhs > 0.8f)
            {
                if (evoShake == null || !evoShake.IsPlaying())
                {
                    evoShake?.Kill();
                    evoShake = EvoBar.transform.DOShakePosition(20, 0.1f, 10, 90, false, false, ShakeRandomnessMode.Harmonic);
                }
            }
        }

        float cln = Mathf.Clamp(_activePoop.Count, 0, MaxClean);
        ClnBar.SetValue(MaxClean -cln , MaxClean);
        SlpBar.SetValue(MaxSleep-_sleepNeeded, MaxSleep);

        //FAIL YOU STARVED/DIED OF LACK OF SLEEP/DIED OF DIRTY
        bool shouldDie = false;
        string deathReason = "";

        if (_currentStage == EvolutionSettings.LifetimeStage.Egg ||
            _currentStage == EvolutionSettings.LifetimeStage.Senior)
        {
            shouldDie = false;
            deathReason = "";
        }
        else if (_sleepNeeded > MaxSleep)
        {
            deathReason = "LACK OF SLEEP";
            shouldDie = true;
        }
        else if (_hunger > MaxHunger)
        {
            deathReason = "STARVATION";
            shouldDie = true;
        }
        else if(cln >= MaxClean)
        {
            deathReason = "DROWNING IN POOP";
            shouldDie = true;
        }
        

        if (shouldDie)
        {
            EndGame("DEATH BY\n"+deathReason);
            return;
        }

       if (GameTime >= transitionTime && !_evolving)
        {
            if (_currentStage == EvolutionSettings.LifetimeStage.Dead 
                || _currentStage == EvolutionSettings.LifetimeStage.None
                || _currentStage == EvolutionSettings.LifetimeStage.Egg )
            {
                //These stages are handled with unique logic
               // Debug.Log($"Returning on custom evolution stage: {_currentStage}");
                return;
            }
            else if ( _currentStage == EvolutionSettings.LifetimeStage.Senior)
                {
                    if (!_dying)
                    {
                        //Meteor.gameObject.SetActive(true);
                        //Meteor.Play(Meteor.Clip);
                        _dying = true;
                        StartCoroutine(Die());
                    }
                }
            else
            {
                Debug.Assert(currentPhase.EvolutionConditions.Count > 0);
                foreach (EvolutionPhase.EvolutionCondition condition in currentPhase.EvolutionConditions)
                {
                    if (condition.FoodType == EvolutionPhase.FoodTypeEvolutionCondition.Dead)
                    {
                        PlayEvolutionAnimation(() =>
                        {
                            _currentStage = EvolutionSettings.LifetimeStage.Senior;
                            BaseChar.Play(currentPhase.Speak);
                        });
                        break;
                    }
                    
                    if (condition.FoodType == EvolutionPhase.FoodTypeEvolutionCondition.Carnivore && _meatPoints > _vegPoints && _meatPoints > _sweetPoints)
                    {
                        //MEAT EVOLUTION
                        Evolve(condition);
                        break;
                    }
                    if (condition.FoodType == EvolutionPhase.FoodTypeEvolutionCondition.Veg && _vegPoints > _meatPoints && _vegPoints > _sweetPoints)
                    {
                        //VEG EVOLUTION
                        Evolve(condition);
                        break;
                    }
            
                    if (condition.FoodType == EvolutionPhase.FoodTypeEvolutionCondition.Sweet && _sweetPoints > _meatPoints && _sweetPoints > _vegPoints)
                    {
                        //SWEET EVOLUTION
                        Evolve(condition);
                        break;
                    }
                }

                if (!_evolving)
                {
                    //this happens  if you dont feed anything
                    var randomEvo =currentPhase.EvolutionConditions[Random.Range(0, currentPhase.EvolutionConditions.Count)];
                    Evolve(randomEvo);
                }
                
                void Evolve(EvolutionPhase.EvolutionCondition condition)
                {
                    _completedMinigameForState = false;
                    Midpoint.gameObject.SetActive(true);
                    PlayEvolutionAnimation(() =>
                    {
                        _currentStage = (EvolutionSettings.LifetimeStage) ((int)_currentStage) + 1;
                        currentPhase = condition.Evolution;
                        BaseChar.Play(currentPhase.Speak);
                    });
                }
            }
        }
    }


    private bool _lastTryAgainResult = false;
    public void MinigameTryAgain()
    {
        _lastTryAgainResult = true;
        TryAgainMenu.Close();
        TryAgainMenuUp = false;
    }
    
    public void MinigameNah()
    {
        _lastTryAgainResult = false;
        TryAgainMenu.Close();
        TryAgainMenuUp = false;
    }

    private bool _evolving = false;

    public float GetNormalizedStageTime()
    {
        float lastEvoTime = GeneralSettings.GetStageTransitionTime(_lastEvolvutionsStage);
        float evoTime = GeneralSettings.GetStageTransitionTime(_currentStage);
        float stageTime = evoTime - lastEvoTime;
        float currentStageTime = GameTime - lastEvoTime;
        float normT = currentStageTime / stageTime;
        return normT;
    }
    
    private void UpdateStateTime()
    {
        float lastEvoTime = GeneralSettings.GetStageTransitionTime(_lastEvolvutionsStage);
        float evoTime = GeneralSettings.GetStageTransitionTime(_currentStage);
        float stageTime = evoTime - lastEvoTime;
        Aurdino.Instance.UpdateTimerState(Mathf.CeilToInt(stageTime));
    }

    private IEnumerator Die()
    {
        LightsMenu.Close();
        FoodMenu.Close();
        LightsOn(false);
        MainMenu.Close();
        MainMenu.gameObject.SetActive(false);
        TopBar.gameObject.SetActive(false);

        Aurdino.Instance.UpdateState(Aurdino.GameState.Dead);
        
        Instance.SFXDeath.Play();
        yield return new WaitForSeconds(1.3f);

        if (!BaseChar.IsPlaying(currentPhase.Death))
        {
            BaseChar.Play(currentPhase.Death);
        }
        
        yield return new WaitForSeconds(3f);
        _currentStage = EvolutionSettings.LifetimeStage.Dead;
        _dying = false;
    }

    public void SetLightsMenuIndex()
    {
        if (_sleeping)
        {
            LightsMenu.CurrentIndex = 1;
        }
        else
        {
            LightsMenu.CurrentIndex = 0;
        }
    }
    private void PlayEvolutionAnimation(Action evolveCallback)
    {
        _evolving = true;
        StartCoroutine(EvoAnim());
        IEnumerator EvoAnim()
        {
            LightsMenu.Close();
            FoodMenu.Close();
            LightsOn(false);
            
            MainMenu.Close();
            MainMenu.gameObject.SetActive(false);
           
            Aurdino.Instance.UpdateState(Aurdino.GameState.Evolving);
            
            BaseChar.transform.DOShakePosition(2f, 0.025f, 10, 90, false, false, ShakeRandomnessMode.Harmonic);
            yield return new WaitForSeconds(1f);
            Instance.SFXEvolve.Play();
            BaseChar.transform.DOShakePosition(1, 0.05f, 10, 90, false, false, ShakeRandomnessMode.Harmonic);
            yield return new WaitForSeconds(1f);
            EvolutionAnimation.gameObject.SetActive(true);
            EvolutionAnimation.Play(EvolutionAnimation.GetCurrentAnimation());

            yield return new WaitForSeconds(1);
            
            _lastEvolvutionsStage = _currentStage;
            evoShake?.Kill(true);
            evolveCallback?.Invoke();
            yield return new WaitForSeconds(1);
            
            while (_activePoop.Count > 0)
            {
                int idx = _activePoop.Count - 1;
                Destroy(_activePoop[idx]);
                _activePoop.RemoveAt(idx);
            }
            _lastPoop = Time.time;
            
            _hunger = 0;
            _sleepNeeded = 0;
            
            EvolutionAnimation.Stop();
            EvolutionAnimation.gameObject.SetActive(false);
          
            BaseChar.transform.DOShakePosition(0.5f, 0.05f, 10, 90, false, false, ShakeRandomnessMode.Harmonic);
            yield return new WaitForSeconds(0.5f);
            BaseChar.transform.DOShakePosition(1, 0.025f, 10, 90, false, false, ShakeRandomnessMode.Harmonic);
            yield return new WaitForSeconds(1f);
            _evolving = false;

            UpdateStateTime();

            MainMenu.gameObject.SetActive(true);
            MainMenu.Open();
            TopBar.SetActive(true);
        }
    }

    private float _lastCleanTime;
    public void Clean()
    {
        if (Time.time - _lastCleanTime > CleanCooldown)
        {
            _lastCleanTime = Time.time;
            if(_activePoop.Count > 0)
            {
                int idx = _activePoop.Count - 1;
                Destroy(_activePoop[idx]);
                _activePoop.RemoveAt(idx);
            }
        }
    }
    
    [UsedImplicitly]
    public void EnviromentMenuSlideOn()
    {
        EnvironmentMenuController.ResetTrigger("Off");
        EnvironmentMenuController.SetTrigger("On");
    }
    
    [UsedImplicitly]
    public void EnviromentMenuSlideOff()
    {
        EnvironmentMenuController.ResetTrigger("On");
        EnvironmentMenuController.SetTrigger("Off");
    }

    [UsedImplicitly]
    public void LightsOn( bool updateArdinoState)
    {
        if (updateArdinoState)
        {
            Aurdino.Instance.UpdateState(Aurdino.GameState.AwakeTimer);
        }
        ResetCurrentEvent();
        _sleeping = false;
        Darkness.SetActive(false);
        Zzz.Stop();
        Zzz.gameObject.SetActive(false);
        GrimReaperEvent.gameObject.SetActive(false);
        EnviromentMenuSlideOff();
    }

    [UsedImplicitly]
    public void LightsOn()
    {
        LightsOn(true);
    }

    private Coroutine currentEvent;

    private void ResetCurrentEvent()
    {
        if (currentEvent != null)
        {
            StopCoroutine(currentEvent);
            currentEvent = null;
        }
    }
    
    [UsedImplicitly]
    public void LightsOff()
    {
        Aurdino.Instance.UpdateState(Aurdino.GameState.SleepTimer);
        _sleeping = true;
        Darkness.SetActive(true);
        ResetCurrentEvent();
        currentEvent = StartCoroutine(ShowZzzAfter(ZzzDelayAfterDarkness));
        EnviromentMenuSlideOff();
    }
    
    [UsedImplicitly]
    public void FeedVeg()
    {
        Instance.SFXEatingBig.Play();
        MeatFoodEat.gameObject.SetActive(false);
        SweetFoodEat.gameObject.SetActive(false);
        VegFoodEat.gameObject.SetActive(true);
        VegFoodEat.Play(VegFoodEat.Clip);
        _hunger = Mathf.Max(0, _hunger-HungerEatReduction);
        _vegPoints++;
        EnviromentMenuSlideOff();
        DoEatAndLove();
    }
    
    [UsedImplicitly]
    public void FeedMeat()
    {
        Instance.SFXEatingBig.Play();
        MeatFoodEat.gameObject.SetActive(true);
        SweetFoodEat.gameObject.SetActive(false);
        VegFoodEat.gameObject.SetActive(false);
        MeatFoodEat.Play(MeatFoodEat.Clip);
        _hunger = Mathf.Max(0, _hunger-HungerEatReduction);
        _meatPoints++;
        EnviromentMenuSlideOff();
        DoEatAndLove();
    }
    
    [UsedImplicitly]
    public void FeedSweet()
    {
        Instance.SFXEatingBig.Play();
        MeatFoodEat.gameObject.SetActive(false);
        SweetFoodEat.gameObject.SetActive(true);
        VegFoodEat.gameObject.SetActive(false);
        SweetFoodEat.Play(SweetFoodEat.Clip);
        _hunger = Mathf.Max(0, _hunger-HungerEatReduction);
        _sweetPoints++;
        EnviromentMenuSlideOff();
        DoEatAndLove();
    }
    
    IEnumerator DoAfter(float time, Action todo)
    {
        yield return new WaitForSeconds(time);
        todo.Invoke();
    }

    void DoEatAndLove()
    {
        StartCoroutine(DoAfter(0.5f,
            () =>
            {
                BaseChar.Play(currentPhase.Eat);

                if (!IsSad())
                {
                    StartCoroutine(ShowHeartsAfter(HeartDelayAfterEating));
                }
            }));
    }

    
    IEnumerator ShowHeartsAfter(float time)
    {
        yield return new WaitForSeconds(time);
        Hearts.gameObject.SetActive(true);
        Hearts.Play(Hearts.GetCurrentAnimation());
        yield return new WaitForSeconds(0.9f);
        Hearts.gameObject.SetActive(false);
    }
    
    IEnumerator ShowZzzAfter(float time)
    {
        yield return new WaitForSeconds(time);
        Zzz.gameObject.SetActive(true);
        Zzz.Play(Zzz.GetCurrentAnimation());

        yield return new WaitForSeconds(3);
        GrimReaperEvent.gameObject.SetActive(true);
    }

    private bool endGameBusy = false;
    public void EndGame(string reason)
    {
        if (endGameBusy)
        {
            return;
        }
        endGameBusy = true;
        _currentStage = EvolutionSettings.LifetimeStage.Dead;
        ResetCurrentEvent();
        currentEvent = StartCoroutine(EndGameCo());
        IEnumerator EndGameCo()
        {
            DeathReasonText.text = reason;
            MainMenu.Close();
            MainMenu.gameObject.SetActive(false);
            TopBar.SetActive(false);
            GrimReaperEvent.gameObject.SetActive(false);
            Hearts.gameObject.SetActive(false);
            Zzz.gameObject.SetActive(false);
            Darkness.SetActive(false);
            Hammer.gameObject.SetActive(true);
            GrimReaperEvent.SetActive(false);
            EndGameEvent.SetActive(true);

            yield return new WaitForSeconds(2);
            Aurdino.Instance.UpdateState(Aurdino.GameState.Egg);
            yield return new WaitForSeconds(1);
            SceneManager.LoadScene(0);
        }
    }

    public void BringUpTryAgainMenu()
    {
        TryAgainMenuUp = true;
        TryAgainMenu.Open();
    }

    public bool TryAgainResult()
    {
        return _lastTryAgainResult;
    }
}

