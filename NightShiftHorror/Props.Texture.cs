using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;
using static System.Net.Mime.MediaTypeNames;

public static partial class Props
{
	static readonly Dictionary<string, Texture2D> _worldTextures = new();
	static Texture2D? _fallbackCheckerTex;

	public static void LoadWorldTextures()
	{
		// Nạp sẵn các texture tiêu biểu (bạn có thể đổi số 01..14 tùy ý)
		GetWorldTex("Floor/Horror_Floor_12-512x512.png"); // Sàn gạch caro xám cửa hàng
		GetWorldTex("Floor/Horror_Floor_13-512x512.png"); // Sàn gạch caro nâu cũ
		GetWorldTex("Brick/Horror_Brick_01-512x512.png"); // Tường gạch nâu mặt tiền
		GetWorldTex("Brick/Horror_Brick_11-512x512.png"); // Tường gạch xám rêu
		GetWorldTex("Metal/Horror_Metal_05-512x512.png"); // Tôn sóng / cửa cuốn
		GetWorldTex("Metal/Horror_Metal_10-512x512.png"); // Kim loại rỉ kệ hàng / tủ
		GetWorldTex("Wall/Horror_Wall_01-512x512.png");   // Tường vữa trong cửa hàng
		GetWorldTex("Stone/Horror_Stone_01-512x512.png"); // Vỉa hè / mặt đường nhựa
	}

	public static void UnloadWorldTextures()
	{
		foreach (var tex in _worldTextures.Values)
		{
			if (tex.Id != 0) Raylib.UnloadTexture(tex);
		}
		_worldTextures.Clear();

		if (_fallbackCheckerTex.HasValue && _fallbackCheckerTex.Value.Id != 0)
		{
			Raylib.UnloadTexture(_fallbackCheckerTex.Value);
			_fallbackCheckerTex = null;
		}
	}

	// Lấy texture theo đường dẫn tương đối trong Assets/Textures/ (tự cache, không lo lag)
	public static Texture2D GetWorldTex(string relativePath, bool pixelArtCrisp = false)
	{
		if (_worldTextures.TryGetValue(relativePath, out Texture2D cached) && cached.Id != 0)
			return cached;

		string fullPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Textures", relativePath);
		if (!File.Exists(fullPath))
		{
			// Tìm ngược lên thư mục gốc project nếu chạy trực tiếp từ IDE
			string devPath = Path.Combine("Assets", "Textures", relativePath);
			if (File.Exists(devPath)) fullPath = devPath;
		}

		if (File.Exists(fullPath))
		{
			Texture2D tex = Raylib.LoadTexture(fullPath);
			Raylib.SetTextureWrap(tex, TextureWrap.Repeat);
			Raylib.SetTextureFilter(tex, pixelArtCrisp ? TextureFilter.Point : TextureFilter.Bilinear);
			_worldTextures[relativePath] = tex;
			return tex;
		}

		return GetFallbackTexture();
	}

	static Texture2D GetFallbackTexture()
	{
		if (_fallbackCheckerTex.HasValue && _fallbackCheckerTex.Value.Id != 0)
			return _fallbackCheckerTex.Value;

		var img = Raylib.GenImageChecked(64, 64, 16, 16, Col(90, 90, 95), Col(65, 65, 70));
		Texture2D tex = Raylib.LoadTextureFromImage(img);
		Raylib.UnloadImage(img);
		Raylib.SetTextureWrap(tex, TextureWrap.Repeat);
		_fallbackCheckerTex = tex;
		return tex;
	}

	// Vẽ mặt phẳng ngang (Sàn nhà, trần nhà, mặt đường) có lặp vân theo kích thước thực tế (tileMeters)
	public static void DrawTexturedFloor(Vector3 center, float width, float depth, Texture2D tex,
										 float tileMeters = 1.5f, Color? tint = null, bool faceDown = false)
	{
		Color c = tint ?? Color.White;
		float hw = width * 0.5f;
		float hd = depth * 0.5f;
		float uMax = width / tileMeters;
		float vMax = depth / tileMeters;

		Rlgl.SetTexture(tex.Id);
		Rlgl.Begin(DrawMode.Quads);
		Rlgl.Color4ub(c.R, c.G, c.B, c.A);

		if (!faceDown)
		{
			Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(center.X - hw, center.Y, center.Z - hd);
			Rlgl.TexCoord2f(0f, vMax); Rlgl.Vertex3f(center.X - hw, center.Y, center.Z + hd);
			Rlgl.TexCoord2f(uMax, vMax); Rlgl.Vertex3f(center.X + hw, center.Y, center.Z + hd);
			Rlgl.TexCoord2f(uMax, 0f); Rlgl.Vertex3f(center.X + hw, center.Y, center.Z - hd);
		}
		else
		{
			Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(center.X - hw, center.Y, center.Z + hd);
			Rlgl.TexCoord2f(0f, vMax); Rlgl.Vertex3f(center.X - hw, center.Y, center.Z - hd);
			Rlgl.TexCoord2f(uMax, vMax); Rlgl.Vertex3f(center.X + hw, center.Y, center.Z - hd);
			Rlgl.TexCoord2f(uMax, 0f); Rlgl.Vertex3f(center.X + hw, center.Y, center.Z + hd);
		}

		Rlgl.End();
		Rlgl.SetTexture(0);
	}

