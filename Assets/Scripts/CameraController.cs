using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraController : MonoBehaviour
{
    [Range(.1f, 10f)]
    public float speed;
    [Range(.1f, 5f)]
    public float rotation;
    public Texture2D cursorTexture;
    private AgentManager manager;

    private Vector2 currentRotation;

    void Start()
    {
        manager = FindObjectOfType<AgentManager>();
        // #if UNITY_WEBGL
        // Cursor.SetCursor(cursorTexture,
        //     new Vector2 (cursorTexture.width / 2, cursorTexture.height / 2),
        //     CursorMode.ForceSoftware
        //  );
        // #else
        // Cursor.SetCursor(cursorTexture,
        //     new Vector2 (cursorTexture.width / 2, cursorTexture.height / 2),
        //     CursorMode.Auto
        //  );
        // #endif
        Cursor.SetCursor(null, new Vector2(0, 0), CursorMode.Auto);
    }

    // Update is called once per frame
    void Update()
    {
        var verticalSpeed = speed;
        float angle = Mathf.PI * currentRotation.x / 180.0f;
        // print(currentRotation.x.ToString() + " "+currentRotation.y.ToString());
        float forward_component = Input.GetAxis("Horizontal") * Mathf.Cos(-angle)
                                + Input.GetAxis("Vertical")   * Mathf.Sin(angle);
        float horizontal_component =  Input.GetAxis("Horizontal") * Mathf.Sin(-angle)
                                    + Input.GetAxis("Vertical")   * Mathf.Cos(angle);
        var moveVector =
            new Vector3(forward_component, 0, horizontal_component) * speed
            + Vector3.up * (Input.GetKey("space") ? verticalSpeed : 0)
            - Vector3.up * (Input.GetKey("left shift") ? verticalSpeed : 0);
        transform.position += moveVector * Time.deltaTime;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
        currentRotation.x += Input.GetAxis("Mouse X") * rotation;
        currentRotation.y -= Input.GetAxis("Mouse Y") * rotation;
        transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray.origin, ray.direction, out hit))
            {
                if (hit.collider.gameObject.name.Equals("Plane"))
                {
                    manager.SetAgentDestinations(hit.point);
                    // target.transform.position = hit.point;
                }
            }
        }
        // if (Input.GetKeyDown("escape"))
        // {
        //     Cursor.lockState = CursorLockMode.None;
        // }
    }
}
