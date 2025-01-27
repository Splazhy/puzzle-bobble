using System;

namespace PuzzleBobble.Easer;

public static class EasingFunctions
{
    public static double Linear(double x) => x;

    public static double SineIn(double x) => 1 - Math.Cos((x * Math.PI) / 2);
    public static double SineInOut(double x) => -(Math.Cos(Math.PI * x) - 1) / 2;
    public static double SineOut(double x) => Math.Sin((x * Math.PI) / 2);

    public static Func<double, double> PowerIn(double pow) => x => Math.Pow(x, pow);

    // fixed original code not correctly scaling in first half of x
    public static Func<double, double> PowerInOut(double pow) => x => x < 0.5
    //    v here used to be constant 2
        ? Math.Pow(2, pow - 1) * Math.Pow(x, pow)
        : 1 - Math.Pow(-2 * x + 2, pow) / 2;
    public static Func<double, double> PowerOut(double pow) => x => 1 - Math.Pow(1 - x, pow);

    public static double ExpoIn(double x) => x <= 0 ? 0 : Math.Pow(2, 10 * x - 10);
    public static double ExpoInOut(double x)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;
        return x < 0.5 ? Math.Pow(2, 20 * x - 10) / 2 : (2 - Math.Pow(2, -20 * x + 10)) / 2;
    }
    public static double ExpoOut(double x) => x >= 1 ? 1 : 1 - Math.Pow(2, -10 * x);
}