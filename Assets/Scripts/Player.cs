using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private float roationSpeed = 360.0f;
    private float movementSpeed = 2.0f;
    // Start is called before the first frame update
    void Start()
    {

    }

    private void FixedUpdate()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");
        transform.Rotate(Vector3.up, horizontal * roationSpeed * Time.deltaTime);
        transform.Translate(vertical * Vector3.forward * movementSpeed * Time.deltaTime);
        //transform.Translate(new Vector3(horizontal, 0, vertical) * (speed * Time.deltaTime));
    }


}