	// Vẽ hình hộp 3D (Tường, quầy, cột, cửa cuốn, bệ) phủ texture lặp đều 6 mặt không bị kéo giãn
	public static void DrawTexturedBox(Vector3 center, Vector3 size, Texture2D tex,
									   float tileMeters = 1.5f, Color? tint = null)
	{
		Color c = tint ?? Color.White;
		float hx = size.X * 0.5f;
		float hy = size.Y * 0.5f;
		float hz = size.Z * 0.5f;

		float ux = size.X / tileMeters;
		float uy = size.Y / tileMeters;
		float uz = size.Z / tileMeters;

		Rlgl.SetTexture(tex.Id);
		Rlgl.Begin(DrawMode.Quads);
		Rlgl.Color4ub(c.R, c.G, c.B, c.A);

		// Mặt trước (+Z)
		Rlgl.TexCoord2f(0f, uy); Rlgl.Vertex3f(center.X - hx, center.Y - hy, center.Z + hz);
		Rlgl.TexCoord2f(ux, uy); Rlgl.Vertex3f(center.X + hx, center.Y - hy, center.Z + hz);
		Rlgl.TexCoord2f(ux, 0f); Rlgl.Vertex3f(center.X + hx, center.Y + hy, center.Z + hz);
		Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(center.X - hx, center.Y + hy, center.Z + hz);

		// Mặt sau (-Z)
		Rlgl.TexCoord2f(0f, uy); Rlgl.Vertex3f(center.X + hx, center.Y - hy, center.Z - hz);
		Rlgl.TexCoord2f(ux, uy); Rlgl.Vertex3f(center.X - hx, center.Y - hy, center.Z - hz);
		Rlgl.TexCoord2f(ux, 0f); Rlgl.Vertex3f(center.X - hx, center.Y + hy, center.Z - hz);
		Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(center.X + hx, center.Y + hy, center.Z - hz);

		// Mặt phải (+X)
		Rlgl.TexCoord2f(0f, uy); Rlgl.Vertex3f(center.X + hx, center.Y - hy, center.Z + hz);
		Rlgl.TexCoord2f(uz, uy); Rlgl.Vertex3f(center.X + hx, center.Y - hy, center.Z - hz);
		Rlgl.TexCoord2f(uz, 0f); Rlgl.Vertex3f(center.X + hx, center.Y + hy, center.Z - hz);
		Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(center.X + hx, center.Y + hy, center.Z + hz);

		// Mặt trái (-X)
		Rlgl.TexCoord2f(0f, uy); Rlgl.Vertex3f(center.X - hx, center.Y - hy, center.Z - hz);
		Rlgl.TexCoord2f(uz, uy); Rlgl.Vertex3f(center.X - hx, center.Y - hy, center.Z + hz);
		Rlgl.TexCoord2f(uz, 0f); Rlgl.Vertex3f(center.X - hx, center.Y + hy, center.Z + hz);
		Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(center.X - hx, center.Y + hy, center.Z - hz);

		// Mặt trên (+Y)
		Rlgl.TexCoord2f(0f, uz); Rlgl.Vertex3f(center.X - hx, center.Y + hy, center.Z + hz);
		Rlgl.TexCoord2f(ux, uz); Rlgl.Vertex3f(center.X + hx, center.Y + hy, center.Z + hz);
		Rlgl.TexCoord2f(ux, 0f); Rlgl.Vertex3f(center.X + hx, center.Y + hy, center.Z - hz);
		Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(center.X - hx, center.Y + hy, center.Z - hz);

		// Mặt dưới (-Y)
		Rlgl.TexCoord2f(0f, uz); Rlgl.Vertex3f(center.X - hx, center.Y - hy, center.Z - hz);
		Rlgl.TexCoord2f(ux, uz); Rlgl.Vertex3f(center.X + hx, center.Y - hy, center.Z - hz);
		Rlgl.TexCoord2f(ux, 0f); Rlgl.Vertex3f(center.X + hx, center.Y - hy, center.Z + hz);
		Rlgl.TexCoord2f(0f, 0f); Rlgl.Vertex3f(center.X - hx, center.Y - hy, center.Z + hz);

		Rlgl.End();
		Rlgl.SetTexture(0);
	}
}