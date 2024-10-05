using UnityEngine;

public class GrimGameEnder : MonoBehaviour
{
   public GameManager GameManager;
   public void EndGame()
   {
      GameManager.EndGame("DEATH BY\nDARKNESS");
   }
}
