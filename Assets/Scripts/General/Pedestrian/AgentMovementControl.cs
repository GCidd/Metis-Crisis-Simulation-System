using System;
using UnityEngine;

public class AgentMovementControl : ThirdPersonCharacter
{
    private Vector3 m_Move;
    private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.
    private Transform mainCameraTransform;

    private void Start()
    {
        m_Animator = GetComponent<Animator>();

        mainCameraTransform = Camera.main.transform;

        if (GetComponent<Pedestrian>().IsPlaced)
            Setup();
    }

    public void Setup()
    {
        Initialize();
    }
    private void Update()
    {

    }

    public void StopMoving()
    {
        m_Animator.SetFloat("Forward", 0f);
    }
    public void MLMove(float horizontal, float vertical)
    {
        // we use world-relative directions in the case of no main camera
        if (mainCameraTransform == null)
            return;
        Vector3 m_CamForward = Vector3.Scale(mainCameraTransform.forward, new Vector3(1, 0, 1)).normalized;
        m_Move = vertical * m_CamForward + horizontal * mainCameraTransform.right;

        m_Rigidbody.useGravity = horizontal != 0 || vertical != 0;

        m_Jump = false;

        // pass all parameters to the character control script
        Move(m_Move, false, m_Jump);
        m_Jump = false;
    }
}