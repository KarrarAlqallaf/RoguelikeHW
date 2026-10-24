using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
   public class CellData
   {
       public bool Passable;
       public CellObject ContainedObject;
   }

   private CellData[,] m_BoardData;
   private Tilemap m_Tilemap;
   private Grid m_Grid;
   private List<Vector2Int> m_EmptyCellsList;

   [Header("Board")]
   [SerializeField, FormerlySerializedAs("Width"), Range(5, 30)]
   [Tooltip("Number of columns, including the outer wall")]
   private int m_Width = 8;
   [SerializeField, FormerlySerializedAs("Height"), Range(5, 30)]
   [Tooltip("Number of rows, including the outer wall")]
   private int m_Height = 8;
   [SerializeField, FormerlySerializedAs("GroundTiles")] private Tile[] m_GroundTiles;
   [SerializeField, FormerlySerializedAs("WallTiles")] private Tile[] m_WallTiles;

   [Header("Prefabs")]
   [SerializeField, FormerlySerializedAs("FoodPrefab")] private FoodObject m_FoodPrefab;
   [SerializeField, FormerlySerializedAs("WallPrefab")] private WallObject m_WallPrefab;
   [SerializeField, FormerlySerializedAs("ExitCellPrefab")] private ExitCellObject m_ExitCellPrefab;
   [SerializeField, FormerlySerializedAs("EnemyPrefab")] private Enemy m_EnemyPrefab;

   [Header("Spawn Counts")]
   [SerializeField, Range(0, 20)]
   [Tooltip("Minimum number of destructible walls per level")]
   private int m_MinWallCount = 6;
   [SerializeField, Range(0, 20)]
   [Tooltip("Maximum number of destructible walls per level (inclusive)")]
   private int m_MaxWallCount = 9;
   [SerializeField, Range(0, 10)]
   [Tooltip("Minimum number of enemies per level")]
   private int m_MinEnemyCount = 1;
   [SerializeField, Range(0, 10)]
   [Tooltip("Maximum number of enemies per level (inclusive)")]
   private int m_MaxEnemyCount = 3;
   [SerializeField, Range(0, 20)]
   [Tooltip("Number of food items per level")]
   private int m_FoodCount = 5;

   private CellObjectPool<WallObject> m_WallPool;
   private CellObjectPool<Enemy> m_EnemyPool;
   private CellObjectPool<FoodObject> m_FoodPool;
   private CellObjectPool<ExitCellObject> m_ExitPool;

   //called once at game start, sized for the biggest level so no new object is needed later
   public void CreatePools()
   {
       m_WallPool = new CellObjectPool<WallObject>(m_WallPrefab, m_MaxWallCount, transform);
       m_EnemyPool = new CellObjectPool<Enemy>(m_EnemyPrefab, m_MaxEnemyCount, transform);
       m_FoodPool = new CellObjectPool<FoodObject>(m_FoodPrefab, m_FoodCount, transform);
       m_ExitPool = new CellObjectPool<ExitCellObject>(m_ExitCellPrefab, 1, transform);
   }

   public void Init()
   {
       m_Tilemap = GetComponentInChildren<Tilemap>();
       m_Grid = GetComponentInChildren<Grid>();
       //Initialize the list
       m_EmptyCellsList = new List<Vector2Int>();

       m_BoardData = new CellData[m_Width, m_Height];

       for (int y = 0; y < m_Height; ++y)
       {
           for(int x = 0; x < m_Width; ++x)
           {
               Tile tile;
               m_BoardData[x, y] = new CellData();

               if(x == 0 || y == 0 || x == m_Width - 1 || y == m_Height - 1)
               {
                   tile = m_WallTiles[Random.Range(0, m_WallTiles.Length)];
                   m_BoardData[x, y].Passable = false;
               }
               else
               {
                   tile = m_GroundTiles[Random.Range(0, m_GroundTiles.Length)];
                   m_BoardData[x, y].Passable = true;

                   //this is a passable empty cell, add it to the list!
                   m_EmptyCellsList.Add(new Vector2Int(x, y));
               }

               m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
           }
       }

       //remove the starting point of the player! It's not empty, the player is there
       m_EmptyCellsList.Remove(new Vector2Int(1, 1));

       Vector2Int endCoord = new Vector2Int(m_Width - 2, m_Height - 2);
       AddObject(m_ExitPool.Get(), endCoord);
       m_EmptyCellsList.Remove(endCoord);

       GenerateWall();
       GenerateEnemy();
       GenerateFood();
   }

   public void Clean()
   {
       //no board data, so exit early, nothing to clean
       if(m_BoardData == null)
           return;

       for (int y = 0; y < m_Height; ++y)
       {
           for (int x = 0; x < m_Width; ++x)
           {
               var cellData = m_BoardData[x, y];

               if (cellData.ContainedObject != null)
               {
                   //send the object back to its pool instead of destroying it
                   RemoveObject(cellData.ContainedObject);
               }

               SetCellTile(new Vector2Int(x,y), null);
           }
       }
   }

   public Vector3 CellToWorld(Vector2Int cellIndex)
   {
       return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
   }

   public CellData GetCellData(Vector2Int cellIndex)
   {
       if (cellIndex.x < 0 || cellIndex.x >= m_Width
           || cellIndex.y < 0 || cellIndex.y >= m_Height)
       {
           return null;
       }

       return m_BoardData[cellIndex.x, cellIndex.y];
   }

   public void SetCellTile(Vector2Int cellIndex, Tile tile)
   {
       m_Tilemap.SetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0), tile);
   }

   public Tile GetCellTile(Vector2Int cellIndex)
   {
       return m_Tilemap.GetTile<Tile>(new Vector3Int(cellIndex.x, cellIndex.y, 0));
   }

   //Empties the object's cell and recycles the object (replaces Destroy)
   public void RemoveObject(CellObject obj)
   {
       var cellData = GetCellData(obj.Cell);
       if (cellData != null && cellData.ContainedObject == obj)
           cellData.ContainedObject = null;

       switch (obj)
       {
           case WallObject wall: m_WallPool.Release(wall); break;
           case Enemy enemy: m_EnemyPool.Release(enemy); break;
           case FoodObject food: m_FoodPool.Release(food); break;
           case ExitCellObject exit: m_ExitPool.Release(exit); break;
       }
   }

   void AddObject(CellObject obj, Vector2Int coord)
   {
       CellData data = m_BoardData[coord.x, coord.y];
       obj.transform.position = CellToWorld(coord);
       data.ContainedObject = obj;
       obj.Init(coord);
   }

   void GenerateWall()
   {
       int wallCount = Random.Range(m_MinWallCount, m_MaxWallCount + 1);
       for (int i = 0; i < wallCount && m_EmptyCellsList.Count > 0; ++i)
       {
           int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
           Vector2Int coord = m_EmptyCellsList[randomIndex];

           m_EmptyCellsList.RemoveAt(randomIndex);
           AddObject(m_WallPool.Get(), coord);
       }
   }

   void GenerateEnemy()
   {
       int enemyCount = Random.Range(m_MinEnemyCount, m_MaxEnemyCount + 1);
       for (int i = 0; i < enemyCount && m_EmptyCellsList.Count > 0; ++i)
       {
           int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
           Vector2Int coord = m_EmptyCellsList[randomIndex];

           m_EmptyCellsList.RemoveAt(randomIndex);
           AddObject(m_EnemyPool.Get(), coord);
       }
   }

   void GenerateFood()
   {
       for (int i = 0; i < m_FoodCount && m_EmptyCellsList.Count > 0; ++i)
       {
           int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
           Vector2Int coord = m_EmptyCellsList[randomIndex];

           m_EmptyCellsList.RemoveAt(randomIndex);
           AddObject(m_FoodPool.Get(), coord);
       }
   }
}
