using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    public float radius;
    public float mass;
    public float perceptionRadius;

    private List<Vector3> path;
    private NavMeshAgent nma;
    private Rigidbody rb;

    private HashSet<GameObject> perceivedNeighbors = new HashSet<GameObject>();
    private HashSet<GameObject> perceivedWalls = new HashSet<GameObject>();

    void Start()
    {
        path = new List<Vector3>();
        nma = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        gameObject.transform.localScale = new Vector3(2 * radius, 1, 2 * radius);
        nma.radius = radius;
        rb.mass = mass;
        GetComponent<SphereCollider>().radius = perceptionRadius / 2;
    }

    private void Update()
    {
        if (path.Count > 1 && Vector3.Distance(transform.position, path[0]) < 1.1f)
        {
            path.RemoveAt(0);
        } else if (path.Count == 1 && Vector3.Distance(transform.position, path[0]) < 2f)
        {
            path.RemoveAt(0);

            if (path.Count == 0)
            {
                gameObject.SetActive(false);
                AgentManager.RemoveAgent(gameObject);
            }
        }

        #region Visualization

        if (false)
        {
            if (path.Count > 0)
            {
                Debug.DrawLine(transform.position, path[0], Color.green);
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i], path[i + 1], Color.yellow);
            }
        }

        if (false)
        {
            foreach (var neighbor in perceivedNeighbors)
            {
                Debug.DrawLine(transform.position, neighbor.transform.position, Color.yellow);
            }
        }

        #endregion
    }

    #region Public Functions

    public void ComputePath(Vector3 destination)
    {
        nma.enabled = true;
        var nmPath = new NavMeshPath();
        nma.CalculatePath(destination, nmPath);
        path = nmPath.corners.Skip(1).ToList();
        //path = new List<Vector3>() { destination };
        //nma.SetDestination(destination);
        nma.enabled = false;
    }

    public Vector3 GetVelocity()
    {
        return rb.velocity;
    }

    #endregion

    #region Incomplete Functions

    private Vector3 ComputeForce()
    {

        var force = CalculateGoalForce();
        force += CalculateWallForce();
        force += CalculateAgentForce();
        force.y = 0;
        print("force mag: "+force.magnitude.ToString()+"\t"+force.ToString());
        //var force = growingSpiral();

        if (force != Vector3.zero)
        {
            return force.normalized * Mathf.Min(force.magnitude, Parameters.maxSpeed);
        }
        else
        {
            return Vector3.zero;
        }

    }

    private Vector3 CalculateGoalForce()
    {
        if (path.Count == 0)
        {
            return Vector3.zero;
        }

        var desiredDir = (path[0] - transform.position);
        var desiredSpeed = Mathf.Min(desiredDir.magnitude, 5f);
        var goalForce = (mass / Parameters.T) * (desiredDir.normalized * desiredSpeed - rb.velocity);
        return goalForce;
    }

    private Vector3 CalculateAgentForce()
    {
        var agentForce = Vector3.zero;

        foreach (var neighborGameObject in perceivedNeighbors)
        {

            if (!AgentManager.IsAgent(neighborGameObject))
            {
                continue;
            }

            var neighbor = AgentManager.agentsObjs[neighborGameObject];
            var dir = (transform.position - neighborGameObject.transform.position).normalized;
            var collisionDist = (radius + neighbor.radius) - Vector3.Distance(transform.position, neighborGameObject.transform.position);
            // var funcG = Mathf.Max(0f, collisionDist);
            var funcG = Mathf.Abs(collisionDist) < 0.00000001f ? collisionDist : 0f;
            // var funcG = collisionDist;
            var tangent = Vector3.Cross(Vector3.up, dir).normalized;

            agentForce += Parameters.A * Mathf.Exp(collisionDist / Parameters.B) * dir;
            agentForce += (Parameters.k * funcG) * dir;
            agentForce += Parameters.Kappa * funcG * Vector3.Dot(rb.velocity - neighbor.GetVelocity(), tangent) * tangent;
        }

        return agentForce;
    }

    private Vector3 CalculateWallForce()
    {
        var wallForce = Vector3.zero;

        foreach (var neighborGameObject in perceivedWalls)
        {

            // if (!WallManager.IsWall(neighborGameObject))
            // {
            //     print("is not wall " + neighborGameObject.name);
            //     continue;
            // }
            // else
            // {
            //     print("wall "+neighborGameObject.name);
            // }
            var dist = transform.position - neighborGameObject.transform.position;
            dist.y = 0f;
            if (Mathf.Abs(dist.x) > Mathf.Abs(dist.z)) dist.z = 0;
            else dist.x = 0;

            var collisionDist = 0.5f-dist.magnitude;
            // var funcG = collisionDist;
            var funcG = Mathf.Abs(collisionDist) < 0.00001f ? collisionDist : 0f;
            var tangent = Vector3.Cross(Vector3.up, dist.normalized).normalized;
            Vector3 n = dist.normalized * 1f / dist.magnitude;
            n = n.normalized;
            wallForce += (Parameters.WALL_A * Mathf.Exp(collisionDist / Parameters.WALL_B)) * n;
            wallForce += (Parameters.WALL_k * funcG) * n;
            wallForce -= Parameters.WALL_Kappa * funcG * Vector3.Dot(rb.velocity, tangent) * tangent;
            print(n.ToString() + "\t"+collisionDist.ToString() + "\t"+dist.magnitude.ToString() + "\t"+ wallForce.ToString());
        }
        return wallForce;
    }

    private Vector3 growingSpiral()
    {
        var totalmagnitude = 2f;
        //direction from position to center & force proportional to magnitude direction
        var dir = (Vector3.zero - transform.position); //towards center

        if (dir.magnitude < .2)
        {
            dir = -(totalmagnitude - dir.magnitude) * dir.normalized;
        }

        //var growing = dir.normalized * Mathf.Cos(Time.deltaTime) *  dir.magnitude + dir / 2f);

        //want to move in position tangent & this force is constant !!
        var tangentSpeed = 2f;
        var tangent = Vector3.Cross(Vector3.up, dir.normalized) * tangentSpeed;

        return tangent + dir;
    }

    public void ApplyForce()
    {
        var force = ComputeForce();
        force.y = 0;

        rb.AddForce(force / mass, ForceMode.Acceleration);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (AgentManager.IsAgent(other.gameObject))
        {
            // print("agent added"+other.gameObject.name);
            perceivedNeighbors.Add(other.gameObject);

        }

        else if (WallManager.IsWall(other.gameObject)
                || other.gameObject.name.Substring(0,4) == "Wall")
                // || other.gameObject.name.Substring(0,4) == "Cube")
        {
            // print("wall added"+other.gameObject.name);
            perceivedWalls.Add(other.gameObject);

        }
        else
        {
            print("unknown found "+other.gameObject.name);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (perceivedNeighbors.Contains(other.gameObject))
        {
            perceivedNeighbors.Remove(other.gameObject);

        }

        if (perceivedWalls.Contains(other.gameObject))
        {
            perceivedWalls.Remove(other.gameObject);
        }
    }

    public void OnCollisionEnter(Collision collision)
    {

    }

    public void OnCollisionExit(Collision collision)
    {

    }

    #endregion
}
