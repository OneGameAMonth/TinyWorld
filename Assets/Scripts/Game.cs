using UnityEngine;
using System.Collections.Generic;

public class Planet
{
    public string Name { get; set; }
    public float Population { get; set; } // in millions
    public float Resources { get; set; } // made up value
    public float TerraformPercent { get; set; }
    public float PopTrend { get; set; }
    public float ResTrend { get; set; }
    
    public void Update()
    {
        float startPop = Population;
        float startRes = Resources;

        // earths resources don't grow, it's too late for that!
        if (Name != "Earth" && Population > 0)
        {
            TerraformPercent += 1f;
            if (TerraformPercent > 100f)
                TerraformPercent = 100f;

            var terraformFactor = 1.002f + (TerraformPercent / 1000f);
            Resources *= terraformFactor;
        }
        else
            TerraformPercent = 0;

        Resources -= Population * Game.ResourceCostPerMillion;

        if (Resources < 0)
        {
            // oops
            Resources = 0;
            Population = 0;
        }
        else // pop only grows when resources present
            Population *= Game.PopulationGrowthFactor;

        PopTrend = ((Population - startPop) / startPop) * 100f;
        ResTrend = ((Resources - startRes) / startRes) * 100f;
    }
}

public class RocketShip
{
    public GameObject gameObject { get; set; }
    public float Population { get; set; }
    public float Resources { get; set; }
    public int DestinationPlanet { get; set; }
    public float Speed { get; set; }
}

public class Game : MonoBehaviour
{
    GameObject go;

    public float Speed;
    public bool GameOver;

    public List<GameObject> planetObjs;
    private List<Planet> planets;
    private List<RocketShip> rockets;
    private Planet earth;

    public GameObject RocketShipPrefab;

    public int Score = 0;
    public int StartPlanet = 2;
    public int CurrentPlanet = 2;
    public float SecondsPerTick = 1f;

    public static float ResourceCostPerMillion = 0.05f; // how many resources do 1M ppl need per tick
    public static float CostPerColonyShip = 3500f; // cost of resources for a terraforming ship
    public static float PopMovedPerColonyShip = 500f; // people moved (in M) with each colony ship
    public static float CostPerSeedShip = 7500f; // cost of resources for a seed ship
    public static float ResMovedPerSeedShip = 500f; // how many resources moved per seed ship
    public static float PopulationGrowthFactor = 1.004f; // growth of pop per tick

    // Use this for initialization
    void Start()
    {
        go = this.gameObject;
        GameOver = true;

        Camera.mainCamera.backgroundColor = Color.black;
        rockets = new List<RocketShip>();

        InitGame();
    }

    void InitGame()
    {
        GameOver = false;

        CancelInvoke();
        InvokeRepeating("UpdatePlanets", SecondsPerTick, SecondsPerTick);

        // too lazy to calc these all out.. but not too lazy to eyedrop the rgb vals from pictures?
        float m = 255f;
        planetObjs[0].renderer.material.color = new Color(187/m, 96/m, 51/m); // mercury 187 96 51
        planetObjs[1].renderer.material.color = new Color(246/m, 215/m, 158/m); // venus 246 215 158
        planetObjs[2].renderer.material.color = new Color(61/m, 108/m, 56/m); // earth 61 108 56
        planetObjs[3].renderer.material.color = new Color(158/m, 99/m, 55/m); // mars 158 99 55 
        planetObjs[4].renderer.material.color = new Color(143/m, 103/m, 63/m); // jupiter 143 103 63
        planetObjs[5].renderer.material.color = new Color(1f, 191/m, 85/m); // saturn 255 191 85 rings 152 120 99
        planetObjs[6].renderer.material.color = new Color(189/m, 227/m, 230/m); // uranus 189 227 230 rings 62 57 51
        planetObjs[7].renderer.material.color = new Color(68/m, 107/m, 252/m); // neptune 69 107 252

        GameObject.Find("Sun").renderer.material.color = new Color(0.85f, 0.85f, 0.2f);
        GameObject.Find("Saturns Rings").renderer.material.color = new Color(202/m, 170/m, 150/ m);
        GameObject.Find("Uranus' Rings").renderer.material.color = new Color(112/m, 107/m, 101/m);

        // parallel to planetObjs because lazy
        planets = new List<Planet>();
        planets.Add(new Planet { Name = "Mercury", Population = 0, Resources = 2000 });
        planets.Add(new Planet { Name = "Venus", Population = 0, Resources = 4000 });
        planets.Add(new Planet { Name = "Earth", Population = 22000, Resources = 250000 });
        planets.Add(new Planet { Name = "Mars", Population = 0, Resources = 3000 });
        planets.Add(new Planet { Name = "Jupiter", Population = 0, Resources = 0 });
        planets.Add(new Planet { Name = "Saturn", Population = 0, Resources = 0 });
        planets.Add(new Planet { Name = "Uranus", Population = 0, Resources = 0 });
        planets.Add(new Planet { Name = "Neptune", Population = 0, Resources = 0 });

        if (rockets.Count != 0)
            foreach (var r in rockets)
                Destroy(r.gameObject);

        rockets = new List<RocketShip>();
        earth = planets[2];
    }

