using System;
using System.Reflection;


namespace SongBrowser.Internals
{
    public static class ReflectionUtils
    {
        public static object GetField(this object obj, string fieldName)
        {
            return (obj is Type ? (Type)obj : obj.GetType())
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(obj);
        }
    }
}
