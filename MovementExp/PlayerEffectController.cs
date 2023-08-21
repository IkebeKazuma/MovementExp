using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectController : MonoBehaviour {

    [SerializeField] ParticleSystem landEff;

    [SerializeField] ParticleSystem avoidEff1;
    [SerializeField] ParticleSystem avoidEff2;

    public void PlayLandEff() {
        landEff.Play();
    }

    public void PlayAvoidEff() {
        avoidEff1.Play();
        avoidEff2.Play();
    }
}