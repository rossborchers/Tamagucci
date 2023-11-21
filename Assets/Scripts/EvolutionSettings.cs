using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TamaG/CommonEvolutionSettings")]
public class EvolutionSettings : ScriptableObject
{
   public enum LifetimeStage
   {
      None = 0,
      Egg = 1,
      Baby = 2,
      Teen = 3,
      Adult = 4,
      Senior = 5,
      Dead = 6
   }

   public int EggHammerHits = 4;
   public float PoopsPerSecond = 0.5f;
   
   public int BabyToTeenTime = 20;
   public int TeenToAdultTime = 40;
   public int AdultToSeniorTime = 100;
   public int SeniorToDeadTime = 120;
   public int DeadToRestartTime = 130;
   
  [Header("Egg Hatching")]
   public AnimationClip EggIdle;
   public AnimationClip EggHit;
   public AnimationClip EggHatch;
   public AnimationClip HammerIdle;
   public AnimationClip HammerHit;

   [Space]
   public AnimationClip TransitionWham;
   public AnimationClip Death;

   public float GetStageTransitionTime(LifetimeStage currentStage)
   {
      switch (currentStage)
      {
         case LifetimeStage.Baby: return BabyToTeenTime;
         case LifetimeStage.Teen: return TeenToAdultTime;
         case LifetimeStage.Adult: return AdultToSeniorTime;
         case LifetimeStage.Senior: return SeniorToDeadTime;
         case LifetimeStage.Dead: return DeadToRestartTime;
         default: return 0; //LifetimeStage.Egg, LifetimeStage.None
      }
   }
}
   