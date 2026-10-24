using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
   public static GameManager Instance { get; private set; }

   public BoardManager BoardManager;
   public PlayerController PlayerController;

   public UIDocument UIDoc;
   private Label m_FoodLabel;
   private VisualElement m_GameOverPanel;
   private Label m_GameOverMessage;

   public TurnManager TurnManager { get; private set; }

   public AudioSource SfxSource;
   public AudioClip MoveClip;
   public AudioClip WallAttackClip;
   public AudioClip FoodPickupClip;
   public AudioClip EnemyAttackClip;
   public AudioClip EnemyDeathClip;
   public AudioClip GameOverClip;

   private bool m_IsGameOver;

   private int m_FoodAmount = 100;
   private int m_CurrentLevel = 1;

   private void Awake()
   {
       if (Instance != null)
       {
           Destroy(gameObject);
           return;
       }

       Instance = this;
   }

   void Start()
   {
       TurnManager = new TurnManager();
       TurnManager.OnTick += OnTurnHappen;

       m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");

       m_GameOverPanel = UIDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
       m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");

       StartNewGame();
   }

   public void StartNewGame()
   {
       m_GameOverPanel.style.visibility = Visibility.Hidden;
       m_IsGameOver = false;

       m_CurrentLevel = 1;
       m_FoodAmount = 100;
       m_FoodLabel.text = "Food : " + m_FoodAmount;

       BoardManager.Clean();
       BoardManager.Init();

       PlayerController.Init();
       PlayerController.Spawn(BoardManager, new Vector2Int(1,1));
   }

   public void NewLevel()
   {
       BoardManager.Clean();
       BoardManager.Init();
       PlayerController.Spawn(BoardManager, new Vector2Int(1,1));

       m_CurrentLevel++;
   }

   void OnTurnHappen()
   {
       ChangeFood(-1);
   }

   public void ChangeFood(int amount)
   {
       m_FoodAmount += amount;
       m_FoodLabel.text = "Food : " + m_FoodAmount;

       if (m_FoodAmount <= 0 && !m_IsGameOver)
       {
           m_IsGameOver = true;
           PlaySound(GameOverClip);
           PlayerController.GameOver();
           m_GameOverPanel.style.visibility = Visibility.Visible;
           m_GameOverMessage.text = "Game Over!\n\nSurvived " + m_CurrentLevel + " days\n\nPress Enter to restart";
       }
   }

   public void PlaySound(AudioClip clip)
   {
       if (clip != null)
           SfxSource.PlayOneShot(clip);
   }
}
