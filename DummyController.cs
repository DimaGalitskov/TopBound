using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DummyController : MonoBehaviour
{
    NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        HandleSearch();
        HandleMove();
        HandleAttack();
    }

    [Header("Search")]
    [SerializeField] float searchRadius;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] float attackRange;
    bool isInRange;
    Vector3 destination;
    void HandleSearch() {
        Collider[] player = Physics.OverlapSphere(transform.position, searchRadius, playerLayer);
        if (player.Length >0) {
            Vector3 playerPosition = player[0].transform.position;
            Vector3 playerDirection = playerPosition - transform.position;
            RaycastHit hit;
            if (playerDirection.magnitude <= attackRange) {
                isInRange = true;
                destination = Vector3.zero;
                agent.isStopped = true;
            }
            else if (Physics.Raycast(transform.position, playerDirection, out hit, searchRadius, playerLayer)) {
                    destination = hit.transform.position;
                    isInRange = false;
                    agent.isStopped = false;
            }
        }
        else destination = Vector3.zero;
    }
    void HandleMove() {
        if (destination != Vector3.zero && !isAttacking)
        {
            agent.SetDestination(destination);
        }
    }

    [Header("Attack")]
    [SerializeField] private GameObject strikeParticle;
    [SerializeField] private GameObject strikeImpactParticle;
    [SerializeField] private GameObject preStrikeParticle;
    [SerializeField] private float stepTimeout;
    [SerializeField] private float launchTimeout;
    [SerializeField] private float stepForce;
    [SerializeField] private Vector3 strikeDisplacement = new Vector3(2, .5f, 2);
    [SerializeField] private float strikeRange = 5;
    [SerializeField] private float strikeStartDelay = .1f;
    [SerializeField] private float strikeRate = .025f;
    [SerializeField] private float strikeEndDelay = .2f;
    [SerializeField] private float strikeFreezeTime = .05f;
    [SerializeField] private float strikeCooldownTime = 1f;
    private bool isAttacking;
    private void HandleAttack()
    {
        if (!isAttacking && isInRange)
        {
            isAttacking = true;
            StartCoroutine(nameof(StartAttack), transform.forward);
        }
    }
    private IEnumerator StartAttack(Vector3 direction)
    {
        StartCoroutine(nameof(AttackStep), stepTimeout);
        // Spawn pre strike special effect and step
        var instance = Instantiate(preStrikeParticle, transform.position, transform.rotation, transform);
        Destroy(instance, 0.5f);
        yield return new WaitForSeconds(strikeStartDelay);
        StartCoroutine(nameof(AttackStep), launchTimeout);

        // Calculate the strike angle based on the direction
        Quaternion strikeRotation = Quaternion.LookRotation(direction, Vector3.up) * strikeParticle.transform.rotation;
        Vector3 sctikeLocation = new Vector3(transform.position.x + direction.x * strikeDisplacement.x, transform.position.y + strikeDisplacement.y, transform.position.z + direction.z * strikeDisplacement.z);
        instance = Instantiate(strikeParticle, sctikeLocation, strikeRotation);
        Destroy(instance, 0.5f);
        StartCoroutine(nameof(AttackScan), direction);
        yield return new WaitForSeconds(strikeEndDelay);
        isAttacking = false;
    }
    private IEnumerator AttackStep(float timeout)
    {
        float startTime = Time.time;
        while (Time.time - startTime <= timeout)
        {
            yield return null;
        }
        startTime = Time.time;
    }
    private IEnumerator AttackScan(Vector3 direction)
    {
        yield return null;
    }
}
