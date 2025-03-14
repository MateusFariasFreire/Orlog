using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;

public class Player : MonoBehaviour
{
    private GameManager _gameManager;

     private List<GameObject> _dice = new List<GameObject>();
    [SerializeField] private GameObject _diePrefab;
    [SerializeField] private Transform _dieThrowPos;

     List<GameObject> _selectedDice = new List<GameObject>();
    [SerializeField] private Transform _selectedDiePos;

    [SerializeField] private int _nbOfThrows = 0;
    [SerializeField] private int _maxNbOfThrows = 3;
    private bool CanThrow => _nbOfThrows < _maxNbOfThrows;
    [SerializeField] private float throwShakeThreshold = 9f;


    [SerializeField] private int _healthPoints = 15;
    [SerializeField] private int _godFavorTokens = 0;
    public int GodFavorTokens { get => _godFavorTokens; }
    [SerializeField] private int _shield = 0;
    [SerializeField] private int _helmet = 0;

    [SerializeField] TMP_Text _healthPointText;
    [SerializeField] TMP_Text _godFavorTokenText;
    [SerializeField] Button _godFavorButton;
    public Button GodFavorButton { get => _godFavorButton; }
    [SerializeField] private Button _skipGodFavorPhaseButton;
    public Button SkipGodFavorPhaseButton { get => _skipGodFavorPhaseButton; }

    [SerializeField] private bool _hasLokiFavor = false;
    public bool HasLokiFavor { get => _hasLokiFavor; set => _hasLokiFavor = value; }
    [SerializeField] private Transform _canvasTransform;

