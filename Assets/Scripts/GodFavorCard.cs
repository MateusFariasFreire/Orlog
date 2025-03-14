using UnityEngine;
using UnityEngine.UI;

public class GodFavorCard : MonoBehaviour
{
    [SerializeField] private string _godName;
    [SerializeField] private GameObject _description;


    void Start()
    {
        _description.SetActive(true);
        gameObject.GetComponent<Image>().color = new Color32(75, 80, 95, 255);
    }


    public void OnCardPressed()
    {
        GameManager.GetInstance().BuyGodFavor(_godName);
    }
}
