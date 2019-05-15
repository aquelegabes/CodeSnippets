public static class FileManipulation
{
	public static bool OpenFile(string path, out byte[] file)
	{
		if (string.IsNullOrWhiteSpace(path))
			throw new NullReferenceException("Path is required");
		
		try
		{
			FileStream fs = File.OpenRead(path);
			byte[] bytes = new byte[fs.Length];
			fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
			fs.Close();
			file = bytes;
			return true;
		}
		catch (Exception ex)
		{
			file = default(byte[]);
			return false;
		}
	}

	public static bool SaveFile(byte[] file, string targetPath, string name, bool overwriteFile = false)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new NullReferenceException("File name is required");
		if (string.IsNullOrWhiteSpace(targetPath))
			throw new NullReferenceException("Target path is required");
		if (file == null || file.Length == 0)
			throw new NullReferenceException("File is required");
			
		try
		{
			string fullPath = $@"{targetPath}\{name}";
			if (!Directory.Exists(targetPath))
				Directory.CreateDirectory(targetPath);
			if (File.Exists(fullPath) && overwriteFile)                    
				File.WriteAllBytes(fullPath, file);
			else
				return false
			return true;
		}
		catch (Exception ex)
		{
			return false;
		}
	}
}