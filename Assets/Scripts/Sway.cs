using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Sway : MonoBehaviour
{
    public float intensity;
    public float smooth;

    public bool isMine;

    private Quaternion originRotation, originFXRotation;
    private Quaternion childRotation;

    public Transform FX;

    private void UpdateSway()
    {
        float t_mouseX = Input.GetAxis("Mouse X");
        float t_mouseY = Input.GetAxis("Mouse Y");

        if (!isMine)
        {
            t_mouseX = 0;
            t_mouseY = 0;
        }

        Quaternion t_adjX = Quaternion.AngleAxis(-intensity * t_mouseX , Vector3.up);
        Quaternion t_adjZ = Quaternion.AngleAxis(-intensity * t_mouseX , Vector3.forward);
        Quaternion t_adjY = Quaternion.AngleAxis(intensity * t_mouseY , Vector3.right);

        Quaternion t_adjFxX = Quaternion.AngleAxis(intensity*2 * t_mouseX, Vector3.up);
        Quaternion t_adjFxY = Quaternion.AngleAxis(-intensity*2 * t_mouseY, Vector3.right);

        Quaternion targetRotation = originRotation * t_adjX * t_adjY;

        FX.localRotation = Quaternion.Lerp(FX.localRotation, originFXRotation * t_adjFxX * t_adjFxY, Time.deltaTime * smooth);

        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smooth * 2);

    }

    private void Awake()
    {
    }

    private void Start()
    {
        originRotation = transform.localRotation;
        originFXRotation = FX.localRotation;
    }
    private void Update()
    {
        UpdateSway();
    }
}
