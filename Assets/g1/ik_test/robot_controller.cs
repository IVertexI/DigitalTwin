using UnityEngine;

public class AxisRotationCopier : MonoBehaviour
{
    public enum LocalAxis
    {
        X,
        Y,
        Z
    }

    [System.Serializable]
    public struct RotationPair
    {
        [Tooltip("The ArticulationBody that will receive the target angle.")]
        public ArticulationBody target;
        
        [Tooltip("The object providing the source rotation.")]
        public GameObject source;
        
        [Tooltip("The local axis to copy from.")]
        public LocalAxis axis;
        
        public float mult;
    }

    [SerializeField] 
    private RotationPair[] rotationPairs;

    private void FixedUpdate()
    {
        CopyRotations();
    }

    private void CopyRotations()
    {
        if (rotationPairs == null) return;

        foreach (var pair in rotationPairs)
        {
            if (pair.target == null || pair.source == null) continue;

            // Используем локальный кватернион вместо нестабильных eulerAngles
            Quaternion localRot = pair.source.transform.localRotation;
            float sourceAngle = 0f;

            // Извлекаем точный угол поворота вокруг конкретной оси
            switch (pair.axis)
            {
                case LocalAxis.X:
                    localRot.ToAngleAxis(out sourceAngle, out Vector3 axisX);
                    sourceAngle *= Mathf.Sign(Vector3.Dot(axisX, Vector3.right));
                    break;
                case LocalAxis.Y:
                    localRot.ToAngleAxis(out sourceAngle, out Vector3 axisY);
                    sourceAngle *= Mathf.Sign(Vector3.Dot(axisY, Vector3.up));
                    break;
                case LocalAxis.Z:
                    localRot.ToAngleAxis(out sourceAngle, out Vector3 axisZ);
                    sourceAngle *= Mathf.Sign(Vector3.Dot(axisZ, Vector3.forward));
                    break;
            }

            // Корректно нормализуем угол в диапазон (-180, 180)
            sourceAngle = Mathf.DeltaAngle(0f, sourceAngle) * pair.mult;

            // Применяем значение к приводу ArticulationBody
            var drive = pair.target.xDrive;
            drive.target = sourceAngle;
            pair.target.xDrive = drive;
        }
    }
}
