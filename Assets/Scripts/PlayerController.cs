using UnityEngine;

public class PlayerController : MonoBehaviour
{
   private BoardManager m_Board;
   private Vector2Int m_CellPosition;

   public void Spawn(BoardManager boardManager, Vector2Int cell)
   {
       m_Board = boardManager;
       m_CellPosition = cell;

       //let's move to the right position...
       transform.position = m_Board.CellToWorld(cell);
   }
}