    // Update is called once per frame
    void Update()
    {
        UpdateKeyboard();
        UpdateScore();
        UpdateGUI();

        if (earth.Population <= 0.0001f)
            EndGame();
    }

    void FixedUpdate()
    {
        UpdateRockets();
        UpdateCamera();
    }

    void EndGame()
    {
        GameOver = true;
        CancelInvoke();
    }

    void UpdateCamera()
    {
        var camPos = go.transform.position;
        var planet = planetObjs[CurrentPlanet];
        var plPos = planet.transform.position;

        if (camPos.z > plPos.z)
        {
            camPos.z -= Time.deltaTime * Speed;
            if (camPos.z < plPos.z) // overshoot?
                camPos.z = plPos.z;
        }

        if (camPos.z < plPos.z)
        {
            camPos.z += Time.deltaTime * Speed;
            if (camPos.z > plPos.z) // overshoot?
                camPos.z = plPos.z;
        }

        go.transform.position = camPos;
    }

    void UpdateKeyboard()
    {
        if ((Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) && CurrentPlanet > 0)
            CurrentPlanet--;

        if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) && CurrentPlanet < 7)
            CurrentPlanet++;

        if (Input.GetKeyDown(KeyCode.S) && earth.Population > 0 && CurrentPlanet != 2 &&
            earth.Resources > CostPerSeedShip)
        {
            var resMoved = ResMovedPerSeedShip;

            if (earth.Resources < CostPerSeedShip + resMoved)
                resMoved = earth.Resources - CostPerSeedShip; // this kills the earth
            
            var totalCost = CostPerSeedShip + resMoved;
            earth.Resources -= totalCost;
            LaunchShip(CurrentPlanet, 0, resMoved);
        }

        if (Input.GetKeyDown(KeyCode.Space) && earth.Population > 0  && CurrentPlanet != 2)
        {
            var popMoved = PopMovedPerColonyShip;

            // if less than usual, send whatever population is left
            if (earth.Population < popMoved)
                popMoved = earth.Population;

            // can only send a ship if we can afford the resources to build it
            var resCost = CostPerColonyShip;
            if (earth.Resources > resCost)
            {
                earth.Resources -= resCost;
                earth.Population -= popMoved;
                LaunchShip(CurrentPlanet, popMoved, 0);
            }
        }            

        // debug restart game
        if (Debug.isDebugBuild && Input.GetKeyDown(KeyCode.P))
            InitGame();
    }

    void LaunchShip(int destPlanet, float popToBoard, float resourcesToBoard)
    {
        var rocketObj = Instantiate(RocketShipPrefab, new Vector3(0, 8, -750), Quaternion.identity) as GameObject;

        var rocket = new RocketShip
        {
            gameObject = rocketObj,
            DestinationPlanet = destPlanet,
            Population = popToBoard,
            Resources = resourcesToBoard,
            Speed = 1f,
        };

        rockets.Add(rocket);
    }

    void UpdateRockets()
    {
        if (GameOver)
            return;

        var toRemove = new List<RocketShip>();

        foreach (var r in rockets)
        {
            var dest = planetObjs[r.DestinationPlanet].transform.position;
            var orig = r.gameObject.transform.position;

            if (Vector3.Distance(dest, orig) > 5f)
            {
                var dir = dest - orig;
                dir.Normalize();
                dir *= r.Speed;
                r.gameObject.transform.LookAt(dest);
                r.gameObject.transform.Rotate(90f, 0f, 0f);
                r.gameObject.transform.position += dir;
            }
            else
            {
                planets[r.DestinationPlanet].Population += r.Population;
                planets[r.DestinationPlanet].Resources += r.Resources;
                Destroy(r.gameObject);
                toRemove.Add(r);
            }
        }
        
        foreach (var r in toRemove)
            rockets.Remove(r);
    }

    void UpdatePlanets()
    {
        if (GameOver)
            return;

        foreach (var p in planets)
            p.Update();
    }

    void UpdateScore()
    {
        Score = 0;
        foreach (var p in planets)
            Score += (int)p.Population;

        var sText = GameObject.Find("TextScore").guiText;

        if (!GameOver)
            sText.text = "Score: " + Score;
        else
        {
            sText.text = string.Format("Game Over! Earth is dead!\nYour final score was {0:#,##0}.",  Score);
        }
    }

    void UpdateGUI()
    {
        var current = planets[CurrentPlanet];

        var plName = GameObject.Find("TextPlanetName");
        var plPop = GameObject.Find("TextPlanetPop");
        var plRes = GameObject.Find("TextPlanetResources");
        var plTerra = GameObject.Find("TextPlanetTerraform");

        var plPopTrend = GameObject.Find("TextPopTrend");
        var plResTrend = GameObject.Find("TextResTrend");

        plName.guiText.text = current.Name;
        plPop.guiText.text = string.Format("Pop: {0:#,##0} M", current.Population);
        plRes.guiText.text = string.Format("Resources: {0:#,##0}", current.Resources);

        if (CurrentPlanet != 2) // earth cannot be terraformed
            plTerra.guiText.text = string.Format("Terraformed: {0:##0}%", current.TerraformPercent);
        else
            plTerra.guiText.text = "";

        if (current.Population > 0)
        {
            string popText;
            Color popColour;
            
            string resText;
            Color resColour;

            if (current.PopTrend >= 0) // assume gaining pop when == 0
            {
                popText = "+";
                popColour = Color.green;
                //popText = string.Format("+{0:##0}%", current.PopTrend);
            }
            else
            {
                popText = "-";
                popColour = Color.red;
                //popText = string.Format("{0:##0}%", current.PopTrend);
            }

            plPopTrend.guiText.text = "";
            plPopTrend.guiText.material.color = popColour;
            for (int i = 0; i < Mathf.Abs(current.PopTrend) / 5; i++)
                plPopTrend.guiText.text += popText;

            if (current.ResTrend > 0) // assume losing resources when == 0
            {
                resText = "+";
                resColour = Color.green;
                //resText = string.Format("+{0:##0}%", current.ResTrend);
            }
            else
            {
                resText = "-";
                resColour = Color.red;
                //resText = string.Format("{0:##0}%", current.ResTrend);
            }

            plResTrend.guiText.text = "";
            plResTrend.guiText.material.color = resColour;
            for (int i = 0; i < Mathf.Abs(current.ResTrend) / 5; i++)
                plResTrend.guiText.text += resText;
        }
        else
        {
            plPopTrend.guiText.text = "";
            plResTrend.guiText.text = "";
        }
    }
}
