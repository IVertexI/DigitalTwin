using System.Collections.Generic;
using UnityEngine;

public class CCDIKAngularLimits : MonoBehaviour
{
    [Header("IK Settings")]
    public Transform target;
    [Range(1, 30)] public int iterations = 15;
    public float delta = 0.001f;

    [Header("Chain Definition")]
    public List<IKBone> bones = new List<IKBone>();

    [System.Serializable]
    public class IKBone
    {
        public Transform transform;
        
        [Header("X Axis Limits (Degrees)")]
        public bool limitX;
        [Range(-180, 180)] public float minX = -45f;
        [Range(-180, 180)] public float maxX = 45f;

        [Header("Y Axis Limits (Degrees)")]
        public bool limitY;
        [Range(-180, 180)] public float minY = -45f;
        [Range(-180, 180)] public float maxY = 45f;

        [Header("Z Axis Limits (Degrees)")]
        public bool limitZ;
        [Range(-180, 180)] public float minZ = -45f;
        [Range(-180, 180)] public float maxZ = 45f;

        // Храним исходное локальное вращение для отсчета лимитов
        [HideInInspector] public Quaternion startLocalRotation;
        // Текущие накопленные углы относительно стартового вращения
        [HideInInspector] public float currentLocalX, currentLocalY, currentLocalZ;
    }

    private int boneCount;
    private Transform endEffector;

    void Start()
    {
        Init();
    }

    void Init()
    {
        boneCount = bones.Count;
        if (boneCount < 2) return;

        endEffector = bones[boneCount - 1].transform;

        // Запоминаем исходные позы костей (T-позу/исходную позицию робота)
        for (int i = 0; i < boneCount; i++)
        {
            if (bones[i].transform != null)
            {
                bones[i].startLocalRotation = bones[i].transform.localRotation;
                bones[i].currentLocalX = 0f;
                bones[i].currentLocalY = 0f;
                bones[i].currentLocalZ = 0f;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null || boneCount < 2) return;

        SolveCCDIKWithLimits();
    }

    void SolveCCDIKWithLimits()
    {
        for (int iter = 0; iter < iterations; iter++)
        {
            if (Vector3.SqrMagnitude(endEffector.position - target.position) < delta * delta)
                break;

            for (int i = boneCount - 2; i >= 0; i--)
            {
                IKBone bone = bones[i];
                Transform t = bone.transform;

                // --- 1. Поворот по оси X ---
                if (bone.limitX)
                {
                    RotateAndClampAxis(t, Vector3.right, ref bone.currentLocalX, bone.minX, bone.maxX);
                }
                else
                {
                    RotateFreeAxis(t, t.right);
                }

                // --- 2. Поворот по оси Y ---
                if (bone.limitY)
                {
                    RotateAndClampAxis(t, Vector3.up, ref bone.currentLocalY, bone.minY, bone.maxY);
                }
                else
                {
                    RotateFreeAxis(t, t.up);
                }

                // --- 3. Поворот по оси Z ---
                if (bone.limitZ)
                {
                    RotateAndClampAxis(t, Vector3.forward, ref bone.currentLocalZ, bone.minZ, bone.maxZ);
                }
                else
                {
                    RotateFreeAxis(t, t.forward);
                }

                // Принудительно обновляем глобальные позиции детей в иерархии Unity
                t.SetPositionAndRotation(t.position, t.rotation);
            }
        }
    }

    // Поворот вокруг оси, если лимиты выключены (свободное вращение CCD вокруг мировой/локальной оси кости)
    private void RotateFreeAxis(Transform t, Vector3 axis)
    {
        Vector3 toEffector = (endEffector.position - t.position).normalized;
        Vector3 toTarget = (target.position - t.position).normalized;

        // Проецируем векторы на плоскость, перпендикулярную оси вращения
        Vector3 effProj = Vector3.ProjectOnPlane(toEffector, axis).normalized;
        Vector3 tarProj = Vector3.ProjectOnPlane(toTarget, axis).normalized;

        if (effProj.sqrMagnitude > 0.001f && tarProj.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.FromToRotation(effProj, tarProj);
            t.rotation = rot * t.rotation;
        }
    }

    // Поворот с жестким ограничением угла
    private void RotateAndClampAxis(Transform t, Vector3 localAxis, ref float currentAngle, float min, float max)
    {
        Vector3 worldAxis = t.TransformDirection(localAxis);
        
        Vector3 toEffector = (endEffector.position - t.position).normalized;
        Vector3 toTarget = (target.position - t.position).normalized;

        Vector3 effProj = Vector3.ProjectOnPlane(toEffector, worldAxis).normalized;
        Vector3 tarProj = Vector3.ProjectOnPlane(toTarget, worldAxis).normalized;

        if (effProj.sqrMagnitude > 0.001f && tarProj.sqrMagnitude > 0.001f)
        {
            // Считаем угол между текущим вектором до кончика и вектором до цели
            float angleDelta = Vector3.SignedAngle(effProj, tarProj, worldAxis);

            // Рассчитываем гипотетический новый угол кости
            float newAngle = currentAngle + angleDelta;
            
            // Зажимаем его в ваши лимиты Inspectora
            float clampedAngle = Mathf.Clamp(newAngle, min, max);
            
            // Находим чистую дельту поворота, которую НА САМОМ ДЕЛЕ можно применить
            float actualDelta = clampedAngle - currentAngle;
            currentAngle = clampedAngle;

            // Вращаем кость в мире на разрешенный угол
            t.rotation = Quaternion.AngleAxis(actualDelta, worldAxis) * t.rotation;
        }
    }

    private void OnDrawGizmos()
    {
        if (bones == null || bones.Count < 2) return;

        for (int i = 0; i < bones.Count - 1; i++)
        {
            if (bones[i].transform != null && bones[i + 1].transform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(bones[i].transform.position, bones[i + 1].transform.position);
                Gizmos.DrawWireSphere(bones[i].transform.position, 0.02f);
            }
        }
        if (boneCount > 0 && bones[boneCount - 1].transform != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(bones[boneCount - 1].transform.position, 0.03f);
        }
    }
}
