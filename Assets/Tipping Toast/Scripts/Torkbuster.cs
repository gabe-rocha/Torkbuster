using System.Transactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torkbuster : MonoBehaviour
{

    public int rpm = 100;

    private void Update()
    {
        Vector3 rotation = new Vector3(-rpm * 6f * Time.deltaTime, 0f, 0f);
        transform.Rotate(rotation);
    }
}