using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class TestAgent : Agent
{

    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform trainingAreaTransform;

    [SerializeField] private float targetDamping;
    [SerializeField] private float targetVelocityMax;
    [SerializeField] private float targetAccelMax;
    [SerializeField] private float targetJerkMax;

    [SerializeField] private float accelMax;
    [SerializeField] private float velocityMax;

    private float accelSpeed;
    private Vector3 velocity;

    private Vector3 targetVelocity;
    private Vector3 targetAccel;
    private Vector3 targetJerk;
    

    private void RandomizeTargetMovement()
	{
        targetVelocity = Random.insideUnitSphere * Random.Range(-targetVelocityMax, targetVelocityMax);
        targetAccel = Random.insideUnitSphere * Random.Range(-targetAccelMax, targetAccelMax);
        targetJerk = Random.insideUnitSphere * Random.Range(-targetJerkMax, targetJerkMax);
	}

    private void RandomizeTargetPosition()
	{
        float trainingAreaRadius = trainingAreaTransform.localScale.x / 2.0f;
		targetTransform.position = trainingAreaTransform.position + Random.insideUnitSphere * trainingAreaRadius; 
	}

    private void RandomizeAgentMovement()
	{
        accelSpeed = Random.Range(0, accelMax);
        velocity = Random.insideUnitSphere * Random.Range(-velocityMax, velocityMax);
	}

    private void RandomizeAgentPosition()
	{
        float trainingAreaRadius = trainingAreaTransform.localScale.x / 2.0f;
		transform.position = trainingAreaTransform.position + Random.insideUnitSphere * trainingAreaRadius; 
	}
    

    public override void OnEpisodeBegin()
	{
        RandomizeTargetMovement();
        RandomizeTargetPosition();
        RandomizeAgentMovement();
		RandomizeAgentPosition();
        
	}
    public override void CollectObservations(VectorSensor sensor)
	{
        Vector3 relativeTargetPos = targetTransform.position - transform.position;
        sensor.AddObservation(relativeTargetPos);
        sensor.AddObservation(velocity);
	}
    public override void OnActionReceived(ActionBuffers actions)
	{
		float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        float moveZ = actions.ContinuousActions[2];

        Vector3 dir = new Vector3(moveX, moveY, moveZ);
        if (dir.magnitude > 1.0f)
		{
			dir = dir.normalized;
		}
        velocity += dir * Time.deltaTime * accelSpeed;
	}


    private float CalculateReward()
	{
		Vector3 toTarget = targetTransform.position - transform.position;
        float distanceToTarget = toTarget.magnitude;
        Vector3 relativeVelocity = velocity - targetVelocity;
        float relativeSpeed = relativeVelocity.magnitude;
        float towardTargetSpeed = Vector3.Dot(relativeVelocity, toTarget);

        float reward = 100.0f / distanceToTarget - distanceToTarget / 10.0f;
        reward += Mathf.SmoothStep(0.0f, 1.0f, distanceToTarget / 2.0f) * towardTargetSpeed;
        reward -= Mathf.SmoothStep(1.0f, 0.0f, distanceToTarget / 2.0f) * relativeSpeed;


        return reward;
	}


    private void Update()
	{
        
        float reward = CalculateReward();

		SetReward(reward);


        targetAccel += targetJerk * Time.deltaTime;
        targetVelocity += targetAccel * Time.deltaTime;
        targetTransform.position += targetVelocity * Time.deltaTime;

        targetJerk *= targetDamping;
        targetAccel *= targetDamping;
        targetVelocity *= targetDamping;

        transform.position += velocity * Time.deltaTime;

        if (Random.Range(0.0f, 1000.0f) < 1.0f)
		{
			targetJerk += Random.insideUnitSphere * targetJerkMax;
            targetAccel += Random.insideUnitSphere * targetAccelMax;
		}

	}

}
