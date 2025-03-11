using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class Player : MonoBehaviour
{
    private GameManager gameManager;

    private List<GameObject> _dice = new List<GameObject>();
    [SerializeField] private GameObject _diePrefab;
    [SerializeField] private Transform _dieThrowPos;

    [SerializeField] List<GameObject> _selectedDice = new List<GameObject>();
    [SerializeField] private Transform _selectedDiePos;

    private List<Vector3> initialPositions = new List<Vector3>();
    private int nbOfThrows = 0;
    [SerializeField] int maxNbOfThrows = 3;
    private bool CanThrow => nbOfThrows < maxNbOfThrows ? true : false;
    [SerializeField] private float throwShakeThreshold = 9f;


    private void Start()
    {
        gameManager = GameManager.GetInstance();


        for (int i = 0; i < 6; i++)
        {
            GameObject die = Instantiate(_diePrefab);
            die.transform.SetParent(transform, false);
            die.SetActive(false);
            _dice.Add(die);
            initialPositions.Add(die.transform.position);
        }
    }

    void Update()
    {
        switch (gameManager.CurrentGamePhase)
        {
            case GamePhase.WAITINGFORTHROW:
                nbOfThrows = 0;
                if (IsShakeDetected())
                {
                    gameManager.SetCurrentGamePhase(GamePhase.DICETHROWING);
                    ThrowDice();
                }
                break;

            case GamePhase.DICETHROWING:
                if (AllDiceStopped())
                {
                    if (CanThrow)
                    {
                        gameManager.SetCurrentGamePhase(GamePhase.DICESELECTION);
                    }
                    else
                    {
                        ArrangeDice(_dice);
                        gameManager.SetCurrentGamePhase(GamePhase.DICERESOLVING);
                    }
                }
                break;

            case GamePhase.DICESELECTION:
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    HandleDiceSelection();
                }
                if (IsShakeDetected() && CanThrow)
                {
                    gameManager.SetCurrentGamePhase(GamePhase.DICETHROWING);
                    ThrowDice();
                }
                break;

            case GamePhase.DICERESOLVING:
                break;
        }
    }


    private bool IsShakeDetected()
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
                    dieComponent.OldPos= hitObject.transform.position;
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

        for (int i = 0; i < dice.Count; i++)
        {
            GameObject die = dice[i];

            die.GetComponent<Rigidbody>().isKinematic = true;
            Vector3 targetPosition = _selectedDiePos.position + new Vector3(i * spacing, 0f, 0f);
            die.transform.position = targetPosition;
            Die dieComponent = die.GetComponent<Die>();
            dieComponent.SetFaceUp(dieComponent.GetFaceUp());
        }
    }




    private void ThrowDice()
    {
        nbOfThrows++;

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
}
