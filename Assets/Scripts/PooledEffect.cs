using UnityEngine;

//Attached to every pooled particle instance so it can return itself to the pool once it has finished playing
public class PooledEffect : MonoBehaviour
{
    public ParticleSystem Prefab { get; private set; }

    private ParticleSystem m_ParticleSystem;

    public void Init(ParticleSystem prefab)
    {
        Prefab = prefab;
        m_ParticleSystem = GetComponent<ParticleSystem>();

        //makes Unity call OnParticleSystemStopped when all particles are dead
        var main = m_ParticleSystem.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    private void OnParticleSystemStopped()
    {
        GameManager.Instance.ReturnEffect(m_ParticleSystem);
    }
}
