using System.Numerics;
using Raylib_cs;

public static partial class Props
{
	static readonly Vector3[] CustomerTables =
	{
		new( 2.85f, 0f,  0.55f),
		new(-2.85f, 0f,  0.45f),
		new( 2.55f, 0f, -2.55f),
	};

	static readonly (Vector3 Pos, float RotY, bool IsFemale, Color Shirt, Color Hair, Color Skin, float Phase)[] CafeCustomers =
	{
		(new Vector3( 2.30f, 0f,  0.55f),  90f, true,  Col(190, 95, 110), Col(45, 30, 25),  Col(235, 198, 172), 0.0f),
		(new Vector3( 3.40f, 0f,  0.55f), -90f, false, Col(75, 115, 160), Col(30, 30, 35),  Col(220, 182, 155), 1.7f),
		(new Vector3(-3.40f, 0f,  0.45f),  90f, false, Col(110, 140, 95), Col(60, 45, 35),  Col(228, 190, 162), 3.1f),
		(new Vector3(-2.30f, 0f,  0.45f), -90f, true,  Col(205, 165, 85), Col(35, 25, 20),  Col(238, 202, 178), 4.4f),
		(new Vector3( 2.55f, 0f, -3.10f),   0f, false, Col(130, 100, 155), Col(50, 48, 52), Col(215, 176, 148), 5.2f),
		(new Vector3( 2.55f, 0f, -2.00f), 180f, true,  Col(85, 145, 140), Col(40, 32, 28),  Col(232, 195, 168), 2.5f),
	};

	public static void DrawCafeCustomers(float time, string speaker = "")
	{
		Vector3 tuEye = new(0.0f, 1.32f, -1.15f);
		Vector3 dungHead = new(0.0f, 1.28f, 0.85f);
		Vector3 denHead = new(0.95f, 1.28f, -0.15f);
		Vector3 hungHead = new(-0.95f, 1.28f, -0.15f);

		Vector3 dungTarget = speaker switch
		{
			"Đen" => denHead,
			"Hưng" => hungHead,
			"Dũng" => tuEye,
			_ => tuEye
		};

		Vector3 denTarget = speaker switch
		{
			"Dũng" => dungHead,
			"Hưng" => hungHead,
			"Đen" => tuEye,
			_ => tuEye
		};

		Vector3 hungTarget = speaker switch
		{
			"Dũng" => dungHead,
			"Đen" => denHead,
			"Hưng" => tuEye,
			_ => tuEye
		};

		var dungGaze = ComputeGazeTarget(dungHead, 180f, dungTarget, 1.0f);
		var denGaze = ComputeGazeTarget(denHead, -90f, denTarget, 1.0f);
		var hungGaze = ComputeGazeTarget(hungHead, 90f, hungTarget, 1.0f);

	}

	static void DrawCafeTableSet(Vector3 pos)
	{
		Color woodDark = Col(72, 46, 30);
		Color woodTop = Col(98, 64, 42);
		Color iron = Col(35, 35, 38);

		Ellipsoid(pos.X, 0.02f, pos.Z, 0.24f, 0.02f, 0.24f, iron, 12, 6);
		Box(pos.X, 0.36f, pos.Z, 0.07f, 0.68f, 0.07f, iron);
		Ellipsoid(pos.X, 0.72f, pos.Z, 0.42f, 0.025f, 0.42f, woodTop, 16, 8);
		Ellipsoid(pos.X, 0.71f, pos.Z, 0.43f, 0.015f, 0.43f, woodDark, 16, 8);

		Box(pos.X - 0.14f, 0.77f, pos.Z + 0.06f, 0.06f, 0.07f, 0.06f, Col(235, 235, 235));
		Box(pos.X + 0.15f, 0.78f, pos.Z - 0.05f, 0.055f, 0.09f, 0.055f, Col(190, 220, 230));
	}

