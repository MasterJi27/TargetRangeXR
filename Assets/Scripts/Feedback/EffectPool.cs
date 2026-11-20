using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace TargetRange.Feedback
{
    /// <summary>
    /// Plays short particle bursts (impact sparks) from a pool, so shooting never allocates memory.
    /// </summary>
    public sealed class EffectPool : MonoBehaviour
    {
        [SerializeField] private ParticleSystem prefab;

        private ObjectPool<ParticleSystem> pool;

        private void Awake()
        {
            pool = new ObjectPool<ParticleSystem>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.gameObject.SetActive(false); },
                actionOnDestroy: ps => { if (ps != null) Destroy(ps.gameObject); },
                collectionCheck: false, defaultCapacity: 8, maxSize: 24);
        }

        public void Play(Vector3 position, Vector3 normal)
        {
            ParticleSystem ps = pool.Get();
            ps.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
            ps.Play(true);
            StartCoroutine(ReleaseWhenDone(ps, ps.main.startLifetime.constantMax + ps.main.duration));
        }

        private IEnumerator ReleaseWhenDone(ParticleSystem ps, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (ps != null) pool.Release(ps);
        }
    }
}
