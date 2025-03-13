using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using System.ComponentModel;

public enum GamePhase { WAITINGFORTHROW = 0, DICETHROWING, DICESELECTION, DICERESOLVING, ANIMATION, GAMEENDED }
public class GameManager : MonoBehaviour
{
    private GamePhase _gamePhase = GamePhase.WAITINGFORTHROW;
    public GamePhase CurrentGamePhase => _gamePhase;

    [SerializeField] private Player _player1;
    [SerializeField] private Player _player2;
    private Player _currentPlayer;
    public Player CurrentPlayer { get => _currentPlayer; }
    private Player _opponentPlayer;
    public Player OpponentPlayer { get => _opponentPlayer; }

    private static GameManager _instance;
    [SerializeField] private Canvas _godsPanel;

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

    private void Start()
    {
        _currentPlayer = _player1;
        _opponentPlayer = _player2;
        _godsPanel.gameObject.SetActive(false);
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

    public void AttackMelee()
    {
        if (_opponentPlayer.AttackMelee())
        {
            EndGame();
        }
    }

    public void AttackRange()
    {
        if (_opponentPlayer.AttackRange())
        {
            EndGame();
        }
    }

    public void Steal()
    {
        if (_opponentPlayer.Steal())
        {
            _currentPlayer.AddGodFavorToken();
        }
    }

    public void EndTurn()
    {
        Player temp = _currentPlayer;
        _currentPlayer = _opponentPlayer;
        _opponentPlayer = temp;
        SetCurrentGamePhase(GamePhase.WAITINGFORTHROW);
        _currentPlayer.StartNewTurn();
    }

    public void EndGame()
    {
        SetCurrentGamePhase(GamePhase.GAMEENDED);

        if (_currentPlayer == _player1)
        {
            Debug.Log("Player 1 wins");
        }
        else
        {
            Debug.Log("Player 2 wins");
        }
    }

    public void OpenGodsPannelPlayer1()
    {
        if (_player1 == _currentPlayer)
        {
            _godsPanel.gameObject.SetActive(true);
            _godsPanel.gameObject.transform.GetChild(0).transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    public void OpenGodsPannelPlayer2()
    {
        if (_player2 == _currentPlayer)
        {
            _godsPanel.gameObject.SetActive(true);
            _godsPanel.gameObject.transform.GetChild(0).transform.rotation = Quaternion.Euler(0, 0, 180);
        }
    }

    public void BuyGodFavor(string GodName)
    {

    }

    public void CloseGodsPannel()
    {
        _godsPanel.gameObject.SetActive(false);

    }
}
