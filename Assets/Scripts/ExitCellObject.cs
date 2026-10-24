using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class ExitCellObject : CellObject
{
    [SerializeField, FormerlySerializedAs("EndTile")]
    private Tile m_EndTile;

    public override void Init(Vector2Int coord)
    {
        base.Init(coord);
        GameManager.Instance.BoardManager.SetCellTile(coord, m_EndTile);
    }

    public override void PlayerEntered()
    {
        GameManager.Instance.NewLevel();
    }
}
