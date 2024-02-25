namespace Types;

public static class NullExtensions {
    public static T Unwrap<T>(this T? obj) where T : class {
        if (obj == null) {
            Environment.Exit(1);
        }
        return obj;
    }

    public static T Expect<T>(this T obj, string msg) where T : class {
        if (obj is null) {
            Console.WriteLine(msg);
            Environment.Exit(1);
        }
        return obj;
    }
    
    public static T Unwrap<T>(this T? val) where T : struct => val!.Value;
}
