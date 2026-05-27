using UnityEngine;

public class BipedUrdfBalanceController : MonoBehaviour
{
    [Header("IK Targets")]
    public Transform leftLegIKTarget;
    public Transform rightLegIKTarget;

    [Header("URDF Robot Parts")]
    [Tooltip("Корневой компонент ArticulationBody на звене Pelvis")]
    public ArticulationBody pelvisArticulationBody;
    [Tooltip("Точка центра масс (можно указать сам Pelvis, если нет отдельного CoM объекта)")]
    public Transform centerOfMass;

    [Header("Balance Parameters")]
    [Tooltip("Высота центра масс над землей")]
    public float comHeight = 0.8f;
    [Tooltip("Ширина стойки (расстояние между стопами)")]
    public float stanceWidth = 0.3f;
    [Tooltip("Высота подъема ноги при шаге")]
    public float stepHeight = 0.1f;
    [Tooltip("Скорость перемещения ноги к цели")]
    public float stepSpeed = 10f;

    private Vector3 leftHomePos;
    private Vector3 rightHomePos;
    private bool isLeftLegSupporting = true;
    private float stepProgress = 1f;
    
    private Vector3 currentLeftTarget;
    private Vector3 currentRightTarget;
    private Vector3 desiredStepWorldPos;

    void Start()
    {
        if (pelvisArticulationBody == null)
        {
            Debug.LogError("Пожалуйста, назначьте ArticulationBody таза (Pelvis)!");
            enabled = false;
            return;
        }

        // Фиксируем стартовое локальное смещение ног относительно таза
        leftHomePos = pelvisArticulationBody.transform.InverseTransformPoint(leftLegIKTarget.position);
        rightHomePos = pelvisArticulationBody.transform.InverseTransformPoint(rightLegIKTarget.position);
        
        currentLeftTarget = leftLegIKTarget.position;
        currentRightTarget = rightLegIKTarget.position;
    }

    void Update()
    {
        if (centerOfMass == null || pelvisArticulationBody == null) return;

        // 1. Извлекаем линейную скорость из ArticulationBody
        Vector3 comPos = centerOfMass.position;
        Vector3 comVel = pelvisArticulationBody.linearVelocity; // Получаем скорость URDF-корня

        // 2. Расчет Точки Захвата (Capture Point)
        float omega = Mathf.Sqrt(9.81f / comHeight);
        Vector3 capturePoint = comPos + (comVel / omega);
        capturePoint.y = pelvisArticulationBody.transform.position.y - comHeight; // Проецируем на землю

        // 3. Определение необходимости шага
        EvaluateStance(capturePoint);

        // 4. Управление движением ног
        AnimateSteps();
    }

    void EvaluateStance(Vector3 capturePoint)
    {
        Transform pelvisTransform = pelvisArticulationBody.transform;
        
        Vector3 worldLeftHome = pelvisTransform.TransformPoint(leftHomePos);
        Vector3 worldRightHome = pelvisTransform.TransformPoint(rightHomePos);

        worldLeftHome.y = capturePoint.y;
        worldRightHome.y = capturePoint.y;

        if (isLeftLegSupporting)
        {
            // Левая нога опорная
            currentLeftTarget = Vector3.Lerp(currentLeftTarget, worldLeftHome, Time.deltaTime * stepSpeed);

            // Правая ловит точку баланса
            desiredStepWorldPos = capturePoint + (pelvisTransform.right * (stanceWidth * 0.5f));
            
            if (Vector3.Distance(currentRightTarget, desiredStepWorldPos) > stanceWidth && stepProgress >= 1f)
            {
                isLeftLegSupporting = false;
                stepProgress = 0f;
            }
        }
        else
        {
            // Правая нога опорная
            currentRightTarget = Vector3.Lerp(currentRightTarget, worldRightHome, Time.deltaTime * stepSpeed);

            // Левая ловит точку баланса
            desiredStepWorldPos = capturePoint - (pelvisTransform.right * (stanceWidth * 0.5f));

            if (Vector3.Distance(currentLeftTarget, desiredStepWorldPos) > stanceWidth && stepProgress >= 1f)
            {
                isLeftLegSupporting = true;
                stepProgress = 0f;
            }
        }
    }

    void AnimateSteps()
    {
        if (stepProgress < 1f)
        {
            stepProgress += Time.deltaTime * stepSpeed * 0.5f;
            float heightOffset = Mathf.Sin(stepProgress * Mathf.PI) * stepHeight;

            if (isLeftLegSupporting)
            {
                Vector3 newPos = Vector3.Lerp(currentRightTarget, desiredStepWorldPos, stepProgress);
                newPos.y += heightOffset;
                currentRightTarget = newPos;
            }
            else
            {
                Vector3 newPos = Vector3.Lerp(currentLeftTarget, desiredStepWorldPos, stepProgress);
                newPos.y += heightOffset;
                currentLeftTarget = newPos;
            }
        }

        leftLegIKTarget.position = currentLeftTarget;
        rightLegIKTarget.position = currentRightTarget;
    }

    private void OnDrawGizmos()
    {
        if (centerOfMass == null || pelvisArticulationBody == null) return;
        
        float omega = Mathf.Sqrt(9.81f / comHeight);
        Vector3 cp = centerOfMass.position + (pelvisArticulationBody.linearVelocity / omega);
        cp.y = pelvisArticulationBody.transform.position.y - comHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(cp, 0.05f);
        Gizmos.DrawLine(centerOfMass.position, cp);
    }
}
