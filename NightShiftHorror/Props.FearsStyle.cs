using System.Collections.Generic;
using System;
using System.Numerics;
using Raylib_cs;

public enum FearsOutfitStyle
{
	PlainFabric,    // Áo thun / áo sơ mi vải trơn có độ nhăn nhẹ
	StripedSweater, // Áo len sọc ngang (như gã đàn ông trong ảnh)
	FlannelPlaid,   // Áo sơ mi caro (kiểu nhân viên / khách đêm)
	HoodieRaincoat  // Áo khoác trùm đầu / áo mưa dày
}

public readonly record struct FearsBodySpec(
	float HeightScale,   // Chiều cao tổng thể (0.90 = thấp, 1.0 = chuẩn, 1.08 = cao lêu nghêu)
	float ShoulderWidth, // Độ rộng vai (0.26 = vai hẹp/nữ, 0.31 = chuẩn, 0.36 = vai bè)
	float ChestRadius,   // Độ nở ngực
	float BellyRadius,   // Độ phình bụng (0.16 = eo thon, 0.21 = chuẩn, 0.27 = bụng phệ)
	float LimbThickness, // Độ dày tay & chân (0.8 = mảnh khảnh, 1.0 = chuẩn, 1.25 = đầm)
	float HunchAngle,    // Độ gù/cúi đầu (0 = thẳng lưng, 5 = lầm lì, 14 = ông lão còng lưng)
	bool IsFemale,       // Tóc dài & hông nở nhẹ
	FearsOutfitStyle Outfit
)
{
	public static readonly FearsBodySpec StockyMan = new(1.00f, 0.35f, 0.25f, 0.27f, 1.22f, 5f, false, FearsOutfitStyle.StripedSweater);
	public static readonly FearsBodySpec AverageMan = new(1.02f, 0.31f, 0.22f, 0.20f, 1.00f, 2f, false, FearsOutfitStyle.FlannelPlaid);
	public static readonly FearsBodySpec SlimGirl = new(0.95f, 0.25f, 0.19f, 0.15f, 0.82f, 0f, true, FearsOutfitStyle.PlainFabric);
	public static readonly FearsBodySpec HunchedOldMan = new(0.92f, 0.28f, 0.20f, 0.22f, 0.88f, 15f, false, FearsOutfitStyle.PlainFabric);
	public static readonly FearsBodySpec TallLanky = new(1.10f, 0.30f, 0.20f, 0.17f, 0.85f, 6f, false, FearsOutfitStyle.HoodieRaincoat);

	public static readonly FearsBodySpec Manager = new(1.00f, 0.34f, 0.24f, 0.27f, 1.18f, 6f, false, FearsOutfitStyle.StripedSweater);
	public static readonly FearsBodySpec OldManNpc = new(0.92f, 0.28f, 0.20f, 0.22f, 0.88f, 15f, false, FearsOutfitStyle.PlainFabric);
	public static readonly FearsBodySpec MaiNpc = new(0.95f, 0.25f, 0.19f, 0.15f, 0.82f, 0f, true, FearsOutfitStyle.PlainFabric);
	public static readonly FearsBodySpec HungNpc = new(1.08f, 0.30f, 0.20f, 0.16f, 0.88f, 8f, false, FearsOutfitStyle.FlannelPlaid);
	public static readonly FearsBodySpec YellowGirl = new(0.96f, 0.26f, 0.20f, 0.16f, 0.85f, 0f, true, FearsOutfitStyle.HoodieRaincoat);
	public static readonly FearsBodySpec YellowHorror = new(1.16f, 0.27f, 0.17f, 0.12f, 0.70f, 13f, true, FearsOutfitStyle.HoodieRaincoat);
	public static readonly FearsBodySpec StaffGirl = new(0.95f, 0.26f, 0.19f, 0.16f, 0.85f, 1f, true, FearsOutfitStyle.FlannelPlaid);
	public static readonly FearsBodySpec StaffGuy = new(1.03f, 0.32f, 0.23f, 0.22f, 1.05f, 2f, false, FearsOutfitStyle.PlainFabric);
}

public static partial class Props
{
	static readonly Dictionary<(FearsOutfitStyle, uint), Texture2D> _outfitTextures = new();

