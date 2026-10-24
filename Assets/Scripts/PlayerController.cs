using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("MoveSpeed"), Range(1f, 20f)]
    [Tooltip("Movement speed in world units per second when moving between cells")]
    private float m_MoveSpeed = 5.0f;

    private BoardManager m_Board;
    private Vector2Int m_CellPosition;
    private bool m_IsGameOver;
    private bool m_IsMoving;
    private Coroutine m_MoveCoroutine;

    private Animator m_Animator;

    public Vector2Int Cell => m_CellPosition;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    public void Init()
    {
        m_IsGameOver = false;
    }

    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        m_Board = boardManager;
        MoveTo(cell, true);
    }

    public void MoveTo(Vector2Int cell, bool immediate)
    {
        m_CellPosition = cell;

        //cancel any movement in progress (e.g. respawning on a new level)
        if (m_MoveCoroutine != null)
        {
            StopCoroutine(m_MoveCoroutine);
            m_MoveCoroutine = null;
        }

        if (immediate)
        {
            m_IsMoving = false;
            transform.position = m_Board.CellToWorld(m_CellPosition);
            m_Animator.SetBool("Moving", false);
        }
        else
        {
            GameManager.Instance.PlaySound(GameManager.Instance.MoveClip);
            m_MoveCoroutine = StartCoroutine(MoveRoutine(m_Board.CellToWorld(m_CellPosition)));
        }
    }

    IEnumerator MoveRoutine(Vector3 target)
    {
        //input is ignored in Update while this is true, so no cell can be skipped
        m_IsMoving = true;
        m_Animator.SetBool("Moving", true);

        Vector3 start = transform.position;
        float duration = Vector3.Distance(start, target) / m_MoveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        transform.position = target;
        m_IsMoving = false;
        m_MoveCoroutine = null;
        m_Animator.SetBool("Moving", false);

        var cellData = m_Board.GetCellData(m_CellPosition);
        if (cellData.ContainedObject != null)
            cellData.ContainedObject.PlayerEntered();
    }

    public void GameOver()
    {
        m_IsGameOver = true;

        //freeze the player where it is, it gets respawned on restart
        if (m_MoveCoroutine != null)
        {
            StopCoroutine(m_MoveCoroutine);
            m_MoveCoroutine = null;
        }
        m_IsMoving = false;
        m_Animator.SetBool("Moving", false);
    }

    private void Update()
    {
        if (m_IsGameOver)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                GameManager.Instance.StartNewGame();
            }

            return;
        }

        //block input until the movement coroutine has reached the cell
        if (m_IsMoving)
            return;

        Vector2Int newCellTarget = m_CellPosition;
        bool hasMoved = false;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            newCellTarget.y -= 1;
            hasMoved = true;
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x += 1;
            hasMoved = true;
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            newCellTarget.x -= 1;
            hasMoved = true;
        }

        if (hasMoved)
        {
            //check if the new position is passable, then move there if it is.
            BoardManager.CellData cellData = m_Board.GetCellData(newCellTarget);

            if (cellData != null && cellData.Passable)
            {
                GameManager.Instance.TurnManager.Tick();

                if (cellData.ContainedObject == null)
                {
                    MoveTo(newCellTarget, false);
                }
                else if (cellData.ContainedObject.PlayerWantsToEnter())
                {
                    //PlayerEntered is called once the player reaches the cell (see MoveRoutine)
                    MoveTo(newCellTarget, false);
                }
                else
                {
                    //the object blocked the player (e.g. a wall), so play the attack animation
                    m_Animator.SetTrigger("Attack");
                }
            }
        }
    }
}
