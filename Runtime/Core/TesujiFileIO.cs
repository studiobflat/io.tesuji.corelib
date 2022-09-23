using System;
using System.IO;
using UnityEngine;

namespace Tesuji
{
	public static class TesujiFileIO
	{
		private static string _persistentPath = null;

		public static string persistentPath
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(_persistentPath)) return _persistentPath;

				_persistentPath =
#if UNITY_EDITOR
					Path.Combine(Application.dataPath.Replace("/Assets", "/Library"), "Tesuji");
#else
            Path.Combine(Application.persistentDataPath, "Tesuji");
            Debug.LogWarning(_persistentPath);
#endif
				return _persistentPath;
			}
		}

		public static string GetFolderPath(string folderPath, bool autoCreateFolder = true)
		{
			var path = Path.Combine(persistentPath, folderPath);
			if (autoCreateFolder && !Directory.Exists(path)) Directory.CreateDirectory(path);
			return path;
		}

		public static string GetFilePath(string filePath, bool autoCreateFolder = true)
		{
			var path = Path.Combine(persistentPath, filePath);
			if (!autoCreateFolder) return path;

			DirectoryInfo parentDir = Directory.GetParent(path);
			if (!parentDir.Exists) parentDir.Create();
			return path;
		}

		public static void WriteText(string filePath, string content)
		{
			var path = GetFilePath(filePath);

			try
			{
				File.WriteAllText(path, content);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Write error {e}\n{path}!");
			}
		}

		public static string ReadText(string fileName)
		{
			var path = GetFilePath(fileName);

			try
			{
				return File.Exists(path) ? File.ReadAllText(path) : null;
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Write error {e}\n{path}!");
			}

			return null;
		}

		public static bool WriteImage(string fileName, Texture2D tex)
		{
			var path = GetFilePath(fileName);
			var ext = Path.GetExtension(fileName).ToLower();
			byte[] bytes = null;

			switch (ext)
			{
				case ".png":
					bytes = tex.EncodeToPNG();
					break;
				case ".jpg":
					bytes = tex.EncodeToJPG();
					break;
				default:
				{
					Debug.LogWarning($"Unsupported file extension: {ext}!");
					return false;
				}
			}

			try
			{
				File.WriteAllBytes(path, bytes);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Write error {e}\n{path}!");
				return false;
			}

			return true;
		}

		public static Texture2D ReadImage(string fileName)
		{
			var path = GetFilePath(fileName);
			if (!File.Exists(path)) return null;

			byte[] bytes = null;

			try
			{
				bytes = File.ReadAllBytes(path);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Write error {e}\n{path}!");
				return null;
			}


			var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
			tex.LoadImage(bytes, true);
			return tex;
		}
	}
}