	static void DrawCafeChair(Vector3 pos, float rotY)
	{
		Color chairWood = Col(82, 52, 34);
		Color cushion = Col(58, 42, 36);

		Rlgl.PushMatrix();
		Rlgl.Translatef(pos.X, pos.Y, pos.Z);
		Rlgl.Rotatef(rotY, 0f, 1f, 0f);

		Box(0f, 0.41f, 0.02f, 0.38f, 0.04f, 0.38f, chairWood);
		Box(0f, 0.435f, 0.02f, 0.33f, 0.025f, 0.33f, cushion);

		Box(-0.15f, 0.20f, -0.14f, 0.04f, 0.40f, 0.04f, chairWood);
		Box(0.15f, 0.20f, -0.14f, 0.04f, 0.40f, 0.04f, chairWood);
		Box(-0.15f, 0.20f, 0.17f, 0.04f, 0.40f, 0.04f, chairWood);
		Box(0.15f, 0.20f, 0.17f, 0.04f, 0.40f, 0.04f, chairWood);

		Box(-0.15f, 0.64f, -0.14f, 0.04f, 0.46f, 0.04f, chairWood);
		Box(0.15f, 0.64f, -0.14f, 0.04f, 0.46f, 0.04f, chairWood);
		Box(0f, 0.78f, -0.14f, 0.34f, 0.14f, 0.035f, chairWood);

		Rlgl.PopMatrix();
	}

	static void DrawSeatedCustomer(Vector3 pos, float rotY, bool isFemale, Color shirt, Color hair, Color skin, float time, float phase)
	{
		Rlgl.PushMatrix();
		Rlgl.Translatef(pos.X, pos.Y, pos.Z);
		Rlgl.Rotatef(rotY, 0f, 1f, 0f);

		float nod = MathF.Sin(time * 2.4f + phase) * 3.5f;
		float turn = MathF.Sin(time * 1.1f + phase * 1.7f) * 8.0f;
		float breathe = MathF.Sin(time * 2.0f + phase) * 0.005f;

		Color pants = Col(52, 56, 68);

		Box(-0.08f, 0.46f, 0.14f, 0.11f, 0.10f, 0.34f, pants);
		Box(0.08f, 0.46f, 0.14f, 0.11f, 0.10f, 0.34f, pants);
		Box(-0.08f, 0.23f, 0.28f, 0.10f, 0.44f, 0.10f, pants);
		Box(0.08f, 0.23f, 0.28f, 0.10f, 0.44f, 0.10f, pants);

		Box(0f, 0.76f + breathe, 0f, 0.34f, 0.48f, 0.18f, shirt);

		float gesture = MathF.Sin(time * 3.2f + phase) * 0.015f;
		Box(-0.21f, 0.74f + gesture, 0.14f, 0.09f, 0.09f, 0.30f, shirt);
		Box(0.21f, 0.74f - gesture, 0.14f, 0.09f, 0.09f, 0.30f, shirt);
		Ellipsoid(-0.21f, 0.74f + gesture, 0.30f, 0.045f, 0.04f, 0.05f, skin, 8, 8);
		Ellipsoid(0.21f, 0.74f - gesture, 0.30f, 0.045f, 0.04f, 0.05f, skin, 8, 8);

		Rlgl.PushMatrix();
		Rlgl.Translatef(0f, 1.01f + breathe, 0.02f);
		Rlgl.Rotatef(turn, 0f, 1f, 0f);
		Rlgl.Rotatef(nod, 1f, 0f, 0f);
		Vector2 gaze = new(MathF.Sin(time * 0.9f + phase) * 0.5f, 0f);
		DrawHead(isFemale, hair, skin, FaceId.None, gaze);
		Rlgl.PopMatrix();

		Rlgl.PopMatrix();
	}

