using UnityEngine;

public class FoodObject : CellObject
{
    public int AmountGranted = 10;

    public override void PlayerEntered()
    {
        Destroy(gameObject);
        GameManager.Instance.PlaySound(GameManager.Instance.FoodPickupClip);

        //increase food
        GameManager.Instance.ChangeFood(AmountGranted);
    }
}
