using UnityEngine;

namespace Jazornowsky.QuantumStorage.utils
{
    public class PositionUtils
    {
        public static void SetupSidesPositions(byte flags, MachineSides machineSides)
        {
            Quaternion rotationQuaternion = SegmentCustomRenderer.GetRotationQuaternion(flags);
            machineSides.Front = rotationQuaternion * Vector3.forward;
            machineSides.Front.Normalize();
            machineSides.Back = rotationQuaternion * Vector3.back;
            machineSides.Back.Normalize();
            machineSides.Right = rotationQuaternion * Vector3.right;
            machineSides.Right.Normalize();
            machineSides.Left = rotationQuaternion * Vector3.left;
            machineSides.Left.Normalize();
            machineSides.Top = rotationQuaternion * Vector3.up;
            machineSides.Top.Normalize();
            machineSides.Bottom = rotationQuaternion * Vector3.down;
            machineSides.Bottom.Normalize();
        }

        public static void GetSegmentPos(Vector3 position, long mnX, long mnY, long mnZ, out long x, out long y, out long z)
        {
            x = mnX - (long) position.x;
            y = mnY - (long) position.y;
            z = mnZ - (long) position.z;
        }

        public static bool IsSegmentPositionEqual(SegmentEntity segment, long tempPosX, long tempPosY, long tempPosZ)
        {
            return tempPosX == segment.mWrapper.mnX &&
                   tempPosY == segment.mWrapper.mnY &&
                   tempPosZ == segment.mWrapper.mnZ;
        }
    }
}