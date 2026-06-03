using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class G1Agent : Agent
{

    [SerializeField] Vector3 pelvisPosWeight = new Vector3(1.0f,10.0f,1.0f);
    [SerializeField] float velocityWeight = 1.0f;
    [SerializeField] float pelvisRotWeight = 1.0f;
    [SerializeField] float heightWeight = 1.0f;
    [SerializeField] float energyWeight = 1.0f;
    [SerializeField] float yawWeight = 1.0f;
    [SerializeField] float excessSpeedWeight = 1.0f;
    [SerializeField] float excessSpeedThreshold = 1.0f;


    [SerializeField] private Transform trainingAreaFloor;
    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;
    [SerializeField] private float areaWidth;
    [SerializeField] private float areaLength;

    [SerializeField] private Transform targetTransform;

    private float maxTargetVelocity;
    private Vector3 targetVelocity;
    private float targetYaw;

    [SerializeField] private ArticulationBody pelvis_link;
    [SerializeField] private ArticulationBody torso_link;
    [SerializeField] private ArticulationBody r_hip_p_link;
    [SerializeField] private ArticulationBody r_hip_r_link;
    [SerializeField] private ArticulationBody r_hip_y_link;
    [SerializeField] private ArticulationBody r_knee_link;
    [SerializeField] private ArticulationBody r_ankle_p_link;
    [SerializeField] private ArticulationBody r_ankle_r_link;
    [SerializeField] private ArticulationBody l_hip_p_link;
    [SerializeField] private ArticulationBody l_hip_r_link;
    [SerializeField] private ArticulationBody l_hip_y_link;
    [SerializeField] private ArticulationBody l_knee_link;
    [SerializeField] private ArticulationBody l_ankle_p_link;
    [SerializeField] private ArticulationBody l_ankle_r_link;
    
    
    private void ResetJoints(ArticulationBody body)
	{
	
        List<float> jointPositions = new List<float>();
        
        body.GetJointPositions(jointPositions);
        
        for (int i = 0; i < jointPositions.Count; i++)
        {
            jointPositions[i] = 0f; 
        }
        
        body.SetJointPositions(jointPositions);
        
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        SetTarget(body, 0.0f);
    
	}



    private void  ResetRobot(Vector3 newPos, float newYaw)
	{

        Quaternion pelvisRot = Quaternion.Euler(0,newYaw,0);
        Quaternion r_hip_p_Rot = Quaternion.Euler(0,0,0);
        Quaternion r_hip_r_Rot = Quaternion.Euler(-10,0,0);
        Quaternion r_hip_y_Rot = Quaternion.Euler(0,0,0);
        Quaternion r_knee_Rot = Quaternion.Euler(10,0,0);
        Quaternion r_ankle_p_Rot = Quaternion.Euler(0,0,0);
        Quaternion r_ankle_r_Rot = Quaternion.Euler(0,0,0);

        pelvis_link.TeleportRoot(newPos, Quaternion.Euler(0,0,0));

        ResetJoints(torso_link);

        ResetJoints(r_hip_p_link);
        ResetJoints(r_hip_r_link);
        ResetJoints(r_hip_y_link);
        ResetJoints(r_knee_link);
        ResetJoints(r_ankle_p_link);
        ResetJoints(r_ankle_r_link);

        ResetJoints(l_hip_p_link);
        ResetJoints(l_hip_r_link);
        ResetJoints(l_hip_y_link);
        ResetJoints(l_knee_link);
        ResetJoints(l_ankle_p_link);
        ResetJoints(l_ankle_r_link);

        pelvis_link.TeleportRoot(newPos, pelvisRot);
	}

    public override void OnEpisodeBegin()
	{
		targetTransform.position =  trainingAreaFloor.position + new Vector3(Random.Range(-areaLength, areaLength)/2.0f, Random.Range(minHeight, maxHeight)/2.0f, Random.Range(-areaWidth, areaWidth)/2.0f);
        Vector3 new_pelvis_pos = trainingAreaFloor.position + new Vector3(Random.Range(-areaLength, areaLength)/2.0f, 0.812f, Random.Range(-areaWidth, areaWidth)/2.0f);

        ResetRobot(new_pelvis_pos, Random.Range(0,360));

        targetYaw = Random.Range(0,360);

        targetVelocity = new Vector3(0,0,0);
        
        if (Random.Range(0.0f,100.0f) < 1.0f)
		{
			targetVelocity = Random.insideUnitSphere * maxTargetVelocity;
		}
        

	}
    public override void CollectObservations(VectorSensor sensor)
	{

        Vector3 toTarget = targetTransform.position - pelvis_link.transform.position;

        sensor.AddObservation(toTarget);
        sensor.AddObservation(targetYaw);

        sensor.AddObservation(pelvis_link.transform.eulerAngles);
        sensor.AddObservation(torso_link.transform.eulerAngles);

        sensor.AddObservation(r_hip_p_link.transform.eulerAngles);
        sensor.AddObservation(r_hip_r_link.transform.eulerAngles);
        sensor.AddObservation(r_hip_y_link.transform.eulerAngles);
        sensor.AddObservation(r_knee_link.transform.eulerAngles);
        sensor.AddObservation(r_ankle_p_link.transform.eulerAngles);
        sensor.AddObservation(r_ankle_r_link.transform.eulerAngles);

        sensor.AddObservation(l_hip_p_link.transform.eulerAngles);
        sensor.AddObservation(l_hip_r_link.transform.eulerAngles);
        sensor.AddObservation(l_hip_y_link.transform.eulerAngles);
        sensor.AddObservation(l_knee_link.transform.eulerAngles);
        sensor.AddObservation(l_ankle_p_link.transform.eulerAngles);
        sensor.AddObservation(l_ankle_r_link.transform.eulerAngles);
	}
    public override void OnActionReceived(ActionBuffers actions)
	{
		float r_hip_p_target = actions.ContinuousActions[0] * 180;
        float r_hip_r_target = actions.ContinuousActions[1] * 180;
        float r_hip_y_target = actions.ContinuousActions[2] * 180;
        float r_knee_target = actions.ContinuousActions[3] * 180;
        float r_ankle_p_target = actions.ContinuousActions[4] * 180;
        float r_ankle_r_target = actions.ContinuousActions[5] * 180;

        float l_hip_p_target = actions.ContinuousActions[6] * 180;
        float l_hip_r_target = actions.ContinuousActions[7] * 180;
        float l_hip_y_target = actions.ContinuousActions[8] * 180;
        float l_knee_target = actions.ContinuousActions[9] * 180;
        float l_ankle_p_target = actions.ContinuousActions[10] * 180;
        float l_ankle_r_target = actions.ContinuousActions[11] * 180;

        SetTarget(r_hip_p_link, r_hip_p_target);
        SetTarget(r_hip_r_link, r_hip_r_target);
        SetTarget(r_hip_y_link, r_hip_y_target);
        SetTarget(r_knee_link, r_knee_target);
        SetTarget(r_ankle_p_link,r_ankle_p_target);
        SetTarget(r_ankle_r_link,r_ankle_r_target);

        SetTarget(l_hip_p_link, l_hip_p_target);
        SetTarget(l_hip_r_link, l_hip_r_target);
        SetTarget(l_hip_y_link, l_hip_y_target);
        SetTarget(l_knee_link, l_knee_target);
        SetTarget(l_ankle_p_link,l_ankle_p_target);
        SetTarget(l_ankle_r_link,l_ankle_r_target);

	}

    private void SetTarget(ArticulationBody body, float target)
    {
        var drive = body.xDrive;
        drive.target = target;
        body.xDrive = drive;
    }

    public float CalculateJointEnergyCost(ArticulationBody body)
    {
        float energyCost = 0f;

        ArticulationReducedSpace forces = body.jointForce;
        ArticulationReducedSpace velocities = body.jointVelocity;

        for (int i = 0; i < body.dofCount; i++)
        {
            float force = forces[i];
            float velocity = velocities[i];

            energyCost += velocity * velocity;
        }

        return energyCost;
    }

    public float GetTotalEnergyCost()
	{
		float energy = 0.0f;

        energy += CalculateJointEnergyCost(r_hip_p_link);
        energy += CalculateJointEnergyCost(r_hip_r_link);
        energy += CalculateJointEnergyCost(r_hip_y_link);
        energy += CalculateJointEnergyCost(r_knee_link);
        energy += CalculateJointEnergyCost(r_ankle_p_link);
        energy += CalculateJointEnergyCost(r_ankle_r_link);
        energy += CalculateJointEnergyCost(l_hip_p_link);
        energy += CalculateJointEnergyCost(l_hip_r_link);
        energy += CalculateJointEnergyCost(l_hip_y_link);
        energy += CalculateJointEnergyCost(l_knee_link);
        energy += CalculateJointEnergyCost(l_ankle_p_link);
        energy += CalculateJointEnergyCost(l_ankle_r_link);

        return energy;
	}

    private float CalculateReward()
	{

        Vector3 pelvisRot = pelvis_link.transform.eulerAngles;
        Vector3 toTarget = targetTransform.position - pelvis_link.transform.position;


        float yawCoefficient = -Mathf.Abs(targetYaw - pelvisRot.y) * yawWeight;

        Vector3 velocity = pelvis_link.linearVelocity;
        float excessSpeedCoefficient = -Mathf.Max(velocity.magnitude - excessSpeedThreshold, 0.0f) * excessSpeedWeight;

        Vector3 targetDir = toTarget.normalized;
        targetDir.y = 0;
        float forwardSpeed = Vector3.Dot(velocity, targetDir);
        float velocityCoefficient = forwardSpeed * velocityWeight;

        Vector3 pelvisUp = pelvis_link.transform.up;
        float pelvisRotCoefficient = Mathf.Max(0f, Vector3.Dot(pelvisUp, Vector3.up)) * pelvisRotWeight;

        Vector3 weightedToTarget = new Vector3(toTarget.x*pelvisPosWeight.x,toTarget.y*pelvisPosWeight.y,toTarget.z*pelvisPosWeight.z);
        float pelvisPosCoefficient = -weightedToTarget.magnitude;

        float heightError = pelvis_link.transform.position.y - targetTransform.position.y;
        float heightCoefficient = Mathf.Exp(-5.0f * heightError * heightError) * heightWeight;

        float energyCoefficient = -GetTotalEnergyCost() * energyWeight;

        float reward = velocityCoefficient + pelvisRotCoefficient + pelvisPosCoefficient + heightCoefficient + energyCoefficient + yawCoefficient + excessSpeedCoefficient;
        
        return reward;
	}


    private void Update()
	{
        
        targetTransform.position += targetVelocity * Time.deltaTime;


        float reward = CalculateReward();

		SetReward(reward);

        float groundHeight = trainingAreaFloor.transform.position.y;
        float pelvisHeight = pelvis_link.transform.position.y;
        float r_foootHeight = r_ankle_r_link.transform.position.y;
        float l_foootHeight = l_ankle_r_link.transform.position.y;

        Vector3 pelvisUp = pelvis_link.transform.up;
        float pelvisAlignment = Vector3.Dot(pelvisUp, Vector3.up);

        if ((r_foootHeight > groundHeight + 0.25) && (l_foootHeight > groundHeight + 0.25))
		{
			SetReward(reward - 10000.0f);
            EndEpisode();
		} else if (pelvisHeight < groundHeight + 0.25)
		{
			SetReward(reward - 10000.0f);
            EndEpisode();
		} else if (pelvisAlignment < 0.3)
		{
			SetReward(reward - 10000.0f);
            EndEpisode();
		}


	}

}
