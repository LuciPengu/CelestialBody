using UnityEngine;
using System.Collections.Generic;

public class AdvancedOrbitalMotion : MonoBehaviour
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
        public float argumentOfPeriapsis; // in degrees
        public float meanAnomaly; // in degrees
        public float radius; // in km
        public List<Moon> moons = new List<Moon>();
        [HideInInspector] public GameObject instance;
    }

    [System.Serializable]
    public class Moon : CelestialBody
    {
        public float parentMass; // in kg
    }

    [System.Serializable]
    public class AsteroidBelt
    {
        public string name;
        public GameObject asteroidPrefab;
        public float innerRadius; // in AU
        public float outerRadius; // in AU
        public int numberOfAsteroids;
        public float inclination; // in degrees
        public List<GameObject> asteroids = new List<GameObject>();
    }

    public Transform centralBody;
    public List<CelestialBody> celestialBodies = new List<CelestialBody>();
    public List<AsteroidBelt> asteroidBelts = new List<AsteroidBelt>();
    public float timeScale = 1f; // 1 Unity time unit = 1 Earth year
    public float auToUnityUnits = 10f; // Scale factor for visualization
    public float sizeScale = 1f; // Scale factor for celestial body sizes

    private const float TWO_PI = 2f * Mathf.PI;
    private const float GRAVITATIONAL_CONSTANT = 6.67430e-11f;
    private const float SUN_MASS = 1.989e30f; // Mass of the Sun in kg

    void Start()
    {
        InstantiateCelestialBodies();
        CreateAsteroidBelts();
    }

    void Update()
    {
        float time = Time.time * timeScale;
        UpdateOrbitalPositions(time);
        UpdateAsteroidPositions(time);
    }

    void InstantiateCelestialBodies()
    {
        foreach (var body in celestialBodies)
        {
            body.instance = Instantiate(body.prefab, Vector3.zero, Quaternion.identity);
            body.instance.name = body.name;
            body.instance.transform.SetParent(transform);
            SetCelestialBodySize(body);

            foreach (var moon in body.moons)
            {
                moon.instance = Instantiate(moon.prefab, Vector3.zero, Quaternion.identity);
                moon.instance.name = moon.name;
                moon.instance.transform.SetParent(body.instance.transform);
                SetCelestialBodySize(moon);
            }
        }
    }

    void SetCelestialBodySize(CelestialBody body)
    {
        float scaleFactor = body.radius * 2f / 1000f * sizeScale / auToUnityUnits;
        body.instance.transform.localScale = Vector3.one * scaleFactor;
    }

    void CreateAsteroidBelts()
    {
        foreach (var belt in asteroidBelts)
        {
            for (int i = 0; i < belt.numberOfAsteroids; i++)
            {
                float radius = Random.Range(belt.innerRadius, belt.outerRadius);
                float angle = Random.Range(0f, TWO_PI);

                Vector3 position = new Vector3(
                    radius * Mathf.Cos(angle),
                    Random.Range(-0.1f, 0.1f) * radius, // Add some vertical spread
                    radius * Mathf.Sin(angle)
                );

                Quaternion rotation = Quaternion.Euler(0f, 0f, belt.inclination);
                position = rotation * position;

                GameObject asteroid = Instantiate(belt.asteroidPrefab, centralBody.position + position * auToUnityUnits, Quaternion.identity);
                asteroid.transform.SetParent(transform);
                asteroid.name = $"{belt.name} Asteroid {i + 1}";
                belt.asteroids.Add(asteroid);

                // Randomize asteroid size
                float sizeVariation = Random.Range(0.5f, 2f);
                asteroid.transform.localScale = Vector3.one * (0.01f * sizeScale * sizeVariation);
            }
        }
    }

    void UpdateOrbitalPositions(float time)
    {
        foreach (var body in celestialBodies)
        {
            Vector3 position = CalculateOrbitalPosition(body, time);
            body.instance.transform.position = centralBody.position + position * auToUnityUnits;

            foreach (var moon in body.moons)
            {
                Vector3 moonPosition = CalculateOrbitalPosition(moon, time, body.instance.transform.position, moon.parentMass);
                moon.instance.transform.position = moonPosition;
            }
        }
    }

    void UpdateAsteroidPositions(float time)
    {
        foreach (var belt in asteroidBelts)
        {
            for (int i = 0; i < belt.asteroids.Count; i++)
            {
                float radius = Vector3.Distance(belt.asteroids[i].transform.position, centralBody.position) / auToUnityUnits;
                float angle = CalculateMeanMotion(radius) * time;

                Vector3 position = new Vector3(
                    radius * Mathf.Cos(angle),
                    belt.asteroids[i].transform.position.y / auToUnityUnits, // Maintain vertical position
                    radius * Mathf.Sin(angle)
                );

                Quaternion rotation = Quaternion.Euler(0f, 0f, belt.inclination);
                position = rotation * position;

                belt.asteroids[i].transform.position = centralBody.position + position * auToUnityUnits;
            }
        }
    }

    Vector3 CalculateOrbitalPosition(CelestialBody body, float time, Vector3 parentPosition = default, float parentMass = 0f)
    {
        float effectiveMass = parentMass > 0f ? parentMass : SUN_MASS;
        float meanMotion = CalculateMeanMotion(body.semiMajorAxis, effectiveMass);
        float M = (body.meanAnomaly * Mathf.Deg2Rad + meanMotion * time) % TWO_PI;
        float E = SolveKepler(M, body.eccentricity);
        float trueAnomaly = 2f * Mathf.Atan(Mathf.Sqrt((1f + body.eccentricity) / (1f - body.eccentricity)) * Mathf.Tan(E / 2f));
        float distance = body.semiMajorAxis * (1f - body.eccentricity * Mathf.Cos(E));

        Vector3 positionInPlane = new Vector3(
            distance * Mathf.Cos(trueAnomaly),
            0f,
            distance * Mathf.Sin(trueAnomaly)
        );

        Vector3 position = ApplyOrbitalOrientation(positionInPlane, body);

        if (parentMass > 0f)
        {
            position = parentPosition + position * auToUnityUnits;
        }

        return position;
    }

    float CalculateMeanMotion(float semiMajorAxis, float centralMass = SUN_MASS)
    {
        return Mathf.Sqrt(GRAVITATIONAL_CONSTANT * centralMass / Mathf.Pow(semiMajorAxis * 1.496e11f, 3f));
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

    Vector3 ApplyOrbitalOrientation(Vector3 positionInPlane, CelestialBody body)
    {
        Quaternion rotation = Quaternion.Euler(
            body.inclination,
            body.longitudeOfAscendingNode,
            body.argumentOfPeriapsis
        );
        return rotation * positionInPlane;
    }
}