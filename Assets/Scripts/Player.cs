using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using TMPro;

public class Player : MonoBehaviour
{
    private GameManager _gameManager;

    private List<GameObject> _dice = new List<GameObject>();
    [SerializeField] private GameObject _diePrefab;
    [SerializeField] private Transform _dieThrowPos;

    List<GameObject> _selectedDice = new List<GameObject>();
    [SerializeField] private Transform _selectedDiePos;

    private int _nbOfThrows = 0;
    [SerializeField] private int _maxNbOfThrows = 3;
    private bool CanThrow => _nbOfThrows < _maxNbOfThrows;
    [SerializeField] private float throwShakeThreshold = 9f;


     private int _healthPoints = 15;
    private int _godFavorToken = 0;
     private int _shield = 0;
    private int _helmet = 0;

    [SerializeField] TMP_Text _healthPointText;
    [SerializeField] TMP_Text _godFavorTokenText;


    private void Start()
    {
        _gameManager = GameManager.GetInstance();


        for (int i = 0; i < 6; i++)
        {
            GameObject die = Instantiate(_diePrefab);
            die.transform.SetParent(transform, false);
            die.SetActive(false);
            _dice.Add(die);
        }

        _healthPointText.SetText(_healthPoints.ToString());
        _godFavorTokenText.SetText(_godFavorToken.ToString());
    }

    void Update()
    {
        if (_gameManager.CurrentPlayer != this)
        {
            return;
        }

        switch (_gameManager.CurrentGamePhase)
        {
            case GamePhase.WAITINGFORTHROW:
                if (ShakeDetected())
                {
                    _gameManager.SetCurrentGamePhase(GamePhase.DICETHROWING);
                    ThrowDice();
                }
                break;

            case GamePhase.DICETHROWING:
                if (AllDiceStopped())
                {
                    if (CanThrow)
                    {
                        _gameManager.SetCurrentGamePhase(GamePhase.DICESELECTION);
                    }
                    else
                    {
                        ArrangeDice(_dice);
                        _gameManager.SetCurrentGamePhase(GamePhase.DICERESOLVING);
                    }
                }
                break;

            case GamePhase.DICESELECTION:

                if (AllDiceSelected() || !CanThrow)
                {
                    ArrangeDice(_dice);
                    _gameManager.SetCurrentGamePhase(GamePhase.DICERESOLVING);
                }
                else
                {
                    if (ShakeDetected())
                    {
                        _gameManager.SetCurrentGamePhase(GamePhase.DICETHROWING);
                        ThrowDice();
                    }

                    if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    {
                        HandleDiceSelection();
                    }
                }
                break;

            case GamePhase.DICERESOLVING:

                ResolveDice();
                _gameManager.EndTurn();

                break;
        }
    }

    private void ResolveDice()
    {
        List<GameObject> activeDice = new List<GameObject>();

        foreach (GameObject die in _dice)
        {
            if (die.activeSelf)
            {
                activeDice.Add(die);
            }
        }

        foreach (GameObject die in activeDice)
        {
            Die dieComponent = die.GetComponent<Die>();

            switch (dieComponent.GetFaceUp())
            {
                case Die.DieFace.Axe1:
                case Die.DieFace.Axe2:
                    _gameManager.AttackMelee();
                    die.SetActive(false);
                    ArrangeDice(_dice);
                    break;

                case Die.DieFace.Helmet:
                    AddHelmet();
                    AddGodFavorToken();
                    break;

                case Die.DieFace.Arrow:
                    _gameManager.AttackRange();
                    die.SetActive(false);
                    ArrangeDice(_dice);
                    break;

                case Die.DieFace.Shield:
                    AddShield();
                    AddGodFavorToken();
                    break;

                case Die.DieFace.Steal:
                    _gameManager.Steal();
                    die.SetActive(false);
                    ArrangeDice(_dice);
                    break;
            }
        }
    }


    private bool ShakeDetected()
    {
        if (Accelerometer.current == null) return false;

        Vector3 acceleration = Accelerometer.current.acceleration.ReadValue();
        return acceleration.sqrMagnitude > throwShakeThreshold;
    }

