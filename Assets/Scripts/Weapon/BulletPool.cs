using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using TargetRange.Core;

namespace TargetRange.Weapon
{
    /// <summary>Where a bullet ended, so sound and sparks can be played without the bullet knowing about them.</summary>
    public readonly struct ImpactInfo
    {
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly bool HitShootable;

        public ImpactInfo(Vector3 point, Vector3 normal, bool hitShootable)
        {
            Point = point;
            Normal = normal;
            HitShootable = hitShootable;
        }
    }

    /// <summary>
    /// Recycles bullets with UnityEngine.Pool.ObjectPool instead of Instantiate/Destroy for every shot,
    /// which avoids garbage collection hiccups (very noticeable in VR where frame drops cause discomfort).
    /// </summary>
    public sealed class BulletPool : MonoBehaviour
    {
        [SerializeField] private Bullet prefab;
        [SerializeField] private RangeConfig config;

        private ObjectPool<Bullet> pool;
        private GameManager game;
        private readonly HashSet<Bullet> active = new HashSet<Bullet>();
        public int ActiveCount => active.Count;

        public event Action<ImpactInfo> Impact;

        private void Awake()
        {
            game = FindAnyObjectByType<GameManager>();
            game.RoundReset += ClearActive;
            pool = new ObjectPool<Bullet>(
                createFunc: () =>
                {
                    Bullet bullet = Instantiate(prefab, transform);
                    bullet.Setup(this, config);
                    return bullet;
                },
                actionOnGet: bullet => bullet.gameObject.SetActive(true),
                actionOnRelease: bullet => bullet.gameObject.SetActive(false),
                actionOnDestroy: bullet => { if (bullet != null) Destroy(bullet.gameObject); },
                collectionCheck: false, defaultCapacity: 16, maxSize: 64);
        }

        public void Fire(Vector3 position, Vector3 direction, Collider[] ignored = null)
        {
            Bullet bullet = pool.Get();
            active.Add(bullet);
            bullet.Launch(position, direction, ignored);
        }

        public void Release(Bullet bullet)
        {
            if (active.Remove(bullet)) pool.Release(bullet);
        }
        private void ClearActive()
        {
            foreach (var bullet in active) if (bullet != null) pool.Release(bullet);
            active.Clear();
        }
        private void OnDestroy()
        {
            if (game != null) game.RoundReset -= ClearActive;
            pool?.Dispose();
        }
        public void ReportMiss() => game.RegisterMiss();

        public void ReportImpact(ImpactInfo info)
        {
            if (!info.HitShootable) ReportMiss();
            Impact?.Invoke(info);
        }
    }
}
