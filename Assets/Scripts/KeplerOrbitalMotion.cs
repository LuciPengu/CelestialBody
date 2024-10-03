using UnityEngine;
using System.Collections.Generic;

public class KeplerOrbitalMotion : MonoBehaviour
{
    [System.Serializable]
    public class CelestialBody
    {
        public string name;
        public GameObject prefab;
        public float semiMajorAxis; // in AU
        public float eccentricity;
        public float inclination; // in degrees
        public float longitudeOfAscendingNode; // in degrees
        public float argumentOfPerihelion; // in degrees
        public float meanAnomalyAtEpoch; // in degrees
        public float orbitalPeriod; // in Earth years
        public float radius; // in km
        public float mass; // in kg
        [HideInInspector] public GameObject instance;
        public List<CelestialBody> moons = new List<CelestialBody>();
    }

    public Transform centralBody;
    public List<CelestialBody> celestialBodies = new List<CelestialBody>();
    public float timeScale = 1f; // 1 Unity time unit = 1 Earth year
    public float auToUnityUnits = 10f; // Scale factor for visualization
    public float kmToUnityUnits = 1f / 149597870.7f; // Convert km to AU, then to Unity units
    public float planetScaleFactor = 1000f;

    private const float TWO_PI = 2f * Mathf.PI;

    void Start()
    {
        InstantiateCelestialBodies();
    }

    void Update()
    {
        float time = Time.time * timeScale;
        UpdateOrbitalPositions(time);
    }

    void InstantiateCelestialBodies()
    {
        foreach (var body in celestialBodies)
        {
            InstantiateCelestialBody(body, centralBody);
        }
    }

    void InstantiateCelestialBody(CelestialBody body, Transform parent)
    {
        body.instance = Instantiate(body.prefab, Vector3.zero, Quaternion.identity);
        body.instance.name = body.name;
        body.instance.transform.SetParent(parent);

        // Set the scale based on the radius
        float scale = body.radius * kmToUnityUnits * auToUnityUnits * 2f * planetScaleFactor; // Diameter
        body.instance.transform.localScale = new Vector3(scale, scale, scale);

        foreach (var moon in body.moons)
        {
            InstantiateCelestialBody(moon, body.instance.transform);
        }
    }

    void UpdateOrbitalPositions(float time)
    {
        foreach (var body in celestialBodies)
        {
            UpdateBodyPosition(body, centralBody, time);
        }
    }

    void UpdateBodyPosition(CelestialBody body, Transform parentBody, float time)
    {
        Vector3 position = CalculateOrbitalPosition(body, time);
        body.instance.transform.position = parentBody.position + position * auToUnityUnits;

        foreach (var moon in body.moons)
        {
            UpdateBodyPosition(moon, body.instance.transform, time);
        }
    }

    Vector3 CalculateOrbitalPosition(CelestialBody body, float time)
    {
        float meanAnomaly = (body.meanAnomalyAtEpoch * Mathf.Deg2Rad + TWO_PI / body.orbitalPeriod * time) % TWO_PI;
        float eccentricAnomaly = SolveKepler(meanAnomaly, body.eccentricity);
        float trueAnomaly = 2f * Mathf.Atan(Mathf.Sqrt((1f + body.eccentricity) / (1f - body.eccentricity)) * Mathf.Tan(eccentricAnomaly / 2f));
        float distance = body.semiMajorAxis * (1f - body.eccentricity * Mathf.Cos(eccentricAnomaly));
        Vector3 positionInOrbitPlane = new Vector3(
            distance * Mathf.Cos(trueAnomaly),
            0f,
            distance * Mathf.Sin(trueAnomaly)
        );
        return RotateVector(positionInOrbitPlane, body.inclination, body.longitudeOfAscendingNode, body.argumentOfPerihelion);
    }

    float SolveKepler(float M, float e, int maxIterations = 10, float epsilon = 1e-6f)
    {
        float E = M;
        for (int i = 0; i < maxIterations; i++)
        {
            float E_next = E - (E - e * Mathf.Sin(E) - M) / (1f - e * Mathf.Cos(E));
            if (Mathf.Abs(E_next - E) < epsilon)
            {
                return E_next;
            }
            E = E_next;
        }
        return E;
    }

    Vector3 RotateVector(Vector3 vector, float inclination, float longitudeOfAscendingNode, float argumentOfPerihelion)
    {
        Quaternion rotation = Quaternion.Euler(inclination, longitudeOfAscendingNode, argumentOfPerihelion);
        return rotation * vector;
    }
}