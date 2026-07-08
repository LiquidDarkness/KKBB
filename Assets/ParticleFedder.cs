using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleFedder : MonoBehaviour
{
    public ParticleSystem particleSystem;
    public AnimationCurve xFade, yFade;
    private ParticleSystem.Particle[] particles;

    // Start is called before the first frame update
    void Start()
    {
        particles = new ParticleSystem.Particle[particleSystem.maxParticles];
    }

    // Update is called once per frame
    void Update()
    {
        int count = particleSystem.GetParticles(particles);
        for (int i = 0; i < count; i++)
        {
            var particle = particles[i];
            var colour = particle.color;
            float alpha = Mathf.Min(xFade.Evaluate(particle.position.x), yFade.Evaluate(particle.position.y));
            alpha = Mathf.Clamp01(alpha);
            colour.a = (byte)(alpha * 255);
            particle.color = colour;
            particles[i] = particle;
        }
        particleSystem.SetParticles(particles, count);
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    [ContextMenu("Play")]

    public void Play()
    {
        particleSystem.Play();
    }
}
