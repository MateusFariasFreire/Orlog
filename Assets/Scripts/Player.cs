using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using UnityEngine;

public class Player : MonoBehaviour
{
    private GameManager gameManager;

    private bool finishedTurn = false;


    private List<GameObject> dice = new List<GameObject>();

    [SerializeField] private GameObject diePrefab;
    [SerializeField] private Transform diceSpawnPos;

    private int nbOfThrows = 0;

    private void Start()
    {
        gameManager = GameManager.GetInstance();

        for (int i = 0; i < 6; i++)
        {
            GameObject die = Instantiate(diePrefab);
            die.transform.SetParent(transform, false);
            die.SetActive(false); // On le d�sactive
            dice.Add(die);
        }
    }



    void Update()
    {
        if (!finishedTurn)
        {
            switch (gameManager.CurrentGamePhase)
            {
                case GamePhase.WAITINGFORTHROW:
                    nbOfThrows = 0;
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        gameManager.SetCurrentGamePhase(GamePhase.DICETHROWING);
                        ThrowDice();
                    }
                    break;


                case GamePhase.DICETHROWING:
                    if (AllDiceStopped())
                    {
                        if (nbOfThrows < 3)
                        {
                            gameManager.SetCurrentGamePhase(GamePhase.DICESELECTION);
                        }
                        else
                        {
                            gameManager.SetCurrentGamePhase(GamePhase.DICERESOLVING);
                        }
                    }
                    break;


                case GamePhase.DICESELECTION:
                    if (IsTouching())
                    {
                        HandleDiceSelection();
                    }
                    break;
            }
        }
    }

    private bool IsTouching()
    {
        if (Input.touchCount > 0) // V�rifie si l'utilisateur touche l'�cran
        {
            Touch touch = Input.GetTouch(0); // On prend le premier touch
            Ray ray = Camera.main.ScreenPointToRay(touch.position); // Cr�e un ray � partir de la position du touch
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) // Si le raycast touche un objet
            {
                if (hit.collider.CompareTag("Die")) // V�rifie si l'objet touch� est un d�
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void HandleDiceSelection()
    {
        foreach (GameObject die in dice)
        {
            // V�rifie si le d� est touch� et s�lectionne-le
            if (die.activeSelf && IsTouchingDie(die))
            {
                // Garde le d� et emp�che de le relancer
                die.GetComponent<Die>().SetSelected(true);
            }
            else
            {
                // Relance les autres d�s
                die.GetComponent<Die>().SetSelected(false);
            }
        }
    }
    private bool IsTouchingDie(GameObject die)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(die.transform.position);
        Rect dieRect = new Rect(screenPos.x - die.GetComponent<Renderer>().bounds.size.x / 2, screenPos.y - die.GetComponent<Renderer>().bounds.size.y / 2, die.GetComponent<Renderer>().bounds.size.x, die.GetComponent<Renderer>().bounds.size.y);

        return dieRect.Contains(Input.GetTouch(0).position); // V�rifie si le touch est dans le rectangle du d�
    }



    private void ThrowDice()
    {
        nbOfThrows++;

        foreach (GameObject die in dice)
        {
            if (!die.activeSelf)
            {
                die.SetActive(true);
            }

            float randomX = Random.Range(-1.3f, 1.3f);
            float randomZ = Random.Range(-1.3f, 1.3f);
            float offsetX = Random.Range(-0.2f, 0.2f);
            float offsetZ = Random.Range(-0.2f, 0.2f);

            Vector3 randomPos = new Vector3(randomX + offsetX, diceSpawnPos.position.y, randomZ + offsetZ);
            Quaternion randomRot = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
            Vector3 randomAngularVelocity = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f));

            die.transform.position = randomPos;
            die.transform.rotation = randomRot;

            Rigidbody rb = die.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero; // reset old motion
            rb.angularVelocity = randomAngularVelocity;
        }
    }

    private bool AllDiceStopped()
    {
        foreach (GameObject dice in dice)
        {
            if (dice.GetComponent<Die>().GetDieUpwardFace() == Die.DieFace.NotStopped)
            {
                return false;
            }
        }

        return true;
    }
}
