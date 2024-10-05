using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class MiniGame : MonoBehaviour
{
   public abstract MiniGameType GameType { get; }
   
   public event Action<bool> OnMinigameEnd = (b) => { };
   public enum MiniGameType
   {
      None = 0,
      FeedHomeworkToDog = 1,
   }

   private void Awake()
   {
      //Let game manager control minigame if in main scene.
      if (FindObjectOfType<GameManager>())
      {
         gameObject.SetActive(false);
      }
   }

   public void EndMiniGame(bool success)
   {
      gameObject.SetActive(false);
      OnMinigameEnd?.Invoke(success);
   }

   public bool StartMinigame(MiniGameType type)
   {
      if (type == GameType)
      {
         gameObject.SetActive(true);
         return true;
      }

      return false;
   }
}
