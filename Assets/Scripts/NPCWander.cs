using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC brain with four states and five personalities.
///
/// States
/// ──────
///   Wander  – roams randomly (default)
///   Idle    – stands still for a moment then resumes wandering
///   Flee    – sprints away from the car when it gets too close
///   Angry   – chases the car after being knocked down and recovering
///
/// Personalities (assigned randomly in OnEnable)
/// ─────────────────────────────────────────────
///   Normal     – balanced flee/angry response
///   Runner     – very fast, large territory, flees more readily
///   Aggressive – wide anger zone, chases even when not personally hit
///   Coward     – flees at long range, never gets angry
///   Brawler    – slow wanderer but enters Angry immediately when nearby hits happen
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NPCWander : MonoBehaviour
{
    public enum NPCState { Wander, Idle, Flee, Angry }
    public enum Personality { Normal, Runner, Aggressive, Coward, Brawler }

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 25f;
    [SerializeField] private Vector2 idlePause  = new Vector2(0f, 4f);

    [Header("Flee")]
    [SerializeField] private float fleeRadius   = 14f;
    [SerializeField] private float fleeSpeed    = 5.5f;

    [Header("Angry")]
    [SerializeField] private float angrySpeed   = 5f;
    [SerializeField] private float angryDuration = 18f;
    [Tooltip("Radius within which this NPC gets angry watching a NEARBY NPC get hit (Brawler/Aggressive only).")]
    [SerializeField] private float witnessAngryRadius = 12f;

    // ── runtime state ──────────────────────────────────────────────────────
    public NPCState State  { get; private set; }
    public Personality PersonalityType { get; private set; }

    private NavMeshAgent agent;
    private RagdollController ragdoll;
    private Animator animator;

    private float waitTimer;
    private float angryTimer;
    private float stateCheckTimer;
    private string lastAnim;
    private bool wasHitByCar;   // set by RagdollController before ragdoll

    private float baseSpeed;
    private float baseWanderRadius;

    // ── static helpers ─────────────────────────────────────────────────────
    // All living NPCs register here so Brawler/Aggressive can detect nearby hits
    private static readonly System.Collections.Generic.List<NPCWander> allNPCs = new();

    // ── lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        agent   = GetComponent<NavMeshAgent>();
        ragdoll = GetComponent<RagdollController>();
        animator = GetComponentInChildren<Animator>();
        baseSpeed = agent.speed;
        baseWanderRadius = wanderRadius;
    }

    void OnEnable()
    {
        allNPCs.Add(this);
        waitTimer  = 0f;
        angryTimer = 0f;
        wasHitByCar = false;
        lastAnim = "";
        AssignPersonality();
        SetState(NPCState.Wander);
    }

    void OnDisable() => allNPCs.Remove(this);

    void Update()
    {
        if (ragdoll != null && ragdoll.IsRagdoll)
        {
            if (agent.enabled) agent.enabled = false;
            return;
        }

        if (!agent.enabled || !agent.isOnNavMesh) return;

        // Throttle car-distance checks (every ~0.2 s)
        stateCheckTimer -= Time.deltaTime;
        bool doCheck = stateCheckTimer <= 0f;
        if (doCheck) stateCheckTimer = 0.2f;

        if (doCheck) TickStateLogic();

        switch (State)
        {
            case NPCState.Wander: UpdateWander(); break;
            case NPCState.Idle:   UpdateIdle();   break;
            case NPCState.Flee:   UpdateFlee();   break;
            case NPCState.Angry:  UpdateAngry();  break;
        }

        UpdateAnimation();
    }

    // ── called by RagdollController ─────────────────────────────────────────

    public void OnHitByCar()
    {
        wasHitByCar = true;

        // Notify nearby Brawler/Aggressive NPCs who witnessed the hit
        foreach (var other in allNPCs)
        {
            if (other == this || other.State == NPCState.Angry) continue;
            if (other.PersonalityType != Personality.Aggressive &&
                other.PersonalityType != Personality.Brawler) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist <= other.witnessAngryRadius)
                other.TriggerAnger();
        }
    }

    public void OnStoodUp()
    {
        if (wasHitByCar && PersonalityType != Personality.Coward)
            TriggerAnger();
        wasHitByCar = false;
    }

    // ── state machine ────────────────────────────────────────────────────────

    private void TickStateLogic()
    {
        Transform car = CarControllerFIXED.Instance?.transform;
        if (car == null) return;

        float dist = Vector3.Distance(transform.position, car.position);

        if (State == NPCState.Angry)
        {
            angryTimer -= 0.2f; // approximate (checked every 0.2 s)
            if (angryTimer <= 0f) SetState(NPCState.Wander);
            return; // don't interrupt anger with flee
        }

        bool carNear = dist < fleeRadius;
        if (carNear && State != NPCState.Flee)  SetState(NPCState.Flee);
        else if (!carNear && State == NPCState.Flee) SetState(NPCState.Wander);

        // Aggressive types anger when car enters witnessAngryRadius
        if (PersonalityType == Personality.Aggressive && dist < witnessAngryRadius
            && State != NPCState.Angry)
            TriggerAnger();
    }

    private void UpdateWander()
    {
        if (agent.pathPending) return;
        if (agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                agent.SetDestination(RandomNavPoint(wanderRadius));
                waitTimer = Random.Range(idlePause.x, idlePause.y);
                if (waitTimer > 1f) SetState(NPCState.Idle);
            }
        }
    }

    private void UpdateIdle()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0f)
        {
            agent.SetDestination(RandomNavPoint(wanderRadius));
            SetState(NPCState.Wander);
        }
    }

    private void UpdateFlee()
    {
        Transform car = CarControllerFIXED.Instance?.transform;
        if (car == null) { SetState(NPCState.Wander); return; }

        // Every 0.4 s pick a new flee destination away from the car
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            Vector3 away = transform.position + (transform.position - car.position).normalized
                           * fleeRadius * 0.75f;
            if (NavMesh.SamplePosition(away, out NavMeshHit hit, fleeRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    private void UpdateAngry()
    {
        Transform car = CarControllerFIXED.Instance?.transform;
        if (car == null) { SetState(NPCState.Wander); return; }

        // Chase the car
        agent.SetDestination(car.position);
    }

    private void UpdateAnimation()
    {
        string anim = agent.velocity.magnitude > 0.3f ? "walk" : "idle";
        if (anim != lastAnim)
        {
            lastAnim = anim;
            animator?.SetTrigger(anim);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    public void TriggerAnger()
    {
        angryTimer = angryDuration;
        SetState(NPCState.Angry);
    }

    private void SetState(NPCState next)
    {
        State = next;
        switch (next)
        {
            case NPCState.Flee:
                agent.speed = fleeSpeed; break;
            case NPCState.Angry:
                agent.speed = angrySpeed; break;
            default:
                agent.speed = baseSpeed; break;
        }
    }

    private void AssignPersonality()
    {
        // Weighted random: Normal 35%, Runner 20%, Aggressive 15%, Coward 20%, Brawler 10%
        float r = Random.value;
        if      (r < 0.35f) PersonalityType = Personality.Normal;
        else if (r < 0.55f) PersonalityType = Personality.Runner;
        else if (r < 0.70f) PersonalityType = Personality.Aggressive;
        else if (r < 0.90f) PersonalityType = Personality.Coward;
        else                 PersonalityType = Personality.Brawler;

        ApplyPersonalityStats();
    }

    private void ApplyPersonalityStats()
    {
        switch (PersonalityType)
        {
            case Personality.Normal:
                agent.speed   = baseSpeed;
                wanderRadius  = baseWanderRadius;
                fleeRadius    = 14f;
                angryDuration = 18f;
                break;
            case Personality.Runner:
                agent.speed   = baseSpeed * 1.6f;
                wanderRadius  = baseWanderRadius * 1.8f;
                fleeRadius    = 20f;
                angryDuration = 10f;
                fleeSpeed     = baseSpeed * 2.2f;
                break;
            case Personality.Aggressive:
                agent.speed       = baseSpeed * 0.9f;
                wanderRadius      = baseWanderRadius;
                fleeRadius        = 5f;   // barely flees
                angryDuration     = 30f;
                angrySpeed        = baseSpeed * 1.5f;
                witnessAngryRadius = 18f;
                break;
            case Personality.Coward:
                agent.speed   = baseSpeed * 1.1f;
                wanderRadius  = baseWanderRadius * 0.6f;
                fleeRadius    = 25f;      // flees from very far
                fleeSpeed     = baseSpeed * 2f;
                angryDuration = 0f;       // never stays angry
                break;
            case Personality.Brawler:
                agent.speed       = baseSpeed * 0.8f;
                wanderRadius      = baseWanderRadius * 0.7f;
                fleeRadius        = 6f;
                angryDuration     = 40f;
                angrySpeed        = baseSpeed * 1.3f;
                witnessAngryRadius = 22f;
                break;
        }
    }

    private Vector3 RandomNavPoint(float radius)
    {
        Vector3 candidate = transform.position + Random.insideUnitSphere * radius;
        candidate.y = transform.position.y;
        if (NavMesh.SamplePosition(candidate, out var hit, radius, NavMesh.AllAreas))
            return hit.position;
        return transform.position;
    }
}
