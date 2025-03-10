using UnityEngine;

public enum GamePhase { WAITINGFORTHROW = 0, DICETHROWING, DICESELECTION, DICERESOLVING }
public class GameManager : MonoBehaviour
{
    private GamePhase _gamePhase = GamePhase.WAITINGFORTHROW;
    public GamePhase CurrentGamePhase => _gamePhase;

    private static GameManager _instance;

    public static GameManager GetInstance()
    {
        if (_instance == null)
        {
            _instance = FindFirstObjectByType<GameManager>();
            if (_instance == null)
            {
                GameObject go = new GameObject("GameManager");
                _instance = go.AddComponent<GameManager>();
            }
        }

        return _instance;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        switch (_gamePhase)
        {
            case GamePhase.WAITINGFORTHROW:
                Debug.Log("Waiting for throw");
                break;
            case GamePhase.DICETHROWING:
                Debug.Log("Throwing dice");
                break;
            case GamePhase.DICESELECTION:
                Debug.Log("Selecting dice");
                break;
            case GamePhase.DICERESOLVING:
                Debug.Log("Resolving dice");
                break;
        }
    }

    public void SetCurrentGamePhase(GamePhase gamePhase)
    {
        _gamePhase = gamePhase;
    }
}
