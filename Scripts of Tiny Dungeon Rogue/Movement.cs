using UnityEngine;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Movement : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    private Rigidbody2D body;
    private Vector2 axisMovement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        body = GetComponent<Rigidbody2D>(); 
    }

    // Update is called once per frame
    void Update()
    {
        axisMovement.x = Input.GetAxisRaw("Horizontal");
        axisMovement.y = Input.GetAxisRaw("Vertical");
    }
    void FixedUpdate()
    {
        Move();
    }
    private void Move()
    {
        body.linearVelocity = axisMovement.normalized * speed;
        CheckForSlipping();
    }
    private void CheckForSlipping()
    {
        bool movingLeft = axisMovement.x < 0;
        bool movingRight = axisMovement.x > 0;
        if(movingLeft)
        {
            transform.localScale = new Vector3(-1f, transform.localScale.y);
        }

        if (movingRight)
        {
            transform.localScale = new Vector3(1f, transform.localScale.y);
        }
    }
}

