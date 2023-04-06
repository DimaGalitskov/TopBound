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
    }

    [Header("Search")]
    [SerializeField] float searchRadius;
    [SerializeField] LayerMask playerLayer;
    Vector3 destination;
    void HandleSearch() {
        Collider[] player = Physics.OverlapSphere(transform.position, searchRadius, playerLayer);
        if (player.Length > 0) {
            Vector3 playerPosition = player[0].transform.position;
            Vector3 playerDirection = playerPosition - transform.position;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, playerDirection, out hit, searchRadius, playerLayer))
            {
                destination = hit.transform.position;
            }
            else destination = Vector3.zero;
        }
    }

    void HandleMove() {
        if (destination != Vector3.zero) {
            agent.SetDestination(destination);
            Debug.Log(destination);
        }
    }
}
