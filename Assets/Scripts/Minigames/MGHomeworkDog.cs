using PowerTools;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class MGHomeworkDog : MiniGame
{
    public GameObject Fin;
    public Transform HWRoot;
    public Transform BeforeIn;
    public Transform TargetIn;
    public AnimationClip DogEatClip;
    public AnimationClip DogIdleClip;
    
    public SpriteAnim Title;
    [FormerlySerializedAs("DogEat")] public SpriteAnim Dog;
    public SpriteAnim DogEatParticles;
    public SpriteAnim BookParticles;

    public Transform HW100Percent;
    public Transform HW80Percent;
    public Transform HW60Percent;
    public Transform HW40Percent;
    public Transform HW20Percent;
    public Transform HW10Percent;

    public float AmountPerClick = 0.101f;

    private float _amount = 1;

    public void OnEnable()
    {
        Fin.SetActive(false);
        _gameStarted = false;
        Title.gameObject.SetActive(true); 
        Title.Play(Title.Clip);
        _startTime = Time.time;
        _amount = 1;
        HWRoot.position = BeforeIn.position;
    }

    public int PagesToEat = 4;

    private int _currentEaten;

    private float _startTime;
    private bool _gameStarted;
    void Update()
    {
        if (_gameStarted)
        {
            HWRoot.position = Vector3.MoveTowards(HWRoot.position, TargetIn.position, Time.deltaTime*4);
        }
        
        if (InputProxy.Instance.LeftDown || InputProxy.Instance.RightDown || InputProxy.Instance.SubmitDown)
        {
            if (!_gameStarted && Time.time-_startTime > 0.25)
            {
                Title.Stop();
                Title.gameObject.SetActive(false);
                _gameStarted = true;
            }
            else
            {
                _amount -= AmountPerClick;

                if (_amount > 0.8f)
                {
                    _amount = 0.79f;
                }

                if (!Dog.IsPlaying(DogEatClip))
                {
                    Dog.Play(DogEatClip);
                }
                
                if (!DogEatParticles.IsPlaying())
                {
                    DogEatParticles.Play(DogEatParticles.Clip);
                }
                
                if (!BookParticles.IsPlaying())
                {
                    BookParticles.Play(BookParticles.Clip);
                }

                RemoveAllHW();
                if (_amount > 0.8f)
                {
                    HW100Percent.gameObject.SetActive(true);
                }
                else if (_amount > 0.6f)
                {
                    HW80Percent.gameObject.SetActive(true);
                }
                else if (_amount > 0.4f)
                {
                    HW60Percent.gameObject.SetActive(true);
                }
                else if (_amount > 0.2f)
                {
                    HW40Percent.gameObject.SetActive(true);
                }
                else if (_amount > 0.1f)
                {
                    HW20Percent.gameObject.SetActive(true);
                }
                else
                {
                    HW10Percent.gameObject.SetActive(true);
                }
            }
        }
        else if (!Dog.IsPlaying())
        {
            Dog.Play(DogIdleClip);
        }

        if (_amount <= 0)
        {
            _currentEaten = Mathf.Min(_currentEaten+1, PagesToEat);

            if (_currentEaten >= PagesToEat)
            {
                RemoveAllHW();
                Fin.SetActive(true);

                StartCoroutine(WaitThenEnd());

                IEnumerator WaitThenEnd()
                {
                    yield return new WaitForSeconds(2f);
                    EndMiniGame(true);
                }
            }
            else
            {
                RemoveAllHW();
                HW100Percent.gameObject.SetActive(true);
                _amount = 1;
                HWRoot.position = BeforeIn.position;
            }
        }
    }
    
    void RemoveAllHW()
    {
        HW100Percent.gameObject.SetActive(false);
        HW80Percent.gameObject.SetActive(false);
        HW60Percent.gameObject.SetActive(false);
        HW40Percent.gameObject.SetActive(false);
        HW20Percent.gameObject.SetActive(false);
        HW10Percent.gameObject.SetActive(false);
    }

    public override MiniGameType GameType => MiniGameType.FeedHomeworkToDog;
}
