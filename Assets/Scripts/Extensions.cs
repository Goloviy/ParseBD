using Unity.Mathematics;

public static class Extensions
{
    public static int TruncateLastDigits(this int number, int digitsToZero)
    {
        var power = (int)math.pow(10, digitsToZero);
        return (number / power) * power;
    }
}
