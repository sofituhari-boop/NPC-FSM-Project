using UnityEngine;

public class NormalEnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Detect, Chase, Attack, Search, Death }

    [Header("References")] public Transform player; public Transform[] patrolPoints; public LayerMask obstacleMask;
    [Header("Perception")] [Min(0.1f)] public float detectionRange = 6f; [Range(0f,360f)] public float fieldOfView = 180f;
    [Header("Movement")] [Min(0f)] public float moveSpeed = 2f;
    [Header("Initial Idle")] [Min(0f)] public float initialIdleDuration = 5f;
    [Header("Combat")] public int maxHealth = 3; public int attackDamage = 1; [Min(0.1f)] public float attackRange = 1.2f; [Min(0.05f)] public float attackCooldown = 1f;
    [Header("Search")] [Min(0.1f)] public float searchDuration = 3f;
    [Header("Feedback")] public Color attackFlashColor = Color.red; public float attackFlashDuration = 0.12f;

    public State currentState = State.Idle;
    int currentHealth, patrolIndex; float attackTimer, searchTimer, idleTimer, flashTimer; Vector3 lastKnownPosition; SpriteRenderer sprite; Color baseColor;

    void Start() { currentHealth = maxHealth; FindPlayer(); idleTimer = initialIdleDuration; sprite = GetComponent<SpriteRenderer>(); if (sprite) baseColor = sprite.color; ChangeState(State.Idle); }
    void Update()
    {
        if (currentState == State.Death) return;
        if (currentHealth <= 0) { ChangeState(State.Death); return; }
        attackTimer -= Time.deltaTime; flashTimer -= Time.deltaTime; if (sprite && flashTimer <= 0) sprite.color = baseColor;
        switch (currentState) { case State.Idle: UpdateIdle(); break; case State.Patrol: UpdatePatrol(); break; case State.Detect: UpdateDetect(); break; case State.Chase: UpdateChase(); break; case State.Attack: UpdateAttack(); break; case State.Search: UpdateSearch(); break; }
    }
    void FindPlayer() { if (!player) { GameObject p = GameObject.FindWithTag("Player"); if (p) player = p.transform; } }
    void UpdateIdle() { if (CanSeePlayer()) { lastKnownPosition = player.position; ChangeState(State.Detect); return; } idleTimer -= Time.deltaTime; if (idleTimer <= 0) ChangeState(State.Patrol); }
    void UpdatePatrol() { if (CanSeePlayer()) { lastKnownPosition = player.position; ChangeState(State.Detect); return; } if (patrolPoints == null || patrolPoints.Length == 0 || !patrolPoints[patrolIndex]) return; MoveTo(patrolPoints[patrolIndex].position); if (Vector2.Distance(transform.position, patrolPoints[patrolIndex].position) <= .15f) patrolIndex = (patrolIndex + 1) % patrolPoints.Length; }
    void UpdateDetect() { if (CanSeePlayer()) { lastKnownPosition = player.position; ChangeState(State.Chase); } else ChangeState(State.Patrol); }
    void UpdateChase()
    {
        if (!player) { ChangeState(State.Search); return; }
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > detectionRange) { ChangeState(State.Patrol); return; }
        if (!CanSeePlayer()) { ChangeState(State.Search); return; }
        lastKnownPosition = player.position;
        if (distance <= attackRange) ChangeState(State.Attack); else MoveTo(player.position);
    }
    void UpdateAttack() { if (!player) { ChangeState(State.Search); return; } float d = Vector2.Distance(transform.position, player.position); if (d > detectionRange) { ChangeState(State.Patrol); return; } if (d > attackRange) { ChangeState(State.Chase); return; } if (attackTimer <= 0) { attackTimer = attackCooldown; PlayerController2D pc = player.GetComponent<PlayerController2D>(); if (pc) pc.TakeDamage(attackDamage); FlashAttack(); } }
    void UpdateSearch() { if (CanSeePlayer()) { lastKnownPosition = player.position; ChangeState(State.Chase); return; } MoveTo(lastKnownPosition); searchTimer -= Time.deltaTime; if (searchTimer <= 0) ChangeState(State.Patrol); }
    bool CanSeePlayer()
    {
        if (!player) return false; Vector2 dir = player.position - transform.position; float distance = dir.magnitude; if (distance > detectionRange) return false; if (Vector2.Angle(transform.right, dir) > fieldOfView * .5f) return false; return !Physics2D.Raycast(transform.position, dir.normalized, distance, obstacleMask);
    }
    void MoveTo(Vector3 target) { Vector2 dir = (target - transform.position); if (dir.sqrMagnitude < .0001f) return; dir.Normalize(); transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime); transform.right = dir; }
    void FlashAttack() { if (sprite) { sprite.color = attackFlashColor; flashTimer = attackFlashDuration; } Debug.Log("NormalEnemy: ATTACK"); }
    public void TakeDamage(int damage) { if (currentState == State.Death) return; currentHealth -= Mathf.Max(0, damage); if (currentHealth <= 0) ChangeState(State.Death); }
    void ChangeState(State next) { if (currentState == next) return; currentState = next; if (next == State.Search) searchTimer = searchDuration; if (next == State.Idle) idleTimer = initialIdleDuration; Debug.Log("NormalEnemy -> " + next); if (next == State.Death) { Debug.Log("NormalEnemy: DEATH"); enabled = false; } }
    void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange); Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange); }
}
