using System;
using System.Reflection;


namespace SongBrowserPlugin
{
    public static class ReflectionUtil
	{
		public static void SetPrivateField(object obj, string fieldName, object value)
		{
			var prop = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			prop.SetValue(obj, value);
		}
		
		public static T GetPrivateField<T>(object obj, string fieldName)
		{
			var prop = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			var value = prop.GetValue(obj);
			return (T) value;
		}
		
		public static void SetPrivateProperty(object obj, string propertyName, object value)
		{
			var prop = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			prop.SetValue(obj, value, null);
		}

		public static void InvokePrivateMethod(object obj, string methodName, object[] methodParams)
		{
			MethodInfo dynMethod = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
			dynMethod.Invoke(obj, methodParams);
		}

        public static object InvokeMethod<T>(this T o, string methodName, params object[] args)
        {
            var mi = o.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mi != null)
            {
                return mi.Invoke(o, args);
            }
            return null;
        }

        public static object InvokeStaticMethod(Type t, string methodName, params object[] args)
        {
            var mi = t.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (mi != null)
            {
                return mi.Invoke(obj: null, parameters: args);
            }
            return null;
        }       
    }
}
