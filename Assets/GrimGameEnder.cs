using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrimGameEnder : MonoBehaviour
{
   public GameManager GameManager;
   public void EndGame()
   {
      GameManager.EndGame();
   }
}
