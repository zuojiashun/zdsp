﻿using Newtonsoft.Json;
using System;
using Zealot.Common;

public static class ObjectClone
{
    /// <summary>
    /// Perform a deep Copy of the object, using Json as a serialisation method. NOTE: Private members are not cloned using this method.
    /// </summary>
    /// <typeparam name="T">The type of object being copied.</typeparam>
    /// <param name="source">The object instance to copy.</param>
    /// <returns>The copied object.</returns>
    public static T CloneJson<T>(this T source)
    {
        // Don't serialize a null object, simply return the default for that object
        if (Object.ReferenceEquals(source, null))
        {
            return default(T);
        }

        // initialize inner objects individually
        // for example in default constructor some list property initialized with some values,
        // but in 'source' these items are cleaned -
        // without ObjectCreationHandling.Replace default constructor values will be added to result
        var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
    }

    /// <summary>
    /// Use this to perform deep copy of the object if the object contains any IInventoryItem
    /// </summary>
    public static T CloneJsonWithItemConverter<T>(this T source)
    {
        if (Object.ReferenceEquals(source, null))
        {
            return default(T);
        }

        var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
        deserializeSettings.Converters.Add(new ClientInventoryItemConverter());
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, deserializeSettings), deserializeSettings);
    }
}