using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using System;

public class Agent : MonoBehaviour
{
    public float radius;
    public float mass;
    public float perceptionRadius;
    private bool growingSpiral = false;
    private bool pursueAndEvade = true;
    private bool leaderFollowing = false;
    private bool crowdFollowing = false;

    private List<Vector3> path;
    private List<Vector3> spiral;
    private NavMeshAgent nma;
    private Rigidbody rb;

    private HashSet<GameObject> perceivedNeighbors = new HashSet<GameObject>();
    private HashSet<GameObject> perceivedWalls = new HashSet<GameObject>();

    private float speed = 5f;
    private float rotation = 2f;
    private Vector2 currentRotation;
    private AgentManager manager;
    private Vector3 goal = Vector3.zero;
    //leader follower
    private Agent leader;
    private Vector3 prevLeaderVel;

    void Start()
    {
        manager = FindObjectOfType<AgentManager>();
        path = new List<Vector3>();
        spiral = new List<Vector3>();
        nma = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        currentRotation = new Vector2(Camera.main.transform.rotation.y,Camera.main.transform.rotation.x);
        gameObject.transform.localScale = new Vector3(2 * radius, 1, 2 * radius);
        nma.radius = radius;
        rb.mass = mass;
        GetComponent<SphereCollider>().radius = perceptionRadius / 2;
        Cursor.SetCursor(null, new Vector2(0, 0), CursorMode.Auto);
        if (pursueAndEvade)
        {
            var renderer = GetComponent<Renderer>();
            if (Int32.Parse(name.Substring(5)) % 2 == 0)
            // evader
            {
                renderer.material.SetColor("_Color", Color.red);
            }
            else
            // pursuer
            {
                renderer.material.SetColor("_Color", Color.green);

            }
        }
        else if (leaderFollowing)
        {
            var renderer = GetComponent<Renderer>();
            if (Int32.Parse(name.Substring(5)) == 0)
            // leader
            {
                renderer.material.SetColor("_Color", Color.red);
            }
            else
            {
                leader = GameObject.Find("Agent 0").GetComponent<Agent>();
            }
        }
    }

