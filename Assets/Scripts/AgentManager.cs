using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class AgentManager : MonoBehaviour
{
    public int agentCount = 10;
    public float agentSpawnRadius = 20;
    public GameObject agentPrefab;

    private List<Agent> agents = new List<Agent>();
    private GameObject agentParent;

    private const float UPDATE_RATE = 0.5f;

    #region Unity Functions

    void Start()
    {
        Random.InitState(0);

        agentParent = GameObject.Find("Agents");
        for (int i = 0; i < agentCount; i++)
        {
            var randPos = new Vector3((Random.value - 0.5f) * agentSpawnRadius, 1, (Random.value - 0.5f) * agentSpawnRadius);
            NavMeshHit hit;
            NavMesh.SamplePosition(randPos, out hit, 10, NavMesh.AllAreas);
            randPos = hit.position;

            GameObject agent = null;
            agent = Instantiate(agentPrefab, randPos, Quaternion.identity);
            agent.name = "Agent " + i;
            agent.transform.parent = agentParent.transform;
            var agentScript = agent.GetComponent<Agent>();

            agents.Add(agentScript);
        }

        StartCoroutine(Run());
    }
    
    void Update()
    {
        #region Visualization
        

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }

    #endregion

    #region Incomplete Functions
    
    #endregion

    #region Private Functions
    
    #endregion

    #region Visualization Functions
    
    #endregion

    #region Utility Classes
    
    private class Tuple<K,V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v) {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
