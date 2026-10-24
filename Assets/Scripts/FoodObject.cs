using UnityEngine;
using UnityEngine.Serialization;

public class FoodObject : CellObject
{
    [SerializeField, FormerlySerializedAs("AmountGranted"), Range(1, 50)]
    [Tooltip("Food added to the player when picked up")]
    private int m_AmountGranted = 10;

    public override void PlayerEntered()
    {
        //recycle the food instead of destroying it
        GameManager.Instance.BoardManager.RemoveObject(this);
        GameManager.Instance.PlaySound(GameManager.Instance.FoodPickupClip);
        GameManager.Instance.PlayEffect(GameManager.Instance.FoodPickupEffect, transform.position);

        //increase food
        GameManager.Instance.ChangeFood(m_AmountGranted);
    }
}
