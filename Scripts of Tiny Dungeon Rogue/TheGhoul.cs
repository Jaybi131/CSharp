using UnityEngine;

public class TheGhoul : MonoBehaviour
{
    [SerializeField] private int damage = 5;
    [SerializeField] private float speed = 1.5f;
    private Rigidbody2D body;
    [SerializeField] private EnemyData data;

    private GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        body = GetComponent<Rigidbody2D>(); // <-- действительно берём Rigidbody2D
        SetEnemyValues();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Sworm();
    }

    private void SetEnemyValues()
    {
        GetComponent<Health>().SetHealth(data.hp, data.hp);
        damage = data.damage;
        speed = data.speed;
    }

    private void Sworm()
    {
        //transform.position = Vector2.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
        if (!player) return;
        Vector2 dir = ((Vector2)player.transform.position - body.position).normalized;
        Vector2 next = body.position + dir * speed * Time.fixedDeltaTime;
        body.MovePosition(next); // <-- вместо transform.position = MoveTowards(...)
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            if (collider.GetComponent<Health>() != null)
            {
                collider.GetComponent<Health>().Damage(damage);
                this.GetComponent<Health>().Damage(10000);
            }
        }
    }
}