	public static void DrawCafeWaiter(float time)
	{
		Vector3[] waypoints =
		{
			new(-1.55f, 0f, -2.65f),
			new( 1.45f, 0f, -2.65f),
			new( 1.65f, 0f, -0.75f),
			new( 1.65f, 0f,  1.65f),
			new(-1.65f, 0f,  1.65f),
			new(-1.65f, 0f, -0.75f),
		};

		float speed = 0.65f;
		float[] segLengths = new float[waypoints.Length];
		float totalDist = 0f;
		for (int i = 0; i < waypoints.Length; i++)
		{
			int next = (i + 1) % waypoints.Length;
			segLengths[i] = Vector3.Distance(waypoints[i], waypoints[next]);
			totalDist += segLengths[i];
		}

		float traveled = (time * speed) % totalDist;
		int seg = 0;
		while (traveled > segLengths[seg])
		{
			traveled -= segLengths[seg];
			seg = (seg + 1) % waypoints.Length;
		}

		int nextSeg = (seg + 1) % waypoints.Length;
		float t = segLengths[seg] > 0.001f ? traveled / segLengths[seg] : 0f;
		Vector3 pos = Vector3.Lerp(waypoints[seg], waypoints[nextSeg], t);
		Vector3 dir = Vector3.Normalize(waypoints[nextSeg] - waypoints[seg]);
		float rotY = MathF.Atan2(dir.X, dir.Z) * (180f / MathF.PI);

		float walkCycle = time * 4.2f;
		float legSwing = MathF.Sin(walkCycle) * 22f;
		float bobY = MathF.Abs(MathF.Cos(walkCycle)) * 0.018f;

		Color apron = Col(38, 36, 40);
		Color shirt = Col(235, 232, 226);
		Color pants = Col(32, 34, 40);
		Color skin = Col(230, 192, 165);
		Color hair = Col(35, 28, 25);

		Rlgl.PushMatrix();
		Rlgl.Translatef(pos.X, pos.Y + bobY, pos.Z);
		Rlgl.Rotatef(rotY, 0f, 1f, 0f);

		foreach (int side in new[] { -1, 1 })
		{
			Rlgl.PushMatrix();
			Rlgl.Translatef(side * 0.085f, 0.72f, 0f);
			Rlgl.Rotatef(side * legSwing, 1f, 0f, 0f);
			Box(0f, -0.35f, 0f, 0.11f, 0.70f, 0.12f, pants);
			Rlgl.PopMatrix();
		}

		Box(0f, 1.00f, 0f, 0.35f, 0.54f, 0.19f, shirt);
		Box(0f, 0.86f, 0.015f, 0.33f, 0.58f, 0.19f, apron);

		Rlgl.PushMatrix();
		Rlgl.Translatef(-0.22f, 1.22f, 0f);
		Rlgl.Rotatef(-legSwing * 0.6f, 1f, 0f, 0f);
		Box(0f, -0.22f, 0f, 0.09f, 0.44f, 0.09f, shirt);
		Ellipsoid(0f, -0.46f, 0f, 0.042f, 0.045f, 0.042f, skin, 8, 8);
		Rlgl.PopMatrix();

		Box(0.21f, 1.06f, 0.10f, 0.09f, 0.28f, 0.09f, shirt);
		Box(0.21f, 0.95f, 0.24f, 0.09f, 0.09f, 0.26f, shirt);
		Ellipsoid(0.21f, 0.96f, 0.37f, 0.045f, 0.04f, 0.05f, skin, 8, 8);

		Ellipsoid(0.21f, 1.01f, 0.38f, 0.19f, 0.015f, 0.19f, Col(110, 72, 45), 12, 8);
		Box(0.15f, 1.06f, 0.36f, 0.06f, 0.09f, 0.06f, Col(245, 245, 245));
		Box(0.27f, 1.07f, 0.40f, 0.055f, 0.11f, 0.055f, Col(185, 215, 225));

		Rlgl.PushMatrix();
		Rlgl.Translatef(0f, 1.28f, 0f);
		DrawHead(false, hair, skin, FaceId.None, Vector2.Zero);
		Rlgl.PopMatrix();

		Rlgl.PopMatrix();
	}
}