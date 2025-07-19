using System.Collections.Generic;
using System.Runtime.InteropServices;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Universal_x86_Tuning_Utility.Properties;

public sealed class Settings
{
    public static Settings Default = new Settings();
    
    private const string Path = "props.dat";
    private Dictionary<string, object> _properties = new Dictionary<string, object>();
    
    public T? Get<T>(string key)
    {
        if (_properties.Count != 0)
        {
            var value = CollectionsMarshal.GetValueRefOrNullRef(_properties, key);
            return (T?)value;
        }
        
        return default;
    }

    public void Set<T>(string key, T value)
    {
        if (value != null)
        {
            ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_properties, key, out _);
            valueRef = value;
        }
    }

    public void Save()
    {
        var serialized = JsonSerializer.Serialize(_properties);
        System.IO.File.WriteAllText(Path, serialized);
    }

    private void Load()
    {
        if (System.IO.File.Exists(Path))
        {
            var serializedObject = System.IO.File.ReadAllText(Path);
            if (!string.IsNullOrWhiteSpace(serializedObject))
            {
                _properties = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedObject) ?? new Dictionary<string, object>();
            }
        }
    }
}