	static Texture2D GetOutfitTexture(FearsOutfitStyle style, Color mainCol)
	{
		uint key = ((uint)mainCol.R << 24) | ((uint)mainCol.G << 16) | ((uint)mainCol.B << 8) | mainCol.A;
		if (_outfitTextures.TryGetValue((style, key), out Texture2D cached) && cached.Id != 0)
			return cached;

		const int w = 64, h = 64;
		Image img = Raylib.GenImageColor(w, h, mainCol);

		Color darkCol = new((byte)(mainCol.R * 0.45f), (byte)(mainCol.G * 0.45f), (byte)(mainCol.B * 0.45f), (byte)255);
		Color lightCol = new((byte)Math.Min(255, mainCol.R * 1.35f + 25), (byte)Math.Min(255, mainCol.G * 1.35f + 25), (byte)Math.Min(255, mainCol.B * 1.35f + 25), (byte)255);

		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				Color c = mainCol;
				if (style == FearsOutfitStyle.StripedSweater)
				{
					int band = (y / 5) % 3;
					c = band == 0 ? darkCol : (band == 1 ? lightCol : mainCol);
				}
				else if (style == FearsOutfitStyle.FlannelPlaid)
				{
					bool rowLine = (y % 8) < 2;
					bool colLine = (x % 8) < 2;
					if (rowLine && colLine) c = darkCol;
					else if (rowLine || colLine) c = LerpCol(mainCol, darkCol, 0.5f);
				}

				// Nhiễu hạt vải + đổ bóng tối hai bên sườn & nếp gấp dưới bụng
				int grain = (Hash(x, y, 71) % 17) - 8;
				float sideShade = 1f - 0.24f * MathF.Abs((x / (float)w) - 0.5f) * 2f;
				float creaseShade = 1f - 0.12f * MathF.Sin((y / (float)h) * MathF.PI * 3f);

				byte r = (byte)Math.Clamp((c.R + grain) * sideShade * creaseShade, 0, 255);
				byte g = (byte)Math.Clamp((c.G + grain) * sideShade * creaseShade, 0, 255);
				byte b = (byte)Math.Clamp((c.B + grain) * sideShade * creaseShade, 0, 255);

				Raylib.ImageDrawPixel(ref img, x, y, new Color(r, g, b, (byte)255));
			}
		}

		Texture2D tex = Raylib.LoadTextureFromImage(img);
		Raylib.UnloadImage(img);
		Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
		Raylib.SetTextureWrap(tex, TextureWrap.Repeat);

		_outfitTextures[(style, key)] = tex;
		return tex;
	}

	static void DrawTexturedLimb(Vector3 p0, Vector3 p1, float rBottom, float rTop,
								 float scaleX, float scaleZ, Texture2D tex, Color tint, int slices = 12)
	{
		if ((p1 - p0).LengthSquared() < 0.00001f) return;

		Rlgl.SetTexture(tex.Id);
		Rlgl.Begin(DrawMode.Quads);
		Rlgl.Color4ub(tint.R, tint.G, tint.B, tint.A);

		for (int i = 0; i < slices; i++)
		{
			float a0 = (i / (float)slices) * MathF.PI * 2f;
			float a1 = ((i + 1) / (float)slices) * MathF.PI * 2f;
			float u0 = i / (float)slices;
			float u1 = (i + 1) / (float)slices;

			float c0 = MathF.Cos(a0), s0 = MathF.Sin(a0);
			float c1 = MathF.Cos(a1), s1 = MathF.Sin(a1);

			Vector3 b0 = new(p0.X + c0 * rBottom * scaleX, p0.Y, p0.Z + s0 * rBottom * scaleZ);
			Vector3 b1 = new(p0.X + c1 * rBottom * scaleX, p0.Y, p0.Z + s1 * rBottom * scaleZ);
			Vector3 t1 = new(p1.X + c1 * rTop * scaleX, p1.Y, p1.Z + s1 * rTop * scaleZ);
			Vector3 t0 = new(p1.X + c0 * rTop * scaleX, p1.Y, p1.Z + s0 * rTop * scaleZ);

			Rlgl.TexCoord2f(u0, 1f); Rlgl.Vertex3f(b0.X, b0.Y, b0.Z);
			Rlgl.TexCoord2f(u1, 1f); Rlgl.Vertex3f(b1.X, b1.Y, b1.Z);
			Rlgl.TexCoord2f(u1, 0f); Rlgl.Vertex3f(t1.X, t1.Y, t1.Z);
			Rlgl.TexCoord2f(u0, 0f); Rlgl.Vertex3f(t0.X, t0.Y, t0.Z);
		}

		Rlgl.End();
		Rlgl.SetTexture(0);
	}

	public static void DrawFearsCharacter(Vector3 pos, float yawDeg, Color shirtColor, Color hairColor,
										  FearsBodySpec spec, float phase, float swing,
										  float headYaw = 0f, float headPitch = 0f, float headRoll = 0f,
										  Vector2? eyeGaze = null, FaceId faceId = FaceId.None)
	{
		Color skin = Col(218, 182, 156);
		Color pants = spec.Outfit == FearsOutfitStyle.HoodieRaincoat ? shirtColor : Col(34, 36, 42);
		Color shoe = Col(18, 18, 22);
		Texture2D clothTex = GetOutfitTexture(spec.Outfit, shirtColor);
		float s = MathF.Sin(phase) * swing;

		Rlgl.PushMatrix();
		Rlgl.Translatef(pos.X, pos.Y, pos.Z);
		Rlgl.Rotatef(yawDeg, 0, 1, 0);
		Rlgl.Scalef(1f, spec.HeightScale, 1f);

		// Chân (hông nữ hơi nở rộng hơn một chút so với nam)
		float hipHalf = spec.IsFemale ? 0.115f : 0.105f * spec.LimbThickness;
		float legR = 0.085f * spec.LimbThickness;
		foreach (int side in new[] { -1, 1 })
		{
			float legAng = side == -1 ? s : -s;
			Rlgl.PushMatrix();
			Rlgl.Translatef(side * hipHalf, 0.84f, 0f);
			Rlgl.Rotatef(legAng, 1, 0, 0);
			Raylib.DrawCylinder(new Vector3(0, -0.76f, 0), legR * 0.9f, legR * 1.05f, 0.76f, 10, pants);
			Ellipsoid(0, -0.78f, 0.04f, legR * 0.95f, 0.05f, 0.15f, shoe, 8, 10);
			Rlgl.PopMatrix();
		}

		// Thân trên (nghiêng nhẹ theo độ gù lưng HunchAngle)
		const float by = 0.82f;
		Rlgl.PushMatrix();
		Rlgl.Translatef(0f, by, 0f);
		Rlgl.Rotatef(spec.HunchAngle, 1, 0, 0);

		float hipR = spec.IsFemale ? spec.BellyRadius * 1.15f : spec.BellyRadius * 0.92f;
		// Phần hông -> eo/bụng
		DrawTexturedLimb(Vector3.Zero, new Vector3(0, 0.30f, 0.008f),
						 hipR, spec.BellyRadius, 1.22f, 0.80f, clothTex, Color.White, 14);
		// Phần eo/bụng -> vai ngực
		DrawTexturedLimb(new Vector3(0, 0.30f, 0.008f), new Vector3(0, 0.60f, 0f),
						 spec.BellyRadius, spec.ChestRadius, 1.28f, 0.78f, clothTex, Color.White, 14);

		// Nắp bo cổ áo & cổ
		Ellipsoid(0f, 0.59f, 0f, spec.ShoulderWidth * 0.85f, 0.06f, 0.17f, LerpCol(shirtColor, Color.Black, 0.35f), 10, 14);
		float neckR = 0.065f * spec.LimbThickness;
		Raylib.DrawCylinder(new Vector3(0, 0.58f, 0.01f), neckR, neckR * 0.95f, 0.09f, 10, skin);

		// Hai cánh tay
		float armR = 0.072f * spec.LimbThickness;
		foreach (int side in new[] { -1, 1 })
		{
			float armAng = side == -1 ? -s * 0.65f : s * 0.65f;
			Rlgl.PushMatrix();
			Rlgl.Translatef(side * spec.ShoulderWidth, 0.55f, 0f);
			Rlgl.Rotatef(armAng, 1, 0, 0);
			Rlgl.Rotatef(side * (-3f - spec.LimbThickness * 2.5f), 0, 0, 1);

			Ellipsoid(0f, -0.02f, 0f, armR * 1.15f, armR * 1.2f, armR * 1.15f, shirtColor, 10, 12);
			DrawTexturedLimb(new Vector3(0, -0.44f, 0), new Vector3(0, -0.02f, 0),
							 armR * 0.82f, armR * 1.05f, 1.0f, 1.0f, clothTex, Color.White, 10);
			Ellipsoid(0f, -0.49f, 0.01f, armR * 0.62f, 0.065f, armR * 0.68f, skin, 8, 10);

			Rlgl.PopMatrix();
		}

		// Đầu & Mũ trùm (nếu mặc hoodie/áo mưa)
		Rlgl.PushMatrix();
		Rlgl.Translatef(0f, 0.65f, 0.015f);
		Rlgl.Rotatef(headYaw, 0, 1, 0);
		Rlgl.Rotatef(-headPitch - spec.HunchAngle * 0.5f, 1, 0, 0);
		Rlgl.Rotatef(headRoll, 0, 0, 1);

		DrawHead(spec.IsFemale, hairColor, skin, faceId, eyeGaze ?? Vector2.Zero);

		if (spec.Outfit == FearsOutfitStyle.HoodieRaincoat)
		{
			Ellipsoid(0f, 0.18f, -0.03f, 0.165f, 0.195f, 0.175f, shirtColor, 12, 14);
		}

		Rlgl.PopMatrix(); // Kết thúc đầu
		Rlgl.PopMatrix(); // Kết thúc thân trên
		Rlgl.PopMatrix(); // Kết thúc nhân vật
	}
}