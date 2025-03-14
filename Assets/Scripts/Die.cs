using System.Collections.Generic;
using UnityEngine;

public class Die : MonoBehaviour
{
    public enum DieFace { NotStopped = 0, Axe1, Axe2, Arrow, Shield, Helmet,  Steal }

    private Vector3 _oldPos;
    public Vector3 OldPos { get => _oldPos; set => _oldPos = value; }

    private Quaternion _oldRot;
    public Quaternion OldRot { get => _oldRot; set => _oldRot = value; }

    [SerializeField] private bool _isSelected = false;
    public bool IsSelected { get => _isSelected; set => _isSelected = value; }

    private Dictionary<DieFace, Quaternion> faceUpDictionnary = new Dictionary<DieFace, Quaternion>()
    {
        { DieFace.Steal,  Quaternion.Euler(0, 0, 0) },        // top
        { DieFace.Helmet, Quaternion.Euler(-90, 0, 0) },      // bottom
        { DieFace.Axe1,   Quaternion.Euler(0, 0, -90) },      // right
        { DieFace.Axe2,   Quaternion.Euler(0, 0, 90) },       // left
        { DieFace.Arrow,  Quaternion.Euler(180, 0, 0) },      // back
        { DieFace.Shield, Quaternion.Euler(90, 0, 0) }        // front
    };

    void Start()
    {
        _oldPos = transform.position;
        _oldRot = transform.rotation;
    }

    public DieFace GetFaceUp()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb.linearVelocity.magnitude > 0.1f || rb.angularVelocity.magnitude > 0.1f)
            return DieFace.NotStopped;

        DieFace bestFace = DieFace.NotStopped;
        float maxDot = -1f;

        foreach (var pair in faceUpDictionnary)
        {
            Vector3 faceDirection = transform.rotation * (Quaternion.Inverse(pair.Value) * Vector3.up);
            float dot = Vector3.Dot(Vector3.up, faceDirection);

            if (dot > maxDot)
            {
                maxDot = dot;
                bestFace = pair.Key;
            }
        }

        return bestFace;
    }

    public void SetFaceUp(DieFace face)
    {
        if (faceUpDictionnary.TryGetValue(face, out Quaternion rotation))
        {
            transform.rotation = rotation;
        }
        else
        {
            Debug.LogWarning("Face non trouvée dans faceRotations.");
        }
    }
}
