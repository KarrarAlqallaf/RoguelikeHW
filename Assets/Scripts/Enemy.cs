using UnityEngine;
using UnityEngine.Serialization;

public class Enemy : CellObject
{
    [SerializeField, FormerlySerializedAs("Health"), Range(1, 10)]
    [Tooltip("Number of hits needed to kill the enemy")]
    private int m_Health = 3;
    [SerializeField, Range(1, 20)]
    [Tooltip("Food removed from the player on each attack")]
    private int m_AttackDamage = 3;

    private int m_CurrentHealth;

    //enemies are pooled, so only listen to turns while active on the board
    private void OnEnable()
    {
        GameManager.Instance.TurnManager.OnTick += TurnHappened;
    }

    private void OnDisable()
    {
        GameManager.Instance.TurnManager.OnTick -= TurnHappened;
    }

    public override void Init(Vector2Int coord)
    {
        base.Init(coord);
        m_CurrentHealth = m_Health;
    }

    public override bool PlayerWantsToEnter()
    {
        m_CurrentHealth -= 1;

        if (m_CurrentHealth <= 0)
        {
            GameManager.Instance.PlaySound(GameManager.Instance.EnemyDeathClip);
            GameManager.Instance.PlayEffect(GameManager.Instance.EnemyDeathEffect, transform.position);
            //recycle the enemy instead of destroying it
            GameManager.Instance.BoardManager.RemoveObject(this);
        }

        return false;
    }

    bool MoveTo(Vector2Int coord)
    {
        var board = GameManager.Instance.BoardManager;
        var targetCell = board.GetCellData(coord);

        if (targetCell == null
            || !targetCell.Passable
            || targetCell.ContainedObject != null)
        {
            return false;
        }

        //remove enemy from current cell
        var currentCell = board.GetCellData(m_Cell);
        currentCell.ContainedObject = null;

        //add it to the next cell
        targetCell.ContainedObject = this;
        m_Cell = coord;
        transform.position = board.CellToWorld(coord);

        return true;
    }

    void TurnHappened()
    {
        var playerCell = GameManager.Instance.PlayerController.Cell;

        int xDist = playerCell.x - m_Cell.x;
        int yDist = playerCell.y - m_Cell.y;

        int absXDist = Mathf.Abs(xDist);
        int absYDist = Mathf.Abs(yDist);

        if ((xDist == 0 && absYDist == 1)
            || (yDist == 0 && absXDist == 1))
        {
            //we are adjacent to the player, attack!
            GameManager.Instance.PlaySound(GameManager.Instance.EnemyAttackClip);
            GameManager.Instance.ChangeFood(-m_AttackDamage);
        }
        else
        {
            if (absXDist > absYDist)
            {
                if (!TryMoveInX(xDist))
                {
                    //if our move was not successful (so no move and not attack)
                    //we try to move along Y
                    TryMoveInY(yDist);
                }
            }
            else
            {
                if (!TryMoveInY(yDist))
                {
                    TryMoveInX(xDist);
                }
            }
        }
    }

    bool TryMoveInX(int xDist)
    {
        //try to get closer in x

        //player to our right
        if (xDist > 0)
        {
            return MoveTo(m_Cell + Vector2Int.right);
        }

        //player to our left
        return MoveTo(m_Cell + Vector2Int.left);
    }

    bool TryMoveInY(int yDist)
    {
        //try to get closer in y

        //player on top
        if (yDist > 0)
        {
            return MoveTo(m_Cell + Vector2Int.up);
        }

        //player below
        return MoveTo(m_Cell + Vector2Int.down);
    }
}
