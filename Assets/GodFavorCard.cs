using UnityEngine;
using UnityEngine.UI;

public class GodFavorCard : MonoBehaviour
{
    [SerializeField] private string _godName;
    [SerializeField] private GameObject _description;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _description.SetActive(false);
        gameObject.GetComponent<Image>().color = Color.white;
    }

    private void OnEnable()
    {
        _description.SetActive(false);
        gameObject.GetComponent<Image>().color = Color.white;
    }

    public void OnCardPressed()
    {
        if (_description.activeSelf)
        {
        }
        else
        {
            _description.SetActive(true);
            gameObject.GetComponent<Image>().color = new Color32(75,80,95,255);
        }
    }
}
