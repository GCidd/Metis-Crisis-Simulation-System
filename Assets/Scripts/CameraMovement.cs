using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraMovement : MonoBehaviour
{
    [SerializeField]
    private Transform cameraTransform;
    [SerializeField]
    private float movementSpeed = 1;
    [SerializeField]
    private float movementTime = 5;
    [SerializeField]
    private float rotationAmount = 1;
    [SerializeField]
    private Vector3 zoomAmount = new Vector3(0, -5, 5);
    [SerializeField]
    private bool limitMovements = true;

    private Vector3 newPosition;
    private Quaternion newRotation;
    private Vector3 newZoom;

    private Quaternion defaultRotation;

    private Vector3 dragStartPosition;
    private Vector3 dragCurrentPosition;
    private Vector3 rotationStartPosition;
    private Vector3 rotationCurrentPosition;

    private float lastRightClicked;
    private float lastRotationHit;

    // Start is called before the first frame update
    void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition;

        defaultRotation = transform.rotation;

        lastRightClicked = Time.time;
        lastRotationHit = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftAlt) || PopupWindowManager.WindowVisible)
        {
            return;
        }

        Vector3 view = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        if (view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1)
        {
            return;
        }
        HandleMouseInput();
        HandleKeyboardInput();
    }

    void HandleMouseInput()
    {
        HandleMouseDrag();
        HandleMouseZoom();
        HandleMouseRotation();
    }

    void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(2))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;

            if (plane.Raycast(ray, out entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
        }
        if (Input.GetMouseButton(2))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;

            if (plane.Raycast(ray, out entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);

                newPosition = transform.position + dragStartPosition - dragCurrentPosition;

                newPosition = ClampPosition(newPosition);
            }
        }
    }
    void HandleMouseZoom()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            newZoom += Input.mouseScrollDelta.y * zoomAmount;
        }
    }
    void HandleMouseRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            rotationStartPosition = Input.mousePosition;
            if (Time.time - lastRightClicked <= 0.1f)
            {
                newRotation = defaultRotation;
                lastRightClicked = Time.time;
                return;
            }
            lastRightClicked = Time.time;
        }
        if (Input.GetMouseButton(1))
        {
            rotationCurrentPosition = Input.mousePosition;

            Vector3 difference = rotationStartPosition - rotationCurrentPosition;

            if (difference.magnitude > 0)
            {
                PointerMode.Instance.RightClickDownHandled = true;
            }

            rotationStartPosition = rotationCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.down * difference.x / movementSpeed);
        }
        if (Input.GetMouseButtonUp(1) && PointerMode.Instance.RightClickDownHandled)
        {
            PointerMode.Instance.RightClickDownHandled = false;
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
    }
    void HandleKeyboardInput()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }
    void HandleMovement()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            // forward
            newPosition += (transform.forward * movementSpeed);
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            // back
            newPosition += (-transform.forward * movementSpeed);
        }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            // left
            newPosition += (-transform.right * movementSpeed);
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            // right
            newPosition += (transform.right * movementSpeed);
        }

        newPosition = ClampPosition(newPosition);

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
    }

    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        }
        if (Input.GetKey(KeyCode.E))
        {
            newRotation *= Quaternion.Euler(Vector3.down * rotationAmount);
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
    }

    void HandleZoom()
    {
        if (Input.GetKey(KeyCode.R))
        {
            newZoom += zoomAmount;
        }
        if (Input.GetKey(KeyCode.F))
        {
            newZoom -= zoomAmount;
        }

        newZoom = ClampPosition(newZoom);

        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }
    Vector3 ClampPosition(Vector3 pos)
    {
        if (limitMovements)
            return new Vector3(
                Mathf.Clamp(pos.x, -150f, 150f),
                Mathf.Clamp(pos.y, 0f, 100f),
                Mathf.Clamp(pos.z, -150f, 120f));
        else
            return pos;
    }
}
