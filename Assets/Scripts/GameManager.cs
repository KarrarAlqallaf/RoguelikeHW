using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
   public static GameManager Instance { get; private set; }

   [SerializeField, FormerlySerializedAs("BoardManager")]
   private BoardManager m_BoardManager;
   [SerializeField, FormerlySerializedAs("PlayerController")]
   private PlayerController m_PlayerController;

   [SerializeField, FormerlySerializedAs("UIDoc")]
   private UIDocument m_UIDoc;
   private Label m_FoodLabel;
   private VisualElement m_GameOverPanel;
   private Label m_GameOverMessage;

   [SerializeField, Range(10, 300)]
   [Tooltip("Amount of food the player starts a new game with")]
   private int m_StartingFood = 100;

   [Header("Audio")]
   [SerializeField, FormerlySerializedAs("SfxSource")]
   [Tooltip("AudioSource used to play the sound effects with PlayOneShot")]
   private AudioSource m_SfxSource;
   [SerializeField, FormerlySerializedAs("MoveClip")] private AudioClip m_MoveClip;
   [SerializeField, FormerlySerializedAs("WallAttackClip")] private AudioClip m_WallAttackClip;
   [SerializeField, FormerlySerializedAs("FoodPickupClip")] private AudioClip m_FoodPickupClip;
   [SerializeField, FormerlySerializedAs("EnemyAttackClip")] private AudioClip m_EnemyAttackClip;
   [SerializeField, FormerlySerializedAs("EnemyDeathClip")] private AudioClip m_EnemyDeathClip;
   [SerializeField, FormerlySerializedAs("GameOverClip")] private AudioClip m_GameOverClip;

   [Header("Particle Effects")]
   [SerializeField, FormerlySerializedAs("WallDestroyEffect")] private ParticleSystem m_WallDestroyEffect;
   [SerializeField, FormerlySerializedAs("FoodPickupEffect")] private ParticleSystem m_FoodPickupEffect;
   [SerializeField, FormerlySerializedAs("EnemyDeathEffect")] private ParticleSystem m_EnemyDeathEffect;

   public BoardManager BoardManager => m_BoardManager;
   public PlayerController PlayerController => m_PlayerController;
   public TurnManager TurnManager { get; private set; }

   public AudioClip MoveClip => m_MoveClip;
   public AudioClip WallAttackClip => m_WallAttackClip;
   public AudioClip FoodPickupClip => m_FoodPickupClip;
   public AudioClip EnemyAttackClip => m_EnemyAttackClip;
   public AudioClip EnemyDeathClip => m_EnemyDeathClip;

   public ParticleSystem WallDestroyEffect => m_WallDestroyEffect;
   public ParticleSystem FoodPickupEffect => m_FoodPickupEffect;
   public ParticleSystem EnemyDeathEffect => m_EnemyDeathEffect;

   private bool m_IsGameOver;

   private Dictionary<ParticleSystem, Queue<ParticleSystem>> m_EffectPool = new Dictionary<ParticleSystem, Queue<ParticleSystem>>();
   private List<ParticleSystem> m_ActiveEffects = new List<ParticleSystem>();

   private int m_FoodAmount;
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

       m_FoodLabel = m_UIDoc.rootVisualElement.Q<Label>("FoodLabel");

       m_GameOverPanel = m_UIDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
       m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");

       //pre-instantiate the cell objects once, they are recycled between levels
       m_BoardManager.CreatePools();

       StartNewGame();
   }

   public void StartNewGame()
   {
       m_GameOverPanel.style.visibility = Visibility.Hidden;
       m_IsGameOver = false;
       StopAllEffects();

       m_CurrentLevel = 1;
       m_FoodAmount = m_StartingFood;
       m_FoodLabel.text = "Food : " + m_FoodAmount;

       m_BoardManager.Clean();
       m_BoardManager.Init();

       m_PlayerController.Init();
       m_PlayerController.Spawn(m_BoardManager, new Vector2Int(1,1));
   }

   public void NewLevel()
   {
       StopAllEffects();
       m_BoardManager.Clean();
       m_BoardManager.Init();
       m_PlayerController.Spawn(m_BoardManager, new Vector2Int(1,1));

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
           PlaySound(m_GameOverClip);
           m_PlayerController.GameOver();
           m_GameOverPanel.style.visibility = Visibility.Visible;
           m_GameOverMessage.text = "Game Over!\n\nSurvived " + m_CurrentLevel + " days\n\nPress Enter to restart";
       }
   }

   public void PlaySound(AudioClip clip)
   {
       if (clip != null)
           m_SfxSource.PlayOneShot(clip);
   }

   public void PlayEffect(ParticleSystem prefab, Vector3 position)
   {
       if (prefab == null)
           return;

       if (!m_EffectPool.TryGetValue(prefab, out var pool))
       {
           pool = new Queue<ParticleSystem>();
           m_EffectPool[prefab] = pool;
       }

       ParticleSystem effect;
       if (pool.Count > 0)
       {
           effect = pool.Dequeue();
       }
       else
       {
           effect = Instantiate(prefab, transform);
           effect.gameObject.AddComponent<PooledEffect>().Init(prefab);
       }

       effect.transform.position = position;
       effect.gameObject.SetActive(true);
       effect.Play();
       m_ActiveEffects.Add(effect);
   }

   public void ReturnEffect(ParticleSystem effect)
   {
       //already returned (e.g. by StopAllEffects)
       if (!m_ActiveEffects.Remove(effect))
           return;

       effect.gameObject.SetActive(false);
       m_EffectPool[effect.GetComponent<PooledEffect>().Prefab].Enqueue(effect);
   }

   void StopAllEffects()
   {
       //copy the list as ReturnEffect removes from it
       foreach (var effect in m_ActiveEffects.ToArray())
       {
           effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
           ReturnEffect(effect);
       }
   }
}
