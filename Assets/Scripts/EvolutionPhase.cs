using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TamaG/EvolutionPhase")]
public class EvolutionPhase : ScriptableObject
{
   [Serializable]
   public class EvolutionCondition
   {
      public FoodTypeEvolutionCondition FoodType;
      public EvolutionPhase Evolution;
   }
   
   public enum  FoodTypeEvolutionCondition
   {
      Dead,
      Carnivore,
      Veg,
      Sweet
   }

   public AnimationClip IdleHappy;
   public AnimationClip IdleSad;
   
   [Space]
   public AnimationClip IdleVeryHappy;
   public AnimationClip IdleVerySad;
   
   [Space]
   public AnimationClip Speak;
   public AnimationClip Eat;
   
   [Space]
   public AnimationClip Old;
   public AnimationClip Death;

   public bool UseSpriteTint;
   public Color TintColor;

   [Space]
   public List<EvolutionCondition> EvolutionConditions;
}