    [SerializeField] private List<GameObject> _diceToResolve = new List<GameObject>();


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
        _godFavorTokenText.SetText(_godFavorTokens.ToString());
        _godFavorButton.interactable = false;
        _skipGodFavorPhaseButton.gameObject.SetActive(false);
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
                    _gameManager.SetCurrentGamePhase(GamePhase.DICESELECTION);
                }
                break;

            case GamePhase.DICESELECTION:

                if (AllDiceSelected() || !CanThrow)
                {
                    OrderDice();
                    ArrangeDice(_dice);
                    _gameManager.SetCurrentGamePhase(GamePhase.GODFAVOR);
                    break;
                }

                if (ShakeDetected())
                {
                    _gameManager.SetCurrentGamePhase(GamePhase.DICETHROWING);
                    ThrowDice();
                    break;
                }

                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    HandleDiceSelection();
                }
                break;

            case GamePhase.DICERESOLVING:
                ResolveDice();
                break;
        }
    }

    private void ResolveDice()
    {

        if (_diceToResolve.Count <= 0)
        {
            _gameManager.EndTurn();
            return;
        }

        GameObject dieToResolve = _diceToResolve[0];

        Die dieComponent = dieToResolve.GetComponent<Die>();

        switch (dieComponent.GetFaceUp())
        {
            case Die.DieFace.Axe1:
            case Die.DieFace.Axe2:
                _gameManager.SetCurrentGamePhase(GamePhase.ANIMATION);
                StartCoroutine(AnimateDiceAttack(dieToResolve, true));
                break;

            case Die.DieFace.Arrow:
                _gameManager.SetCurrentGamePhase(GamePhase.ANIMATION);
                StartCoroutine(AnimateDiceAttack(dieToResolve,false));
                break;

            case Die.DieFace.Helmet:
                _gameManager.SetCurrentGamePhase(GamePhase.ANIMATION);
                StartCoroutine(AnimateGodFavorFromDie(dieToResolve));
                _helmet++;
                break;

            case Die.DieFace.Shield:
                _gameManager.SetCurrentGamePhase(GamePhase.ANIMATION);
                StartCoroutine(AnimateGodFavorFromDie(dieToResolve));
                _shield++;
                break;

            case Die.DieFace.Steal:

                _gameManager.SetCurrentGamePhase(GamePhase.ANIMATION);
                StartCoroutine(AnimateGodFavorSteal(dieToResolve));
                break;
        }

        _diceToResolve.Remove(dieToResolve);
    }

    private IEnumerator AnimateDiceAttack(GameObject dieToAnimate, bool melee)
    {
        GameObject enemy = _gameManager.OpponentPlayer.gameObject;
        Vector3 startPos = dieToAnimate.transform.position;
        Vector3 targetPos = enemy.transform.position;

        float time = 0f;
        float duration = 0.7f;
        while (time < duration)
        {
            dieToAnimate.transform.position = Vector3.Lerp(startPos, targetPos, time/ duration);
            time += Time.deltaTime;
            yield return null;
        }

        dieToAnimate.SetActive(false);
        ArrangeDice(_dice);

        _gameManager.SetCurrentGamePhase(GamePhase.DICERESOLVING);

        if (melee)
        {
            _gameManager.AttackMelee();
        }
        else
        {
            _gameManager.AttackRange();
        }
    }

    private IEnumerator AnimateGodFavorFromDie(GameObject dieToAnimate)
    {
        Transform insideCubeTransform = dieToAnimate.transform.Find("InsideCube");

        if (insideCubeTransform != null)
        {
            Material insideCubeMaterial = insideCubeTransform.GetComponent<Renderer>().materials.ElementAt(0);

            float originalGlowIntensity = insideCubeMaterial.GetFloat("_GlowIntensity");

            float maxGlowIntensity = 120f;

            float time = 0f;
            float duration = 0.5f;
            while (time < duration)
            {
                float currentGlowIntensity = Mathf.Lerp(originalGlowIntensity, maxGlowIntensity, time / duration);
                insideCubeMaterial.SetFloat("_GlowIntensity", currentGlowIntensity);

                time += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            time = 0f;
            while (time < duration)
            {
                float currentGlowIntensity = Mathf.Lerp(maxGlowIntensity, originalGlowIntensity, time/duration);
                insideCubeMaterial.SetFloat("_GlowIntensity", currentGlowIntensity);

                time += Time.deltaTime;
                yield return null;
            }
            insideCubeMaterial.SetFloat("_GlowIntensity", originalGlowIntensity);
        }
        else
        {
            Debug.LogError("InsideCube not found in the dieToAnimate object.");
        }

        AddGodFavorToken();
        _gameManager.SetCurrentGamePhase(GamePhase.DICERESOLVING);
        yield return null;
    }

    private IEnumerator AnimateGodFavorSteal(GameObject dieToAnimate)
    {
        GameObject enemy = _gameManager.OpponentPlayer.gameObject;

        Vector3 startPos = dieToAnimate.transform.position;
        Vector3 targetPos = enemy.transform.position;

        float time = 0f;
        float duration = 0.8f; 
        float arcHeight = 3f; 

        Vector3 oscillationOffset = Vector3.zero;

        while (time < duration)
        {
            float t = time / duration;
            float curveHeight = Mathf.Sin(t * Mathf.PI) * arcHeight;
            Vector3 intermediatePos = Vector3.Lerp(startPos, targetPos, t) + Vector3.up * curveHeight;

            oscillationOffset = new Vector3(Mathf.Sin(t * 4f) * 0.3f, 0, Mathf.Cos(t * 4f) * 0.3f);

            dieToAnimate.transform.position = intermediatePos + oscillationOffset;

            time += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        if (_gameManager.StealGodFavor())
        {
            time = 0f;
            while (time < duration)
            {
                float t = time / duration;
                float curveHeight = Mathf.Sin(t * Mathf.PI) * arcHeight;
                Vector3 intermediatePos = Vector3.Lerp(targetPos, startPos, t) + Vector3.up * curveHeight;

                oscillationOffset = new Vector3(Mathf.Sin(t * 4f) * 0.3f, 0, Mathf.Cos(t * 4f) * 0.3f);

                dieToAnimate.transform.position = intermediatePos + oscillationOffset;

                time += Time.deltaTime;
                yield return null;
            }

            dieToAnimate.transform.position = startPos;
            AddGodFavorToken();
        }
        else
        {
            Vector3 originalScale = dieToAnimate.transform.localScale;
            Vector3 targetScale = originalScale * 0f;
            time = 0f;

            while (time < 0.7f)
            {
                dieToAnimate.transform.localScale = Vector3.Lerp(originalScale, targetScale, time);
                time += Time.deltaTime;
                yield return null;
            }

            dieToAnimate.transform.localScale = originalScale;
        }
        dieToAnimate.SetActive(false);

        _gameManager.SetCurrentGamePhase(GamePhase.DICERESOLVING);
        ArrangeDice(_dice);
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

        List<GameObject> activeDice = new List<GameObject>();
        foreach (GameObject die in dice)
        {
            if (die.activeSelf)
            {
                activeDice.Add(die);
            }
        }

        Vector3 rightDirection = _selectedDiePos.right;

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


    public void OnSkipGodFavorPhase()
    {
        if (_gameManager.CurrentPlayer == this && _gameManager.CurrentGamePhase == GamePhase.GODFAVOR)
        {
            _gameManager.SetCurrentGamePhase(GamePhase.DICERESOLVING);
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

    public bool TakeMeleeDamage()
    {
        if (_helmet > 0)
        {
            _helmet--;
            RemoveHelmetDie();
        }
        else
        {
            Attack(1);
        }

        return (_healthPoints <= 0);

    }

    public bool TakeRangeDamage()
    {
        if (_shield > 0)
        {
            _shield--;
            RemoveShieldDie();
        }
        else
        {
            Attack(1);
        }

        return (_healthPoints <= 0);
    }

    private void RemoveShieldDie()
    {
        foreach(GameObject die in _dice)
        {
            if (!die.activeSelf)
            {
                continue;
            }
            if(die.GetComponent<Die>().GetFaceUp() == Die.DieFace.Shield)
            {
                die.SetActive(false);
                ArrangeDice(_dice);
                return;
            }
        }
    }

    private void RemoveHelmetDie()
    {
        foreach (GameObject die in _dice)
        {
            if (!die.activeSelf)
            {
                continue;
            }
            if (die.GetComponent<Die>().GetFaceUp() == Die.DieFace.Helmet)
            {
                die.SetActive(false);
                ArrangeDice(_dice);
                return;
            }
        }
    }


    public bool StealGodFavor()
    {
        if (_godFavorTokens > 0)
        {
            RemoveGodFavor(1);
            return true;
        }
        return false;
    }

    public void StartNewTurn()
    {
        _selectedDice.Clear();
        _diceToResolve.Clear();
        _helmet = 0;
        _shield = 0;
        _nbOfThrows = 0;
        _hasLokiFavor = false;

        foreach (GameObject die in _dice)
        {
            die.SetActive(false);
            die.GetComponent<Rigidbody>().isKinematic = false;
            die.GetComponent<Die>().IsSelected = false;
        }
    }

    public void AddGodFavorToken()
    {
        _godFavorTokens++;
        _godFavorTokenText.SetText(_godFavorTokens.ToString());
        StartCoroutine(ShowPopUpText("+" + 1 + " FD", _godFavorTokenText.transform.position, Color.green));
    }

    public void RemoveGodFavor(int nbOfGodFavors)
    {
        _godFavorTokens -= nbOfGodFavors;
        if (_godFavorTokens < 0)
        {
            _godFavorTokens = 0;
        }
        _godFavorTokenText.SetText(_godFavorTokens.ToString());
        StartCoroutine(ShowPopUpText("-" + nbOfGodFavors + " FD", _godFavorTokenText.transform.position, Color.red));
    }

    public void Heal(int heal)
    {
        _healthPoints += heal;
        _healthPointText.SetText(_healthPoints.ToString());
        StartCoroutine(ShowPopUpText("+" + heal + " HP", _healthPointText.transform.position, Color.green));
    }

    public bool Attack(int damage)
    {
        if (HasLokiFavor)
        {
            return false;
        }

        _healthPoints -= damage;
        _healthPointText.SetText(_healthPoints.ToString());
        StartCoroutine(ShowPopUpText("-" + damage + " HP", _healthPointText.transform.position, Color.red));

        return (_healthPoints <= 0);
    }

    private IEnumerator ShowPopUpText(string text, Vector3 position, Color textColor)
    {
        GameObject tempTextObj = new GameObject("PopUpText");
        tempTextObj.transform.SetParent(_canvasTransform);

        TextMeshProUGUI textMesh = tempTextObj.AddComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.fontSize = 40;
        textMesh.color = textColor;

        float time = 0f;
        Vector3 startPos = position;
        Vector3 targetPos = startPos + Vector3.up * 30f;
        Color startColor = textMesh.color;

        while (time < 1.5f)
        {
            textMesh.transform.position = Vector3.Lerp(startPos, targetPos, time);
            textMesh.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), time);
            time += Time.deltaTime;
            yield return null;
        }

        Destroy(tempTextObj);
    }

    private void OrderDice()
    {
        _dice.Sort((a, b) =>
        {
            Die.DieFace faceA = a.GetComponent<Die>().GetFaceUp();
            Die.DieFace faceB = b.GetComponent<Die>().GetFaceUp();
            return faceA.CompareTo(faceB);
        });
    }

    public void InitDiceResolving()
    {
        _diceToResolve = new List<GameObject>(_dice);
    }
}
