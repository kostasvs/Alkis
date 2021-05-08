using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SSTeam : MonoBehaviour
{
	// list of currently active spaceships
	public static HashSet<SSTeam> list = new HashSet<SSTeam>();

	// list of meshes with team-adapted UVs
	public static Dictionary<Mesh, Mesh> meshPairs = new Dictionary<Mesh, Mesh>(256);
	
	[Tooltip("Current team (0 or 1)")]
	public int team;
	[Tooltip("Mesh renderers to update with adapted meshes")]
	public MeshFilter[] meshFilters;
	[Tooltip("Particle systems to update colors")]
	public ParticleSystem[] particleSystems;
	[Tooltip("particle color gradient for second team")]
	public ParticleSystem.MinMaxGradient particleColorTo;
	
	/// <summary>
	/// collision layer for team 0 spaceships (incremented for team 1)
	/// </summary>
	public const int baseLayer = 9;
	/// <summary>
	/// collision layer for team 0 shields (incremented for team 1)
	/// </summary>
	public const int shieldBaseLayer = 11;
	public SSFlight flight { get; private set; }

	void Start()
	{
		// set layer depending on team
		gameObject.layer = baseLayer + team;

		// update shield team
		var shield = GetComponent<SSShield>();
		if (shield) shield.AssignTeam(team);
		
		if (team == 1)
		{
			// update meshes
			foreach (var item in meshFilters)
			{
				if (!meshPairs.ContainsKey(item.sharedMesh)) CreateMeshPair(item.sharedMesh);
				item.sharedMesh = meshPairs[item.sharedMesh];
			}
			// update particle systems
			foreach (var item in particleSystems)
			{
				var color = item.colorOverLifetime;
				color.color = particleColorTo;
			}
			// update laser gun
			var laser = GetComponent<SSLaser>();
			if (laser != null) foreach (var item in laser.barrels)
				{
					var bPrt = item.GetComponent<ParticleSystem>();
					if (bPrt != null)
					{
						var color = bPrt.colorOverLifetime;
						color.color = particleColorTo;
					}
				}
		}
		flight = GetComponent<SSFlight>();
		list.Add(this);
	}

	private void OnDestroy() { list.Remove(this); }

	/// <summary>
	/// Creates a copy of given mesh with UVs displaced to match second team.
	/// The resulting mesh is stored in meshPairs dictionary using the original mesh as key.
	/// </summary>
	/// <param name="mesh">Mesh to make a copy of</param>
	public static void CreateMeshPair(Mesh mesh)
	{

		//Debug.Log ("Pairing " + mesh);
		var m = Instantiate(mesh);
		var uv = m.uv;
		for (int i = 0; i < uv.Length; i++) uv[i].x += .5f;
		m.uv = uv;
		meshPairs.Add(mesh, m);
	}
}
