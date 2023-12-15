/// Written by Ethan Woods -> 016erw@gmail.com
/// The SetAgentPath script is a script responsible for rapidly setting up a relatively simple path for a NavMesh Agent to follow, therefore this script is to be attached to the same gameobject that the NavMesh Agent component is located on
/// Depending on the number of points inputted in the Location array in the inspector, the agent will perform a different method of moving itself from the first point to the last (with bezier curves)
/// This is script is... ok, but could absolutely use some improvement with something as simple as even creating some toggeleable booleans in the inspector to set a point in the Location array instead of the user having to manually input the transform values
/// There is also some partial implementation of some movements which need a bit more work, also some form of randomization method might be good for things such as rodents in the environment scene

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SetAgentPath : MonoBehaviour
{
    [SerializeField] private float offsetTime, 
        desiredDuration;

    [SerializeField] private Location[] locationArray;

    [Tooltip("Y offset of agent from the ground")] [SerializeField]
    private float offset; // This is here because there were some instances in which the agent would just hover a bit over the ground no matter what I did, therefore a manual offset is a way of making a quick fix for this

    DateTime timeStamp; // Time-keeping variable for movement

    private Vector2 startingPosition;

    private NavMeshAgent agent;
    private NavMeshHit navHit; // Used for y-axis location detection

    private void Awake()
    {
        agent = gameObject.GetComponent<NavMeshAgent>(); // Latching onto local instance of NavMesh Agent
    }

    private void Start()
    {
        // Setting NavMesh Agent to start position (first position in locationArray inputted by developer)
        gameObject.GetComponent<Transform>().position = locationArray[0].position;
        gameObject.GetComponent<Transform>().rotation = locationArray[0].rotation;
        
        /// Now, since we want the NavMesh Agent to be properly bound to the terrain, 'NavMesh.SamplePosition' is basically hovering over the ground and performing raycasts to the terrain to see what y-value the terrain is at
        /// This prevents (for the most part) the NavMesh Agent either hovering over the ground or getting stuck in the ground since it's nearly impossible for the developer to set the positions that the NavMesh Agent will go to exactly at the NavMesh surface
        for (int i = 0; i < locationArray.Length; i++)
        {
            NavMesh.SamplePosition(new Vector3(locationArray[i].position.x, 100, locationArray[i].position.z), out navHit, 110, NavMesh.AllAreas);
            locationArray[i].position = navHit.position; // Adjusting Y component of transform position to height at which 'navHit' hit the NavMesh
            locationArray[i].position.y += offset; // Implementing Agent offset if necessary (though this shouldn't really be necessary)
        }
        timeStamp = DateTime.Now.AddSeconds(offsetTime); // Setting timestamp of time at which NavMesh Agent should begin to move (offsets current time by offsetTime inputted in inspector)

        StartCoroutine(movePosition());
    }

    private IEnumerator movePosition()
    {
        yield return new WaitForSeconds(offsetTime);
        float percentageComplete = 0;
        float elapsedTime = 0;

        do
        {
            yield return new WaitForEndOfFrame();
            elapsedTime += Time.deltaTime;
            percentageComplete = elapsedTime / desiredDuration;
            var agentTransform = gameObject.GetComponent<Transform>();
            switch (locationArray.Length)
            {
                case 1:
                    Debug.LogError("Agent must have more than one path position defined");
                    break;
                case 2:
                    agentTransform.position = Vector3.Lerp(locationArray[0].position, locationArray[1].position, percentageComplete);
                    break;
                case 3:
                    //gameObject.GetComponent<Transform>().position = 
                    break;
                case 4:
                    //agentTransform.position = cubicBezier(positionArray, percentageComplete);
                    break;
            }

        } while (percentageComplete < 1);
    }
    /*
    private Vector2 quadraticBezier(Vector2[] posArray, float t)
    {
    //    return
    }
    */
    private Vector2 cubicBezier(Vector2[] posArray, float t)
    {
        return (((-posArray[0] + 3 * (posArray[1] - posArray[2]) + posArray[3]) * t
                + (3 * (posArray[0] + posArray[2]) - 6 * posArray[1])) * t
                + 3 * (posArray[1] - posArray[0])) * t + posArray[0];
    }



    [Serializable]
    public class Location
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}