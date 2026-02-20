using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("Locomotion")]
    public float runSpeed = 6f;
    public float dashSpeed = 9f;
    public float rotationSpeed = 10f;
    public float movementSmoothTime = 0.1f;

    [Header("HardLanding")]
    public float recoveryTime = 1.0f;
    public float hardLandingHeightThreshold = 5.0f;
    public float recoveryCancelThreshold = 0.2f;


    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;

    [Header("Ability - Drain")]
    [SerializeField] private float drainRadius = 8f;
    [SerializeField] private float drainCooldown = 1.0f;

    [Header("Ability - Dash Strike")]
    [SerializeField] private float strikeEnergyCost = 50f;
    [SerializeField] private float strikeDashSpeed = 30f;
    [SerializeField] private float strikeDuration = 0.3f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float knockbackForce = 20f;
}