    private void HandleDiceSelection()
    {
        if (!Touchscreen.current.primaryTouch.press.isPressed)
        {
            return;
        }

        Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(touchPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (_dice.Contains(hitObject))
            {
                Die dieComponent = hitObject.GetComponent<Die>();
                bool isNowSelected = !dieComponent.IsSelected;
                dieComponent.IsSelected = isNowSelected;

                if (isNowSelected)
                {
                    dieComponent.OldPos = hitObject.transform.position;
                    dieComponent.OldRot = hitObject.transform.rotation;
                    _selectedDice.Add(hitObject);
                }
                else
                {
                    hitObject.transform.SetPositionAndRotation(dieComponent.OldPos, dieComponent.OldRot);
                    _selectedDice.Remove(hitObject);
                }

                ArrangeDice(_selectedDice);
            }
        }

    }

    private void ArrangeDice(List<GameObject> dice)
    {
        float spacing = 1.7f;

        // On récupère uniquement les dés actifs
        List<GameObject> activeDice = new List<GameObject>();
        foreach (GameObject die in dice)
        {
            if (die.activeSelf)
            {
                activeDice.Add(die);
            }
        }

        // Calcule la direction vers la droite selon la rotation du joueur
        Vector3 rightDirection = _selectedDiePos.right; // Vers la droite selon la rotation du transform

        for (int i = 0; i < activeDice.Count; i++)
        {
            GameObject die = activeDice[i];

            die.GetComponent<Rigidbody>().isKinematic = true;

            Vector3 targetPosition = _selectedDiePos.position + rightDirection * (i * spacing);
            die.transform.position = targetPosition;

            Die dieComponent = die.GetComponent<Die>();
            dieComponent.SetFaceUp(dieComponent.GetFaceUp());
        }
    }





    private void ThrowDice()
    {
        _nbOfThrows++;

        foreach (GameObject die in _dice)
        {
            if (!die.activeSelf)
                die.SetActive(true);

            if (die.GetComponent<Die>().IsSelected)
                continue;

            die.GetComponent<Rigidbody>().isKinematic = false;

            // Définir la position et la rotation aléatoires pour le lancer
            float randomX = Random.Range(-1.3f, 1.3f);
            float randomZ = Random.Range(-1.3f, 1.3f);
            float offsetX = Random.Range(-0.2f, 0.2f);
            float offsetZ = Random.Range(-0.2f, 0.2f);

            Vector3 randomPos = new Vector3(randomX + offsetX, _dieThrowPos.position.y, randomZ + offsetZ);
            Quaternion randomRot = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
            Vector3 randomAngularVelocity = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f));

            die.transform.position = randomPos;
            die.transform.rotation = randomRot;

            Rigidbody rb = die.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = randomAngularVelocity;
        }
    }

    private bool AllDiceStopped()
    {
        foreach (GameObject die in _dice)
        {
            if (die.GetComponent<Die>().GetFaceUp() == Die.DieFace.NotStopped)
            {
                return false;
            }
        }
        return true;
    }

    private bool AllDiceSelected()
    {
        foreach (GameObject die in _dice)
        {
            if (!die.GetComponent<Die>().IsSelected)
            {
                return false;
            }
        }
        return true;
    }

    public bool AttackMelee()
    {
        if (_helmet > 0)
        {
            _helmet--;
        }
        else
        {
            _healthPoints--;
            _healthPointText.SetText(_healthPoints.ToString());
        }

        return _healthPoints <= 0;

    }

    public bool AttackRange()
    {
        if (_shield > 0)
        {
            _shield--;
        }
        else
        {
            _healthPoints--;
            _healthPointText.SetText(_healthPoints.ToString());
        }

        return _healthPoints <= 0;
    }

    public void AddGodFavorToken()
    {
        _godFavorToken++;
        _godFavorTokenText.SetText(_godFavorToken.ToString());
    }

    public void AddHelmet()
    {
        _helmet++;
    }

    public void AddShield()
    {
        _shield++;
    }

    public bool Steal()
    {
        if (_godFavorToken > 0)
        {
            _godFavorToken--;
            _godFavorTokenText.SetText(_godFavorToken.ToString());
            return true;
        }
        return false;
    }

    public void StartNewTurn()
    {
        foreach (GameObject die in _dice)
        {
            die.SetActive(false);
            die.GetComponent<Rigidbody>().isKinematic = false;
            die.GetComponent<Die>().IsSelected = false;
            _selectedDice.Clear();
            _helmet = 0;
            _shield = 0;
            _nbOfThrows = 0;
        }
    }
}
