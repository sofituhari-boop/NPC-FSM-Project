using UnityEngine;

public class TeleportEnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Detect, Chase, Attack, Search, Teleport, Death }
    [Header("References")] public Transform player; public Transform[] patrolPoints; public LayerMask obstacleMask;
    [Header("360° Scan")] [Min(0.1f)] public float scanRadius = 6f; [Min(0.05f)] public float scanInterval = .5f;
    [Header("Movement")] [Min(0f)] public float moveSpeed = 2f;
    [Header("Initial Idle")] [Min(0f)] public float initialIdleDuration = 5f;
    [Header("Combat")] public int maxHealth = 3; public int attackDamage = 1; [Min(.1f)] public float attackRange = 1.2f; [Min(.05f)] public float attackCooldown = 1f;
    [Header("Search")] [Min(.1f)] public float searchDuration = 3f;
    [Header("Teleport")] [Min(.1f)] public float teleportCooldown = 5f; [Min(.1f)] public float maxTeleportDistance = 4f; [Min(.1f)] public float teleportTriggerDistance = 2.5f; [Min(.05f)] public float destinationClearanceRadius = .35f;
    [Header("Feedback")] public Color attackFlashColor = Color.magenta; public float attackFlashDuration = .12f;

    public State currentState = State.Idle;
    int currentHealth, patrolIndex; float scanTimer, teleportTimer, attackTimer, searchTimer, idleTimer, flashTimer; Vector3 lastKnownPosition; SpriteRenderer sprite; Color baseColor;

    void Start() { currentHealth = maxHealth; FindPlayer(); idleTimer = initialIdleDuration; sprite = GetComponent<SpriteRenderer>(); if (sprite) baseColor = sprite.color; ChangeState(State.Idle); }
    void Update()
    {
        if (currentState == State.Death) return; if (currentHealth <= 0) { ChangeState(State.Death); return; }
        scanTimer -= Time.deltaTime; teleportTimer -= Time.deltaTime; attackTimer -= Time.deltaTime; flashTimer -= Time.deltaTime; if (sprite && flashTimer <= 0) sprite.color = baseColor;
        if (scanTimer <= 0) { scanTimer = scanInterval; Perform360Scan(); }
        switch (currentState) { case State.Idle: UpdateIdle(); break; case State.Patrol: UpdatePatrol(); break; case State.Detect: UpdateDetect(); break; case State.Chase: UpdateChase(); break; case State.Attack: UpdateAttack(); break; case State.Search: UpdateSearch(); break; case State.Teleport: UpdateTeleport(); break; }
    }
    void FindPlayer() { if (!player) { GameObject p = GameObject.FindWithTag("Player"); if (p) player = p.transform; } }
    void Perform360Scan() { if (!player || currentState == State.Death || currentState == State.Chase || currentState == State.Attack || currentState == State.Teleport) return; float d = Vector2.Distance(transform.position, player.position); if (d <= scanRadius && !Physics2D.Linecast(transform.position, player.position, obstacleMask)) { lastKnownPosition = player.position; ChangeState(State.Detect); } }
    void UpdateIdle() { idleTimer -= Time.deltaTime; if (currentState == State.Detect) return; if (idleTimer <= 0) ChangeState(State.Patrol); }
    void UpdatePatrol() { if (patrolPoints == null || patrolPoints.Length == 0 || !patrolPoints[patrolIndex]) return; MoveTo(patrolPoints[patrolIndex].position); if (Vector2.Distance(transform.position, patrolPoints[patrolIndex].position) <= .15f) patrolIndex = (patrolIndex + 1) % patrolPoints.Length; }
    void UpdateDetect() { if (player) { lastKnownPosition = player.position; ChangeState(State.Chase); } else ChangeState(State.Patrol); }
    void UpdateChase()
    {
        if (!player) { ChangeState(State.Search); return; } float d = Vector2.Distance(transform.position, player.position); lastKnownPosition = player.position;
        if (d > scanRadius * 6f) { ChangeState(State.Patrol); return; }
        if (d <= attackRange) { ChangeState(State.Attack); return; }
        if (teleportTimer <= 0 && d >= teleportTriggerDistance) { ChangeState(State.Teleport); return; }
        MoveTo(player.position);
    }
    void UpdateAttack() { if (!player) { ChangeState(State.Search); return; } float d = Vector2.Distance(transform.position, player.position); if (d > attackRange) { ChangeState(State.Chase); return; } if (attackTimer <= 0) { attackTimer = attackCooldown; PlayerController2D pc = player.GetComponent<PlayerController2D>(); if (pc) pc.TakeDamage(attackDamage); FlashAttack(); } }
    void UpdateSearch() { if (!player) { searchTimer -= Time.deltaTime; if (searchTimer <= 0) ChangeState(State.Patrol); return; } if (Vector2.Distance(transform.position, player.position) <= scanRadius && !Physics2D.Linecast(transform.position, player.position, obstacleMask)) { ChangeState(State.Chase); return; } MoveTo(lastKnownPosition); searchTimer -= Time.deltaTime; if (searchTimer <= 0) ChangeState(State.Patrol); }
    void UpdateTeleport()
    {
        if (!player) { ChangeState(State.Search); return; } Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized; if (dir.sqrMagnitude < .01f) { ChangeState(State.Chase); return; }
        Vector2 destination = (Vector2)transform.position + dir * Mathf.Min(maxTeleportDistance, Vector2.Distance(transform.position, player.position)); bool blocked = Physics2D.Linecast(transform.position, destination, obstacleMask) || Physics2D.OverlapCircle(destination, destinationClearanceRadius, obstacleMask);
        if (!blocked) transform.position = destination; else Debug.Log("TeleportEnemy: teleport blocked"); teleportTimer = teleportCooldown; ChangeState(State.Chase);
    }
    void MoveTo(Vector3 target) { Vector2 dir = target - transform.position; if (dir.sqrMagnitude < .0001f) return; dir.Normalize(); transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime); transform.right = dir; }
    void FlashAttack() { if (sprite) { sprite.color = attackFlashColor; flashTimer = attackFlashDuration; } Debug.Log("TeleportEnemy: ATTACK"); }
    public void TakeDamage(int damage) { if (currentState == State.Death) return; currentHealth -= Mathf.Max(0, damage); if (currentHealth <= 0) ChangeState(State.Death); }
    void ChangeState(State next) { if (currentState == next) return; currentState = next; if (next == State.Search) searchTimer = searchDuration; if (next == State.Idle) idleTimer = initialIdleDuration; Debug.Log("TeleportEnemy -> " + next); if (next == State.Death) { Debug.Log("TeleportEnemy: DEATH"); enabled = false; } }
    void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, scanRadius); Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange); Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, maxTeleportDistance); }
}
