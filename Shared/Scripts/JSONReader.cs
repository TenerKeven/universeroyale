using Godot;
using System;
using System.Text.Json;

public class JSONReader<T>
{
    public static T DeserializeFile(string path) 
    {
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

        if (file == null)
        {
            return default;
        }

        string text = file.GetAsText();
        file.Close();

        return JsonSerializer.Deserialize<T>(text);
    }
}
