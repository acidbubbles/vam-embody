public static class MotionControlNames
{
    public const string Head = "Head";
    public const string LeftHand = "LeftHand";
    public const string RightHand = "RightHand";

    public static bool IsHeadOrHands(string name)
    {
        return name == Head || name == LeftHand || name == RightHand;
    }
}
