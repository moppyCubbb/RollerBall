using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

/*
    OnEpisodeBegin() - when the case failed/need restart one training iteration, reset objects
    CollectObservations(VectorSensor sensor) - collect features data and send to the neural network
    OnActionReceived(float[] vectorAction) - receive action and distribute reward
*/
public class RollerAgent : Agent
{
    Rigidbody rigid;
    public Transform Target;
    public float speed = 10;

    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // put agent back to the floor
        if (this.transform.localPosition.y < 0)
        {
            this.rigid.angularVelocity = Vector3.zero;
            this.rigid.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        Target.localPosition = new Vector3(Random.value * 8 - 4, 0.5f, Random.value * 8 - 4);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // traget and agent position
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        // agent velocity
        sensor.AddObservation(rigid.velocity.x);
        sensor.AddObservation(rigid.velocity.z);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        //actions
        Vector3 controlSignal = new Vector3(vectorAction[0], 0, vectorAction[1]);
        //Vector3 controlSignal = new Vector3(vectorAction[0], 0, 0);
        rigid.AddForce(controlSignal * speed);

        //rewards
        float distanceToTarget = Vector3.Distance(Target.localPosition, this.transform.localPosition);

        //reached target
        if (distanceToTarget < 1.42f)
        {
            SetReward(1.0f);
            EndEpisode();
        }
        
        if (this.transform.localPosition.y < 0)
        {
            EndEpisode();
        }
    }

    //can manually traing the network
    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = Input.GetAxis("Horizontal");
        actionsOut[1] = Input.GetAxis("Vertical");
    }
}
