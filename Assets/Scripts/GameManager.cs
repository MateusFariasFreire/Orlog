using TMPro;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GamePhase { WAITINGFORTHROW = 0, DICETHROWING, DICESELECTION, GODFAVOR, DICERESOLVING, ANIMATION, GAMEENDED }
public class GameManager : MonoBehaviour
{
    private GamePhase _gamePhase = GamePhase.WAITINGFORTHROW;
    public GamePhase CurrentGamePhase { get => _gamePhase; }

    [SerializeField] private Player _player1;
    [SerializeField] private Player _player2;
    private Player _currentPlayer;
    public Player CurrentPlayer { get => _currentPlayer; }
    private Player _opponentPlayer;
    public Player OpponentPlayer { get => _opponentPlayer; }

    private static GameManager _instance;
    [SerializeField] private Canvas _godsPanel;
    [SerializeField] private Canvas _gameEndOverlay;

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
        _gameEndOverlay.gameObject.SetActive(false);
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
            case GamePhase.GODFAVOR:
                Debug.Log("God favor");
                break;
            case GamePhase.DICERESOLVING:
                Debug.Log("Resolving dice");
                break;
            case GamePhase.ANIMATION:
                Debug.Log("Animation");
                break;
            case GamePhase.GAMEENDED:
                Debug.Log("Game ended");
                break;
            default:
                Debug.Log("Oulah, pas normal");
                break;
        }
    }

    public void SetCurrentGamePhase(GamePhase newGamePhase)
    {
        if (_gamePhase == GamePhase.GODFAVOR && newGamePhase == GamePhase.DICERESOLVING)
        {
            CurrentPlayer.InitDiceResolving();

        }

        _gamePhase = newGamePhase;

        

        if (_gamePhase == GamePhase.GODFAVOR)
        {
            CurrentPlayer.GodFavorButton.interactable = true;
            CurrentPlayer.SkipGodFavorPhaseButton.gameObject.SetActive(true);
        }
        else
        {
            CurrentPlayer.GodFavorButton.interactable = false;
            CurrentPlayer.SkipGodFavorPhaseButton.gameObject.SetActive(false);
        }

        
    }

    public void AttackMelee()
    {
        if (_opponentPlayer.TakeMeleeDamage())
        {
            EndGame();
        }
    }

    public void AttackRange()
    {
        if (_opponentPlayer.TakeRangeDamage())
        {
            EndGame();
        }
    }

    public bool StealGodFavor()
    {
        return _opponentPlayer.StealGodFavor();
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
        _player1.gameObject.SetActive(false);
        _player2.gameObject.SetActive(false);

        int winnerNumber = _currentPlayer == _player1 ? 1 : 2;
        string winnerText = $"Le joueur {winnerNumber} remporte la partie!";

        _gameEndOverlay.transform.Find("WinnerText").GetComponent<TMP_Text>().SetText(winnerText);
        _gameEndOverlay.gameObject.SetActive(true);
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

    public void BuyGodFavor(string godName)
    {
        switch (godName)
        {
            case "Odin":
                if (CurrentPlayer.GodFavorTokens >= 10)
                {
                    CurrentPlayer.Heal(3);
                    CurrentPlayer.RemoveGodFavor(10);
                    SetCurrentGamePhase(GamePhase.DICERESOLVING);
                    _godsPanel.gameObject.SetActive(false);
                }
                break;
            case "Thor":
                if (CurrentPlayer.GodFavorTokens >= 8)
                {
                    if (_opponentPlayer.Attack(4))
                    {
                        EndGame();
                        _godsPanel.gameObject.SetActive(false);
                        return;
                    }
                    CurrentPlayer.RemoveGodFavor(8);
                    SetCurrentGamePhase(GamePhase.DICERESOLVING);
                    _godsPanel.gameObject.SetActive(false);
                }
                break;
            case "Loki":
                if (CurrentPlayer.GodFavorTokens >= 9)
                {
                    CurrentPlayer.RemoveGodFavor(9);
                    CurrentPlayer.HasLokiFavor = true;
                    SetCurrentGamePhase(GamePhase.DICERESOLVING);
                    _godsPanel.gameObject.SetActive(false);
                }
                break;
        }
    }

    public void CloseGodsPannel()
    {
        _godsPanel.gameObject.SetActive(false);

    }

    public void OnPlayAgainPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
