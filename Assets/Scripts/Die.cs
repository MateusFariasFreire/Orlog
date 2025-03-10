using System.Collections.Generic;
using UnityEngine;

public class Die : MonoBehaviour
{
    public enum DieFace { NotStopped, Attack, Shield, DivineEnergy, Adoration, Drain, Affliction }
    private readonly DieFace[] faces = { DieFace.Attack, DieFace.Shield, DieFace.DivineEnergy, DieFace.Adoration, DieFace.Drain, DieFace.Affliction };

    void Start()
    {
    }

    public DieFace GetDieUpwardFace()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb.linearVelocity.magnitude > 0.1f || rb.angularVelocity.magnitude > 0.1f)
            return DieFace.NotStopped;

        return GetFaceUp();
    }

    private bool isSelected = false; // Etat de sélection du dé

    // Méthode pour marquer le dé comme sélectionné ou non
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        // Par exemple, on peut changer la couleur du dé pour indiquer qu'il est sélectionné
        GetComponent<Renderer>().material.color = isSelected ? Color.green : Color.white; // Change la couleur
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    private DieFace GetFaceUp()
    {
        Transform t = transform;

        Vector3 up = Vector3.up;

        float maxDot = -1f;
        int faceIndex = -1;

        Vector3[] directions = {
        t.forward,    // face 0
        t.up,         // face 1
        t.right,      // face 2
        -t.forward,   // face 3
        -t.up,        // face 4
        -t.right      // face 5
    };

        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector3.Dot(directions[i], up);
            if (dot > maxDot)
            {
                maxDot = dot;
                faceIndex = i;
            }
        }

        return faces[faceIndex];
    }
}
