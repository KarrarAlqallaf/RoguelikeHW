using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class WallObject : CellObject
{
    [SerializeField, FormerlySerializedAs("ObstacleTile")]
    [Tooltip("Tile shown on the cell while the wall is standing")]
    private Tile m_ObstacleTile;
    [SerializeField, FormerlySerializedAs("MaxHealth"), Range(1, 10)]
    [Tooltip("Number of hits needed to destroy the wall")]
    private int m_MaxHealth = 3;

    private int m_HealthPoint;
    private Tile m_OriginalTile;

    public override void Init(Vector2Int cell)
    {
        base.Init(cell);

        m_HealthPoint = m_MaxHealth;

        m_OriginalTile = GameManager.Instance.BoardManager.GetCellTile(cell);
        GameManager.Instance.BoardManager.SetCellTile(cell, m_ObstacleTile);
    }

    public override bool PlayerWantsToEnter()
    {
        m_HealthPoint -= 1;
        GameManager.Instance.PlaySound(GameManager.Instance.WallAttackClip);

        if (m_HealthPoint > 0)
        {
            return false;
        }

        GameManager.Instance.BoardManager.SetCellTile(m_Cell, m_OriginalTile);
        GameManager.Instance.PlayEffect(GameManager.Instance.WallDestroyEffect, transform.position);
        //recycle the wall instead of destroying it
        GameManager.Instance.BoardManager.RemoveObject(this);
        return true;
    }
}