    private void Update()
    {
        var verticalSpeed = speed;
        float angle = Mathf.PI * currentRotation.x / 180.0f;
        // print(currentRotation.x.ToString() + " "+currentRotation.y.ToString());
        float forward_component = Input.GetAxis("Horizontal") * Mathf.Cos(-angle)
                                + Input.GetAxis("Vertical")   * Mathf.Sin(angle);
        float horizontal_component =  Input.GetAxis("Horizontal") * Mathf.Sin(-angle)
                                    + Input.GetAxis("Vertical")   * Mathf.Cos(angle);
        var moveVector =
            new Vector3(forward_component, 0, horizontal_component) * speed
            + Vector3.up * (Input.GetKey("space") ? verticalSpeed : 0)
            - Vector3.up * (Input.GetKey("left shift") ? verticalSpeed : 0);
        Camera.main.transform.position += moveVector * Time.deltaTime;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
        currentRotation.x += Input.GetAxis("Mouse X") * rotation;
        currentRotation.y -= Input.GetAxis("Mouse Y") * rotation;
        Camera.main.transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray.origin, ray.direction, out hit))
            {
                if (hit.collider.gameObject.name.Equals("Plane"))
                {
                    goal = hit.point;
                    // target.transform.position = hit.point;
                }
            }
        }
        // if (growingSpiral) return;
        if (!growingSpiral && !pursueAndEvade && (!leaderFollowing || (leaderFollowing && Int32.Parse(name.Substring(5)) == 0)))
        {
            manager.SetAgentDestinations(goal);
            ComputePathHelper();

            //if (path.Count > 1 && Vector3.Distance(transform.position, path[0]) < 1.1f)
            //{
            //    path.RemoveAt(0);
            //    } else if (path.Count == 1 && Vector3.Distance(transform.position, path[0]) < 2f)
            //    {
            //        path.RemoveAt(0);

            //        if (path.Count == 0 && !leaderFollowing)
            //        {
            //            gameObject.SetActive(false);
            //            AgentManager.RemoveAgent(gameObject);
            //        }
            //    }
        }
        else if (growingSpiral)
        {
            spiral.Add(transform.position);
        }
        else if (leaderFollowing)
        {
            var leaderVel = leader.GetVelocity();
            var followerGoal = leader.transform.position - leaderVel * Time.deltaTime;
            if (leaderVel == Vector3.zero) followerGoal = leader.transform.position - prevLeaderVel * Time.deltaTime;
            else prevLeaderVel = leaderVel;
            ComputePath(followerGoal);
            ComputePathHelper();
        }

        #region Visualization
        if (growingSpiral)
        {
            for (int i = 0; i < spiral.Count - 1; i++)
            {
                Debug.DrawLine(spiral[i], spiral[i + 1], Color.yellow);
            }
        }
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

    private void ComputePathHelper()
    {
        if (path.Count > 1 && Vector3.Distance(transform.position, path[0]) < 1.1f)
        {
            path.RemoveAt(0);
        }
        else if (path.Count == 1 && Vector3.Distance(transform.position, path[0]) < 2f)
        {
            path.RemoveAt(0);

            if (path.Count == 0 && !leaderFollowing && !crowdFollowing)
            {

                gameObject.SetActive(false);
                AgentManager.RemoveAgent(gameObject);

            }
        }
    }

    private Vector3 ComputeForce()
    {

        Vector3 force;
        if (growingSpiral)
        {
            force = GrowingSpiral();
            force += CalculateWallForce();
            force += CalculateAgentForce();
        }
        else if (pursueAndEvade)
        {
            force = PursueAndEvade();
            force += CalculateWallForce();
            force += CalculateAgentForce();
        }
        else if (leaderFollowing)
        {
            force = LeaderFollowing();
            force += CalculateWallForce();
            force += CalculateAgentForce();
        }
        else if (crowdFollowing)
        {
            var weight = .4f;
            force = weight * CrowdFollowing() + (1 - weight) * CalculateGoalForce();
            force = force.normalized * 5f;
        }
        else
        {
            force = CalculateGoalForce();
            force += CalculateWallForce();
            force += CalculateAgentForce();
        }
        force.y = 0;
        //print("force mag: "+force.magnitude.ToString()+"\t"+force.ToString());
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
        Vector3 agentForce = Vector3.zero;

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
            var funcG = Mathf.Abs(collisionDist) < 0.00000000001f ? collisionDist : 0f;
            // var funcG = collisionDist;
            var tangent = Vector3.Cross(Vector3.up, dir).normalized;
            var multiplier = 1f;
            if (neighborGameObject.name == "Agent 0" && leaderFollowing) multiplier = 2f;
            agentForce += multiplier * Parameters.A * Mathf.Exp(collisionDist / Parameters.B) * dir;
            agentForce += multiplier * (Parameters.k * funcG) * dir;
            agentForce += multiplier * Parameters.Kappa * funcG * Vector3.Dot(rb.velocity - neighbor.GetVelocity(), tangent) * tangent;
        }

        return agentForce;
    }

    private Vector3 CalculateWallForce()
    {
        Vector3 wallForce = Vector3.zero;
        if (transform.position.x > 15.5) wallForce += new Vector3(-10f,0f,0f);
        else if (transform.position.x < -15.5) wallForce += new Vector3(10f,0f,0f);
        else if (transform.position.z > 15.5) wallForce += new Vector3(0f,0f,-10f);
        else if (transform.position.z < -15.5) wallForce += new Vector3(0f,0f,10f);
        foreach (var neighborGameObject in perceivedWalls)
        {
            var dist = transform.position - neighborGameObject.transform.position;
            dist.y = 0f;
            if (neighborGameObject.name == "Cube" ||
                neighborGameObject.name == "Cube (2)")
            {
                dist.x = 0f;
                if (neighborGameObject.name == "Cube" && dist.z > -0.5) dist.z = -0.25f;
                else if (neighborGameObject.name == "Cube (2)" && dist.z < 0.5) dist.z = 0.25f;
            }
            else if (neighborGameObject.name == "Cube (6)" ||
                neighborGameObject.name == "Cube (7)")
            {
                dist.z = 0f;
                if (neighborGameObject.name == "Cube (6)" && dist.x > -0.5) dist.x = -0.25f;
                else if (neighborGameObject.name == "Cube (7)" && dist.x < 0.5) dist.x = 0.25f;
            }
            else
            {
                if (Mathf.Abs(dist.x) > Mathf.Abs(dist.z)) dist.z = 0f;
                else dist.x = 0f;
            }
            // if (!WallManager.IsWall(neighborGameObject))
            // {
            //     print("is not wall " + neighborGameObject.name);
            //     continue;
            // }
            // else
            // {
            //     print("wall "+neighborGameObject.name);
            // }

            var collisionDist = 0.5f-dist.magnitude;
            // var funcG = collisionDist;
            var funcG = Mathf.Abs(collisionDist) < 0.00000000001f ? collisionDist : 0f;
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

    private Vector3 GrowingSpiral()
    {
        var totalmagnitude = 1f;
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

    private Vector3 PursueAndEvade()
    {
        Vector3 pursueAndEvadeForce = Vector3.zero;
        if (Int32.Parse(name.Substring(5)) % 2 == 0)
        // Evader
        {
            foreach (var neighborGameObject in perceivedNeighbors)
            {

                if (!AgentManager.IsAgent(neighborGameObject) ||
                    Int32.Parse(neighborGameObject.name.Substring(5)) % 2 == 0)
                {
                    // skip if seeing another evader
                    continue;
                }
                var desiredDir = transform.position - neighborGameObject.transform.position;
                var desiredSpeed = Mathf.Min(desiredDir.magnitude, 5f);
                pursueAndEvadeForce += (mass / Parameters.T) * (desiredDir.normalized * desiredSpeed - rb.velocity);
                // var neighbor = AgentManager.agentsObjs[neighborGameObject];
                // Vector3 dist = transform.position - neighborGameObject.transform.position - new Vector3(.5f,0f,.5f);
                // Vector3 dir = dist.normalized;
                // pursueAndEvadeForce += dir * 5f * dist.magnitude;
            }
        }
        else
        // Pursuer
        {
            foreach (var neighborGameObject in perceivedNeighbors)
            {

                if (!AgentManager.IsAgent(neighborGameObject) ||
                    Int32.Parse(neighborGameObject.name.Substring(5)) % 2 == 1)
                {
                    // skip if seeing another Pursuer
                    continue;
                }
                var desiredDir = (neighborGameObject.transform.position - transform.position);
                var desiredSpeed = Mathf.Min(desiredDir.magnitude, 5f);
                pursueAndEvadeForce += (mass / Parameters.T) * (desiredDir.normalized * desiredSpeed - rb.velocity);

                // Vector3 dist = transform.position - neighborGameObject.transform.position - new Vector3(.5f,0f,.5f);
                // Vector3 dir = dist.normalized;
                // pursueAndEvadeForce -= dir * 5f * dist.magnitude;
            }
        }
        return pursueAndEvadeForce;
    }

    public Vector3 LeaderFollowing()
    {
        Vector3 leaderFollowingForce = Vector3.zero;
        leaderFollowingForce += CalculateGoalForce();
        if (Int32.Parse(name.Substring(5)) != 0)
        {

            var leaderVelocity = leader.GetVelocity();
            if (Vector3.Dot(transform.position, leaderVelocity) > 0 && leaderVelocity.magnitude > 1f)
            {
                var dir = transform.position - leader.transform.position;
                var tangent = Vector3.Cross(Vector3.up, leaderVelocity).normalized;
                var perpDistToLeader = Vector3.Dot(dir, tangent);
                var sign = Mathf.Sign(perpDistToLeader);
                leaderFollowingForce += (Mathf.Max(8f, Mathf.Exp(Parameters.B / perpDistToLeader)) * sign * tangent);
            }
        }

        return leaderFollowingForce;
    }

    public Vector3 CrowdFollowing()
    {
        Vector3 crowdFollowingForce = Vector3.zero;
        var count = 0;
        foreach (var neighborGameObject in perceivedNeighbors)
        {
            if (!AgentManager.IsAgent(neighborGameObject)) continue;
            var neighbor = AgentManager.agentsObjs[neighborGameObject];
            var desiredDir = (neighbor.transform.position - transform.position);
            var desiredSpeed = Mathf.Min(desiredDir.magnitude - (radius * 3), 5f);
            var goalForce = (desiredDir.normalized * desiredSpeed - rb.velocity) / Time.deltaTime;
            var collisionDist = (radius + neighbor.radius) - Vector3.Distance(transform.position, neighborGameObject.transform.position);
            crowdFollowingForce += (goalForce + 1000f * Mathf.Exp(collisionDist / Parameters.B) * -desiredDir);

            ++count;
        }

        if (count == 0) return crowdFollowingForce;
        return (crowdFollowingForce / count);
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
                || other.gameObject.name.Substring(0,4) == "Wall"
                || other.gameObject.name.Substring(0,4) == "Cube")
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
