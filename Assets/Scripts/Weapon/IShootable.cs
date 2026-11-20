using UnityEngine;

namespace TargetRange.Weapon
{
    /// <summary>What a bullet tells the object it hit: where, from which direction and with how much push.</summary>
    public readonly struct ShotInfo
    {
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly Vector3 Normal;
        public readonly Vector3 Impulse;

        public ShotInfo(Vector3 point, Vector3 direction, Vector3 normal, Vector3 impulse)
        {
            Point = point;
            Direction = direction;
            Normal = normal;
            Impulse = impulse;
        }
    }

    /// <summary>Anything a bullet can hit and react to. The bullet only knows this interface, never the Target class.</summary>
    public interface IShootable
    {
        void OnShot(in ShotInfo shot);
    }
}
