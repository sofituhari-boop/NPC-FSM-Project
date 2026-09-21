using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public int maxHealth = 10;
    public int attackDamage = 1;
    public float attackRange = 1.6f;

    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        Move();

        if (Input.GetKeyDown(KeyCode.Space))
            Attack();
    }

    private void Move()
    {
        Vector2 input = new(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        input = Vector2.ClampMagnitude(input, 1f);
        transform.position += (Vector3)(input * moveSpeed * Time.deltaTime);

        if (input.sqrMagnitude > 0.01f)
            transform.right = input;
    }

    private void Attack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<NormalEnemyAI>(out NormalEnemyAI normalEnemy))
                normalEnemy.TakeDamage(attackDamage);

            if (hit.TryGetComponent<TeleportEnemyAI>(out TeleportEnemyAI teleportEnemy))
                teleportEnemy.TakeDamage(attackDamage);
        }

        Debug.Log("Player attack");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
            transform.position = new Vector3(0f, -4f);
            Debug.Log("Player respawned");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
